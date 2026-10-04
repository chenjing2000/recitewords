using System.Windows.Input;
namespace ReciteWords.ViewModels;

public class SimpleCommand : ICommand
{
    private readonly Action execute;
    private readonly Func<bool>? canExecute;
    public SimpleCommand(Action execute, Func<bool>? canExecute = null) { this.execute = execute; this.canExecute = canExecute; }
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute == null || canExecute();
    public void Execute(object? parameter) { if (CanExecute(parameter)) execute(); }
    public void Refresh() { CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
}
