using System.Windows.Input;

namespace BackupUtility
{
    public class RelayCommand(Action<object> execute, Predicate<object>? canExecute = null) : ICommand
    {
        private readonly Action<object> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private EventHandler? _canExecuteChanged;

        public event EventHandler? CanExecuteChanged
        {
            add { _canExecuteChanged += value; CommandManager.RequerySuggested += value; }
            remove { _canExecuteChanged -= value; CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => canExecute == null || canExecute(parameter);

        public void Execute(object? parameter) => _execute(obj: parameter);

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
