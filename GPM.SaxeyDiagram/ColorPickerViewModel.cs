using CommunityToolkit.Mvvm.Input;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace GPM.CustomAnalysis.SaxeyDiagram;

public class ColorPickerViewModel : BindableBase
{
	private int red;
	public int Red
	{
		get => red;
		set
		{
			SetProperty(ref red, value);
			UpdateColor();
		}
	}

	private int green;
	public int Green
	{
		get => green;
		set
		{
			SetProperty(ref green, value);
			UpdateColor();
		}
	}

	private int blue;
	public int Blue
	{
		get => blue;
		set
		{
			SetProperty(ref blue, value);
			UpdateColor();
		}
	}

	private SolidColorBrush color;
	public SolidColorBrush Color
	{
		get => color;
		set => SetProperty(ref color, value);
	}

	public ColorPickerViewModel(Color color)
	{
		this.color = new(color);
		this.red = color.R;
		this.green = color.G;
		this.blue = color.B;
	}

	private void UpdateColor()
	{
		Color.Color = System.Windows.Media.Color.FromRgb((byte)red, (byte)green, (byte)blue);
	}
}
