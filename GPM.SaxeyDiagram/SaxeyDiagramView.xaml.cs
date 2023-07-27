using System.Windows.Controls;

namespace GPM.CustomAnalysis.SaxeyDiagram;
/// <summary>
/// Interaction logic for SaxeyDiagramView.xaml
/// </summary>
public partial class SaxeyDiagramView : UserControl
{
	public SaxeyDiagramView()
	{
		InitializeComponent();
	}

	private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		var dataContext = (SaxeyDiagramViewModel)DataContext;
		dataContext.OnListViewDoubleClick();
    }
}
