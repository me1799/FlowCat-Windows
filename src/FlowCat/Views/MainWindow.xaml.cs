using System.Windows;
using FlowCat.ViewModels;
using Wpf.Ui.Controls;

namespace FlowCat.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is TrayViewModel vm)
            vm.ShowWindowRequested += () => ShowAndActivate();

        MouseLeftButtonDown += (_, _) => DragMove();
        Deactivated += (_, _) => Hide();
    }

    void ShowAndActivate()
    {
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
}
