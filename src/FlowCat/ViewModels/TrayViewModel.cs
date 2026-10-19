using System.Windows.Input;
using FlowCat.Models;
using FlowCat.Services;

namespace FlowCat.ViewModels;

public sealed class TrayViewModel : ViewModelBase, IDisposable
{
    readonly GlobalSpeedMonitor _monitor;

    string _downloadSpeed = "0 B/s";
    string _uploadSpeed = "0 B/s";
    string _tooltip = "小微Cat";

    public string DownloadSpeed { get => _downloadSpeed; private set => SetField(ref _downloadSpeed, value); }
    public string UploadSpeed { get => _uploadSpeed; private set => SetField(ref _uploadSpeed, value); }
    public string Tooltip { get => _tooltip; private set => SetField(ref _tooltip, value); }

    public ICommand ShowWindowCommand { get; }
    public ICommand ExitCommand { get; }

    public event Action? ShowWindowRequested;

    public TrayViewModel()
    {
        _monitor = new GlobalSpeedMonitor();
        _monitor.SpeedUpdated += OnSpeedUpdated;
        _monitor.Start();

        ShowWindowCommand = new RelayCommand(_ => ShowWindowRequested?.Invoke());
        ExitCommand = new RelayCommand(_ => System.Windows.Application.Current?.Shutdown());
    }

    void OnSpeedUpdated(SpeedRate rate)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            DownloadSpeed = rate.DownloadDisplay;
            UploadSpeed = rate.UploadDisplay;
            Tooltip = $"↓ {rate.DownloadDisplay}  ↑ {rate.UploadDisplay}";
        });
    }

    public void Dispose() => _monitor.Dispose();
}

public class RelayCommand : ICommand
{
    readonly Action<object?> _execute;
    readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
}
