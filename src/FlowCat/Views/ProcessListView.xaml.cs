using System.Windows;
using System.Windows.Controls;
using FlowCat.ViewModels;

namespace FlowCat.Views;

public partial class ProcessListView : UserControl
{
    public ProcessListView()
    {
        InitializeComponent();
    }

    void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProcessListViewModel vm)
            vm.NetworkFilter = FilterBox.SelectedIndex;
    }
}
