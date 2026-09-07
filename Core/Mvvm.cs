using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FactorialApp.Core;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
    protected void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute; private readonly Func<bool>? _can;
    public RelayCommand(Action execute, Func<bool>? can = null) { _execute = execute; _can = can; }
    public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute; private readonly Predicate<T>? _can;
    public RelayCommand(Action<T> execute, Predicate<T>? can = null) { _execute = execute; _can = can; }
    public bool CanExecute(object? parameter) => parameter is T t && (_can?.Invoke(t) ?? true);
    public void Execute(object? parameter) { if (parameter is T t) _execute(t); }
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute; private readonly Func<bool>? _can; private bool _running;
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? can = null) { _execute = execute; _can = can; }
    public bool CanExecute(object? parameter) => !_running && (_can?.Invoke() ?? true);
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        try { _running = true; CommandManager.InvalidateRequerySuggested(); await _execute(); }
        finally { _running = false; CommandManager.InvalidateRequerySuggested(); }
    }
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}
