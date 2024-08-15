using System;
using System.Windows.Input;

namespace Secure.Messenger.WpfHost
{
    public class Command : ICommand
    {
        public delegate void CommandOnExecute(object parameter);
        public delegate bool CommandOnCanExecute(object parameter);

        private CommandOnExecute _execute;
        private CommandOnCanExecute _canExecute;

        public Command(CommandOnExecute onExecuteMethod, CommandOnCanExecute onCanExecuteMethod)
        {
            _execute = onExecuteMethod;
            _canExecute = onCanExecuteMethod;
        }

        #region ICommand Members

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            _execute.Invoke(parameter);
        }

        #endregion
    }
}
