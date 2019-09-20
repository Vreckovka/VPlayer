using System;
using System.Windows.Input;

namespace VCore
{
    public class ActionCommand<TArgument> : ICommand 
    {
        private readonly Action<TArgument> _action;

        public ActionCommand(Action<TArgument> action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action((TArgument)parameter);
        }

        public event EventHandler CanExecuteChanged;
    }


    public class ActionCommand : ICommand
    {
        private readonly Action _action;

        public ActionCommand(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public event EventHandler CanExecuteChanged;
    }
}
