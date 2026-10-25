using System.Windows;
using Prism.Events;
using Yell.Core.Contracts;
using Yell.Core.Events;
using Yell.Core.Models;

namespace Yell.Services
{
    /// <summary>
    /// 主题切换：替换 Application.Resources 中的颜色字典。
    /// 控件样式全部使用 DynamicResource，所以换字典即可实时生效。
    /// </summary>
    public sealed class ThemeService : IThemeService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IAppSettingsService _settingsService;

        public ThemeService(IEventAggregator eventAggregator, IAppSettingsService settingsService)
        {
            _eventAggregator = eventAggregator;
            _settingsService = settingsService;
            CurrentTheme = _settingsService.LoadTheme();
        }

        public AppTheme CurrentTheme { get; private set; }

        public void ApplyTheme(AppTheme theme)
        {
            var resources = Application.Current?.Resources;
            if (resources is null)
            {
                return;
            }

            string fileName = theme == AppTheme.Dark ? "Colors.Dark.xaml" : "Colors.Light.xaml";
            var themeDictionary = new ResourceDictionary
            {
                Source = new Uri(
                    $"pack://application:,,,/Yell;component/Themes/{fileName}",
                    UriKind.Absolute)
            };

            var dictionaries = resources.MergedDictionaries;
            int index = -1;
            for (int i = 0; i < dictionaries.Count; i++)
            {
                string? source = dictionaries[i].Source?.OriginalString;
                if (source is not null &&
                    source.Contains("Themes/Colors.", StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                dictionaries[index] = themeDictionary;
            }
            else
            {
                dictionaries.Insert(0, themeDictionary);
            }

            CurrentTheme = theme;
            _settingsService.SaveTheme(theme);
            _eventAggregator.GetEvent<ThemeChangedEvent>().Publish(theme);
        }

        public AppTheme ToggleTheme()
        {
            AppTheme next = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            ApplyTheme(next);
            return next;
        }
    }
}
