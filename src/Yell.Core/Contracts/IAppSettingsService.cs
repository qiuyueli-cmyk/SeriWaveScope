using Yell.Core.Models;

namespace Yell.Core.Contracts
{
    public interface IAppSettingsService
    {
        AppTheme LoadTheme();

        void SaveTheme(AppTheme theme);
    }
}
