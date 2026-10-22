using System;
using System.Windows.Input;

namespace NetworkService.Helpers
{
    /// <summary>
    /// Implementacija ICommand interfejsa bez parametra.
    /// Koristi se za vezivanje dugmadi (Button.Command) na metode u ViewModel-u.
    /// </summary>
    public class MyICommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public MyICommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        // Rucno okidanje ponovne provere CanExecute (npr. kada se promeni stanje selekcije).
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Verzija ICommand-a koja prima parametar (CommandParameter iz XAML-a).
    /// </summary>
    public class MyICommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public MyICommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
