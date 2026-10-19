using System.Windows;
using System.Windows.Controls;
using FlowCat.Services;
using FlowCat.ViewModels;
using Wpf.Ui.Controls;

namespace FlowCat.Views;

public partial class MainWindow : FluentWindow
{
    readonly LedgerService _ledger = new();
    readonly ProcessListViewModel _processVm;
    readonly StatsViewModel _statsVm;

    public MainWindow()
    {
        InitializeComponent();

        _processVm = new ProcessListViewModel();
        _processVm.SetLedger(_ledger);
        ProcessPanel.DataContext = _processVm;

        _statsVm = new StatsViewModel(_ledger);
        StatsPanel.DataContext = _statsVm;

        ConnLabel.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
            new System.Windows.Data.Binding(nameof(ProcessListViewModel.ConnectionLabel))
            { Source = _processVm });

        _processVm.Initialize();

        if (DataContext is TrayViewModel vm)
            vm.ShowWindowRequested += () => ShowAndActivate();

        MouseLeftButtonDown += (_, _) => DragMove();
        Deactivated += (_, _) => Hide();
    }

    void ShowAndActivate()
    {
        if (TabStats.IsChecked == true)
            _statsVm.Refresh();

        PositionNearTray();
        Show();
        Activate();
    }

    void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 8;
        Top = workArea.Bottom - Height - 8;
    }

    void OnCloseClick(object sender, RoutedEventArgs e) => Hide();

    void OnTabChanged(object sender, RoutedEventArgs e)
    {
        if (TabProcess.IsChecked == true)
        {
            ProcessPanel.Visibility = Visibility.Visible;
            StatsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            ProcessPanel.Visibility = Visibility.Collapsed;
            StatsPanel.Visibility = Visibility.Visible;
            _statsVm.Refresh();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _ledger.Save();
        _ledger.Dispose();
        _processVm.Dispose();
        base.OnClosed(e);
    }
}
