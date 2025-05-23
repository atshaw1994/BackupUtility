using System.Threading.Tasks;

namespace BackupUtility.Views.Services
{
    public interface IDialogService
    {
        void ShowErrorMessage(string message, string title);
        void ShowInfoMessage(string message, string title);
        bool ShowConfirmationMessage(string message, string title);
    }
}
