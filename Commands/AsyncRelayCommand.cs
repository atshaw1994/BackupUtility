using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace BackupUtility.Commands
{
    /// <summary>
    /// An ICommand implementation for asynchronous operations.
    /// Prevents re-execution while the command is already running.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<object?, Task>? _parameterizedExecute;
        private readonly Func<bool>? _canExecute;
        private readonly Func<object?, bool>? _parameterizedCanExecute;
        private bool _isExecuting; // Tracks if the command is currently running

        // --- Constructors ---

        /// <summary>
        /// Initializes a new instance of the AsyncRelayCommand class.
        /// </summary>
        /// <param name="execute">The asynchronous action to execute.</param>
        /// <param name="canExecute">The function to determine if the command can execute (optional).</param>
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Initializes a new instance of the AsyncRelayCommand class with a parameter.
        /// </summary>
        /// <param name="execute">The asynchronous action to execute, accepting a parameter.</param>
        /// <param name="canExecute">The function to determine if the command can execute, accepting a parameter (optional).</param>
        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            _parameterizedExecute = execute ?? throw new ArgumentNullException(nameof(execute));
            _parameterizedCanExecute = canExecute;
        }

        // --- ICommand Members ---

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object? parameter)
        {
            // The command cannot execute if it's already in progress
            if (_isExecuting)
            {
                return false;
            }

            // Evaluate based on the specific canExecute function provided
            if (_canExecute != null)
            {
                return _canExecute();
            }
            if (_parameterizedCanExecute != null)
            {
                return _parameterizedCanExecute(parameter);
            }

            return true; // Default: command can always execute if no canExecute logic is provided
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged(); // Notify UI that CanExecute state has changed (e.g., button disables)

                if (_execute != null)
                {
                    await _execute();
                }
                else if (_parameterizedExecute != null)
                {
                    await _parameterizedExecute(parameter);
                }
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged(); // Notify UI that CanExecute state has changed (e.g., button enables)
            }
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event to indicate that the execute
        /// status of the command has changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            // Ensure the event is raised on the UI thread
            // This is crucial for WPF as UI updates must happen on the Dispatcher thread.
            if (CanExecuteChanged != null)
            {
                Dispatcher currentDispatcher = App.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                currentDispatcher.Invoke(() => CanExecuteChanged.Invoke(this, EventArgs.Empty));
            }
        }
    }
}
