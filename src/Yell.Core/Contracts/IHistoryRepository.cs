using Yell.Core.Models;

namespace Yell.Core.Contracts
{
    public interface IHistoryRepository
    {
        string DatabasePath { get; }

        Task InsertAsync(HistoryRecord record);

        Task<List<HistoryRecord>> QueryAsync(DateTime from, DateTime to, int limit = 5000);
    }
}
