using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GPM.CustomAnalysis.SaxeyDiagram;
/// <summary>
/// Interaction logic for RangeTableView.xaml
/// </summary>
public partial class RangeTableView : UserControl
{
	public string Title { get; }

	public RangeTableView(string title, DataTable dataTable)
	{
		InitializeComponent();
		Title = title;
		DataContext = new RangeTableViewModel(dataTable);
	}

	private void ListBox_MouseDoubleClickX(object sender, MouseButtonEventArgs e)
	{
		((RangeTableViewModel)DataContext).RemoveIonX();
	}

	private void ListBox_MouseDoubleClickY(object sender, MouseButtonEventArgs e)
	{
		((RangeTableViewModel)DataContext).RemoveIonY();
	}
}
