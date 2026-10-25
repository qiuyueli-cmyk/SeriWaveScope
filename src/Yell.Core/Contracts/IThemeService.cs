using Yell.Core.Models;

namespace Yell.Core.Contracts
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }

        void ApplyTheme(AppTheme theme);

        AppTheme ToggleTheme();
    }
}
