using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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

	private void Rectangle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (!(sender is Rectangle rect))
			return;

		if (!(rect.Fill is SolidColorBrush colorBrush))
			return;


		colorBrush.Color = ((SaxeyDiagramViewModel)DataContext).PickColor(colorBrush.Color, rect);

		((SaxeyDiagramViewModel)DataContext).UpdateLines();
	}
}
