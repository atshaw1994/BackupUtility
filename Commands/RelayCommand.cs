using System.Windows.Input;

namespace BackupUtility.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _execute;
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Func<object?, bool>? _canExecute;

        // Constructor for synchronous commands
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Constructor for as ync commands
        public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        // The Execute method needs to handle both sync and async operations
        public async void Execute(object? parameter)
        {
            if (_execute != null) 
                _execute(parameter);
            else if (_executeAsync != null) 
                await _executeAsync(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Helper method to raise CanExecuteChanged manually if needed
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
