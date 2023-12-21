using System;
using System.Windows.Input;

namespace DesktopMagic.Helpers;

public class CommandHandler(Action action, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action action = action;
    private readonly Func<bool> canExecute = canExecute ?? new Func<bool>(() => true);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return canExecute.Invoke();
    }

    public void Execute(object? parameter)
    {
        action();
    }
}
