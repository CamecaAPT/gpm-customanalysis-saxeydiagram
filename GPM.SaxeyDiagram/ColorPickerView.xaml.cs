using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace GPM.CustomAnalysis.SaxeyDiagram;

/// <summary>
/// Interaction logic for ColorPickerWindow.xaml
/// </summary>
public partial class ColorPickerView : Window
{
	private Color origColor;

    public ColorPickerView(Color color)
    {
        InitializeComponent();
		origColor = color;
		DataContext = new ColorPickerViewModel(color);
    }

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		this.Close();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		((ColorPickerViewModel)DataContext).Color.Color = origColor;
		this.Close();
	}
}
