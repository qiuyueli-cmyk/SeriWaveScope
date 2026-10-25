using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using Yell.Core.Contracts;
using Yell.Core.Models;

namespace Yell.Infrastructure.Persistence
{
    /// <summary>
    /// SQLite 历史数据仓储（实例实现）。
    /// 单条插入串行化，避免多线程同时写库产生锁冲突；
    /// 查询与写入可以并发，由 WAL 模式支撑。
    /// </summary>
    public sealed class SqliteHistoryRepository : IHistoryRepository
    {
        private static readonly object NativeInitLock = new();
        private static bool _nativeInitialized;

        private readonly string _connectionString;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public SqliteHistoryRepository()
        {
            EnsureNativeLibrary();

            string dataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(dataFolder);

            DatabasePath = Path.Combine(dataFolder, "seriwave_history.db");
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                DefaultTimeout = 5
            }.ToString();
        }

        public string DatabasePath { get; }

        public async Task InsertAsync(HistoryRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            bool lockTaken = false;
            try
            {
                await EnsureInitializedAsync();
                await _writeLock.WaitAsync();
                lockTaken = true;

                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO history_records
                        (timestamp_ticks, direction, raw_data, value_text, numeric_value)
                    VALUES
                        ($ticks, $direction, $rawData, $valueText, $numericValue);";

                command.Parameters.AddWithValue("$ticks", record.Timestamp.Ticks);
                command.Parameters.AddWithValue("$direction", record.Direction);
                command.Parameters.AddWithValue("$rawData", record.RawData);
                command.Parameters.AddWithValue("$valueText", record.Value);
                command.Parameters.AddWithValue("$numericValue",
                    (object?)record.NumericValue ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // 历史落盘失败不能中断实时采集
                Debug.WriteLine($"[History] 写入失败: {ex.Message}");
            }
            finally
            {
                if (lockTaken)
                {
                    _writeLock.Release();
                }
            }
        }

        public async Task<List<HistoryRecord>> QueryAsync(DateTime from, DateTime to, int limit = 5000)
        {
            await EnsureInitializedAsync();

            int safeLimit = Math.Clamp(limit, 1, 20000);
            var result = new List<HistoryRecord>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, timestamp_ticks, direction, raw_data, value_text, numeric_value
                FROM history_records
                WHERE timestamp_ticks BETWEEN $from AND $to
                ORDER BY timestamp_ticks DESC
                LIMIT $limit;";

            command.Parameters.AddWithValue("$from", from.Ticks);
            command.Parameters.AddWithValue("$to", to.Ticks);
            command.Parameters.AddWithValue("$limit", safeLimit);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new HistoryRecord
                {
                    Id = reader.GetInt64(0),
                    Timestamp = new DateTime(reader.GetInt64(1)),
                    Direction = reader.GetString(2),
                    RawData = reader.GetString(3),
                    Value = reader.GetString(4),
                    NumericValue = reader.IsDBNull(5) ? null : reader.GetDouble(5)
                });
            }

            return result;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    CREATE TABLE IF NOT EXISTS history_records (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        timestamp_ticks INTEGER NOT NULL,
                        direction TEXT NOT NULL,
                        raw_data TEXT NOT NULL,
                        value_text TEXT NOT NULL,
                        numeric_value REAL NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_history_records_ticks
                        ON history_records(timestamp_ticks);";
                await command.ExecuteNonQueryAsync();

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private static void EnsureNativeLibrary()
        {
            if (_nativeInitialized)
            {
                return;
            }

            lock (NativeInitLock)
            {
                if (_nativeInitialized)
                {
                    return;
                }

                SQLitePCL.Batteries_V2.Init();
                _nativeInitialized = true;
            }
        }
    }
}
