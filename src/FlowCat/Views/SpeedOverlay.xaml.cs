using System.Windows;
using System.Windows.Input;

namespace FlowCat.Views;

public partial class SpeedOverlay : Window
{
    public SpeedOverlay()
    {
        InitializeComponent();
        PositionNearTaskbar();
    }

    void PositionNearTaskbar()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 8;
        Top = workArea.Bottom - Height - 4;
    }

    void OnDrag(object sender, MouseButtonEventArgs e) => DragMove();

    void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        var menu = new System.Windows.Controls.ContextMenu();
        var hideItem = new System.Windows.Controls.MenuItem { Header = "隐藏悬浮窗" };
        hideItem.Click += (_, _) => Hide();
        menu.Items.Add(hideItem);
        menu.IsOpen = true;
    }
}
