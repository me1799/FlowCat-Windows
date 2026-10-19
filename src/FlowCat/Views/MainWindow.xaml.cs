using System.Windows;
using System.Windows.Controls;
using FlowCat.Services;
using FlowCat.ViewModels;
using Wpf.Ui.Controls;

namespace FlowCat.Views;

public partial class MainWindow : FluentWindow
{
    readonly LedgerService _ledger = new();
    readonly DnsMonitor _dns = new();
    readonly ProcessListViewModel _processVm;
    readonly StatsViewModel _statsVm;
    readonly DiskIoViewModel _diskVm;
    readonly SpeedOverlay _overlay;

    bool _pinned;

    public MainWindow()
    {
        InitializeComponent();

        _processVm = new ProcessListViewModel();
        _processVm.SetLedger(_ledger);
        _processVm.SetDns(_dns);
        ProcessPanel.DataContext = _processVm;

        _statsVm = new StatsViewModel(_ledger, _dns);
        StatsPanel.DataContext = _statsVm;

        _diskVm = new DiskIoViewModel();
        _diskVm.SetLedger(_ledger);
        DiskPanel.DataContext = _diskVm;

        ConnLabel.SetBinding(System.Windows.Controls.TextBlock.TextProperty,
            new System.Windows.Data.Binding(nameof(ProcessListViewModel.ConnectionLabel))
            { Source = _processVm });

        AutoStartMenuItem.IsChecked = AutoStartService.IsEnabled;

        _overlay = new SpeedOverlay();
        _overlay.DataContext = Resources["TrayVM"];
        _overlay.Show();

        _processVm.Initialize();
        _diskVm.Start();

        if (DataContext is TrayViewModel vm)
            vm.ShowWindowRequested += () => ShowAndActivate();

        MouseLeftButtonDown += (_, _) => DragMove();
        Deactivated += (_, _) => { if (!_pinned) Hide(); };
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

    void OnPinClick(object sender, RoutedEventArgs e)
    {
        _pinned = !_pinned;
        Topmost = _pinned;
        PinBtn.Opacity = _pinned ? 1.0 : 0.4;
    }

    void OnAutoStartToggle(object sender, RoutedEventArgs e)
    {
        AutoStartService.Toggle();
        AutoStartMenuItem.IsChecked = AutoStartService.IsEnabled;
    }

    void OnOverlayToggle(object sender, RoutedEventArgs e)
    {
        if (_overlay.IsVisible)
            _overlay.Hide();
        else
            _overlay.Show();
        OverlayMenuItem.IsChecked = _overlay.IsVisible;
    }

    void OnTabChanged(object sender, RoutedEventArgs e)
    {
        ProcessPanel.Visibility = TabNetwork.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        DiskPanel.Visibility = TabDisk.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        StatsPanel.Visibility = TabStats.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        _statsVm.SetVisible(TabStats.IsChecked == true);
    }

    protected override void OnClosed(EventArgs e)
    {
        _overlay.Close();
        _ledger.Save();
        _ledger.Dispose();
        _processVm.Dispose();
        _diskVm.Dispose();
        _statsVm.Dispose();
        _dns.Dispose();
        base.OnClosed(e);
    }
}
