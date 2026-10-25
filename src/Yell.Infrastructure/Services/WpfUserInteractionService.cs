using System.Windows;
using Microsoft.Win32;
using Yell.Core.Contracts;

namespace Yell.Infrastructure.Services
{
    /// <summary>
    /// ViewModel 需要的弹窗能力由 Shell 层统一用 WPF 实现。
    /// </summary>
    public sealed class WpfUserInteractionService : IUserInteractionService
    {
        public void ShowInfo(string message, string title = "提示") =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public void ShowWarning(string message, string title = "提示") =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        public void ShowError(string message, string title = "错误") =>
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public bool Confirm(string message, string title = "确认") =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;

        public string? PickSaveFile(string filter, string defaultFileName, string extension)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName,
                DefaultExt = extension,
                AddExtension = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
