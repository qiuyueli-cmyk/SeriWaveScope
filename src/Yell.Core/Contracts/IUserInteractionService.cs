namespace Yell.Core.Contracts
{
    /// <summary>
    /// 把 MessageBox / SaveFileDialog 收口到一个服务里，
    /// 这样 ViewModel 不直接依赖 WPF 的弹窗 API，方便测试和替换。
    /// </summary>
    public interface IUserInteractionService
    {
        void ShowInfo(string message, string title = "提示");

        void ShowWarning(string message, string title = "提示");

        void ShowError(string message, string title = "错误");

        bool Confirm(string message, string title = "确认");

        string? PickSaveFile(string filter, string defaultFileName, string extension);
    }
}
