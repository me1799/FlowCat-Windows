using System.Windows;
using System.Windows.Controls;

namespace FlowCat.Views;

public partial class StatsView : UserControl
{
    public StatsView()
    {
        InitializeComponent();
    }

    void OnStatsFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NetStatsPanel == null || DiskStatsPanel == null) return;

        if (StatsFilter.SelectedIndex == 0)
        {
            NetStatsPanel.Visibility = Visibility.Visible;
            DiskStatsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            NetStatsPanel.Visibility = Visibility.Collapsed;
            DiskStatsPanel.Visibility = Visibility.Visible;
        }
    }
}
