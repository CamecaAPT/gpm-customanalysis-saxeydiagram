using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Common;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class RangeTableViewModel : BindableBase, IDisposable
{
	public DataTable RangeTable { get; set; } = new();

	public SaxeyDiagramOptions Options { get; set; }

	private bool autoGenColumns = true;

	public bool AutoGenerateColumns
	{
		get => autoGenColumns;
		set => SetProperty(ref autoGenColumns, value);
	}
	
	public ObservableCollection<string> XIons 
	{ 
		get => Options.RangeTableXIons;
		set => Options.RangeTableXIons = value; 
	}
	public ObservableCollection<string> YIons
	{
		get => Options.RangeTableYIons;
		set => Options.RangeTableYIons = value; 
	}

	private List<int> xCharges
	{
		get => Options.RangeTableXCharges;
		set => Options.RangeTableXCharges = value;
	}
	private List<int> yCharges
	{
		get => Options.RangeTableYCharges;
		set => Options.RangeTableYCharges = value;
	}

	public string SelectionX { get; set; } = "";
	public string SelectionY { get; set; } = "";

	private string textBoxX = "";
	private string textBoxY = "";
	public string TextBoxX
	{
		get => textBoxX;
		set => SetProperty(ref textBoxX, value);
	}
	public string TextBoxY
	{
		get => textBoxY;
		set => SetProperty(ref textBoxY, value);
	}

	private readonly RelayCommand enterPressedX;
	private readonly RelayCommand enterPressedY;
	public ICommand EnterPressedX => enterPressedX;
	public ICommand EnterPressedY => enterPressedY;

	private readonly RelayCommand removeAllX;
	private readonly RelayCommand removeAllY;
	public ICommand RemoveAllX => removeAllX;
	public ICommand RemoveAllY => removeAllY;

	private LinesOptions linesOptions;

	public RangeTableViewModel(LinesOptions linesOptions, SaxeyDiagramOptions options)
	{
		enterPressedX = new(AddIonX);
		enterPressedY = new(AddIonY);
		removeAllX = new(ClearX);
		removeAllY = new(ClearY);
		this.linesOptions = linesOptions;
		Options = options;

		UpdateRangeTable();
	}

	private void UpdateRangeTable()
	{
		AutoGenerateColumns = false;

		RangeTable.Clear();
		RangeTable.Rows.Clear();
		RangeTable.Columns.Clear();

		SaxeyAddons.BuildRangeTable(RangeTable, linesOptions, XIons.ToList(), YIons.ToList(), xCharges, yCharges, Options.RangeTableDecimalPlaces);

		AutoGenerateColumns = true;
	}

	public void Dispose()
	{
		RangeTable.Dispose();
	}

	public void ClearX()
	{
		XIons.Clear();
		UpdateRangeTable();
	}

	public void ClearY()
	{
		YIons.Clear();
		UpdateRangeTable();
	}

	public void AddIonX()
	{
		if (TextBoxX == null)
			return;

		if(SaxeyAddons.ValidateIonString(TextBoxX, out var match))
		{
			var ionFormula = SaxeyAddons.IonFormulaFromMatch(match, linesOptions.elements, out var chargeCount);
			if(ionFormula != null && chargeCount != null)
			{
				string valueToAdd;
				if (!TextBoxX.Contains('+'))
					valueToAdd = TextBoxX + "+";
				else
					valueToAdd = TextBoxX;

				if(!XIons.Contains(valueToAdd))
				{
					XIons.Add(valueToAdd);
					xCharges.Add((int)chargeCount);
					TextBoxX = "";
					UpdateRangeTable();
				}
				else
					MessageBox.Show($"X Axis already contains {valueToAdd}");
			}
		}
	}

	public void RemoveIonX()
	{
		var index = XIons.IndexOf(SelectionX);
		if (index == -1) return;
		XIons.RemoveAt(index);
		xCharges.RemoveAt(index);
		UpdateRangeTable();
	}

	public void AddIonY()
	{
		if (TextBoxY == null)
			return;

		if (SaxeyAddons.ValidateIonString(TextBoxY, out var match))
		{
			var ionFormula = SaxeyAddons.IonFormulaFromMatch(match, linesOptions.elements, out var chargeCount);
			if (ionFormula != null && chargeCount != null)
			{
				string valueToAdd;
				if (!TextBoxY.Contains('+'))
					valueToAdd = TextBoxY + "+";
				else
					valueToAdd = TextBoxY;

				if (!YIons.Contains(valueToAdd))
				{
					YIons.Add(valueToAdd);
					yCharges.Add((int)chargeCount);
					TextBoxY = "";
					UpdateRangeTable();
				}
				else
					MessageBox.Show($"Y Axis already contains {valueToAdd}");
			}
		}
	}

	public void RemoveIonY()
	{
		var index = YIons.IndexOf(SelectionY);
		if(index == -1) return;
		YIons.RemoveAt(index);
		yCharges.RemoveAt(index);
		UpdateRangeTable();
	}
}
