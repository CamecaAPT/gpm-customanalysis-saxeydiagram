﻿using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace GPM.CustomAnalysis.SaxeyDiagram;

public class LineDefinition : BindableBase, IEquatable<LineDefinition>
{
	public LineDefinition(string ion1, string ion2, Color lineColor, bool isVisible = true)
	{
		Ion1 = ion1;
		Ion2 = ion2;
		IsVisible = isVisible;
		color = lineColor;
		LineColor = new(lineColor);
		UpdateDisplay();
	}

	public LineDefinition()
	{
		Ion1 = "";
		Ion2 = "";
		IsVisible = true;
		UpdateDisplay();
	}

	private string display = "";
	public string Display
	{
		get => display;
		set => SetProperty(ref display, value);
	}

	private string ion1 = "";
	private string ion2 = "";
	public string Ion1
	{
		get => ion1;
		set
		{
			SetProperty(ref ion1, value);
			UpdateDisplay();
		}
	}
	public string Ion2
	{
		get => ion2;
		set
		{
			SetProperty(ref ion2, value);
			UpdateDisplay();
		}
	}

	private bool isDissociation = false;
	public bool IsDissociation
	{
		get => isDissociation;
		set => SetProperty(ref isDissociation, value);
	}

	private bool isVisible = true;
	public bool IsVisible
	{
		get => isVisible;
		set => SetProperty(ref isVisible, value);
	}

	private Color color;
	public Color Color
	{
		get => color;
		set => SetProperty(ref color, value);
	}

	[XmlIgnore]
	private SolidColorBrush lineColor = new(Colors.Black);
	[XmlIgnore]
	public SolidColorBrush LineColor
	{
		get => lineColor;
		set => SetProperty(ref lineColor, value);
	}

	public void SetColor(Color color)
	{
		LineColor.Color = color;
		Color = color;
	}

	private void UpdateDisplay()
	{
		display = $"({ion1}, {ion2})";
		RaisePropertyChanged(nameof(display));
	}

	public bool Equals(LineDefinition? other)
	{
		if (other == null)
			return false;
		LineDefinition otherLine = (LineDefinition)other;
		return (this.ion1 == otherLine.ion1 && this.ion2 == otherLine.ion2) || (this.ion2 == otherLine.ion1 && this.ion1 == otherLine.ion2);
	}
}
