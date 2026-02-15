using System;
using System.Collections.Generic;
using System.Text;

namespace UTS.WPF.MVVM
{
    using System.Windows.Input;

    public sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<object?, bool>? _canExecute = canExecute;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
