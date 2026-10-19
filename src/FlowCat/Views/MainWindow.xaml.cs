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
    readonly DiskIoViewModel _diskVm;

    public MainWindow()
    {
        InitializeComponent();

        _processVm = new ProcessListViewModel();
        _processVm.SetLedger(_ledger);
        ProcessPanel.DataContext = _processVm;

        _statsVm = new StatsViewModel(_ledger);
        StatsPanel.DataContext = _statsVm;

        _diskVm = new DiskIoViewModel();
        _diskVm.SetLedger(_ledger);
        DiskPanel.DataContext = _diskVm;

        ConnLabel.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
            new System.Windows.Data.Binding(nameof(ProcessListViewModel.ConnectionLabel))
            { Source = _processVm });

        _processVm.Initialize();
        _diskVm.Start();

        if (DataContext is TrayViewModel vm)
            vm.ShowWindowRequested += () => ShowAndActivate();

        MouseLeftButtonDown += (_, _) => DragMove();
        Deactivated += (_, _) => Hide();
    }

    void ShowAndActivate()
    {
        if (TabStats.IsChecked == true)
            _statsVm.SetVisible(true);

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
        ProcessPanel.Visibility = TabProcess.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        StatsPanel.Visibility = TabStats.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        DiskPanel.Visibility = TabDisk.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        _statsVm.SetVisible(TabStats.IsChecked == true);
    }

    protected override void OnClosed(EventArgs e)
    {
        _ledger.Save();
        _ledger.Dispose();
        _processVm.Dispose();
        _diskVm.Dispose();
        _statsVm.Dispose();
        base.OnClosed(e);
    }
}
