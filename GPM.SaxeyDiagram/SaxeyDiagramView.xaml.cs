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

	private void CheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
	{
		((SaxeyDiagramViewModel)DataContext).UpdateLines();
	}

	private void CheckBox_Unchecked(object sender, System.Windows.RoutedEventArgs e)
	{
		((SaxeyDiagramViewModel)DataContext).UpdateLines();
	}
}
