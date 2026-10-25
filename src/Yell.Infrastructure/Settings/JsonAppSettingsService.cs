using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Yell.Core.Contracts;
using Yell.Core.Models;

namespace Yell.Infrastructure.Settings
{
    /// <summary>
    /// 应用设置保存到 %LocalAppData%\SeriWaveScope\settings.json，
    /// 不污染程序安装目录。
    /// </summary>
    public sealed class JsonAppSettingsService : IAppSettingsService
    {
        private sealed class AppSettings
        {
            public AppTheme Theme { get; set; } = AppTheme.Dark;
        }

        private readonly string _settingsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeriWaveScope");

        private readonly string _settingsPath;

        public JsonAppSettingsService()
        {
            _settingsPath = Path.Combine(_settingsFolder, "settings.json");
        }

        public AppTheme LoadTheme()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    return AppTheme.Dark;
                }

                string json = File.ReadAllText(_settingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings?.Theme ?? AppTheme.Dark;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] 读取失败: {ex.Message}");
                return AppTheme.Dark;
            }
        }

        public void SaveTheme(AppTheme theme)
        {
            try
            {
                Directory.CreateDirectory(_settingsFolder);
                string json = JsonSerializer.Serialize(
                    new AppSettings { Theme = theme },
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] 保存失败: {ex.Message}");
            }
        }
    }
}
