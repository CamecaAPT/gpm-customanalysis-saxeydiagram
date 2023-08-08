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

	private bool autoGenColumns = true;

	public bool AutoGenerateColumns
	{
		get => autoGenColumns;
		set => SetProperty(ref autoGenColumns, value);
	}
	
	public ObservableCollection<string> XIons { get; set; } = new();
	public ObservableCollection<string> YIons { get; set; } = new();

	private List<int> xCharges = new();
	private List<int> yCharges = new();

	public string SelectionX { get; set; } = "";
	public string SelectionY { get; set; } = "";

	public ObservableObject<string> TextBoxX { get; set; } = new();
	public ObservableObject<string> TextBoxY { get; set; } = new();

	private readonly RelayCommand enterPressedX;
	private readonly RelayCommand enterPressedY;
	public ICommand EnterPressedX => enterPressedX;
	public ICommand EnterPressedY => enterPressedY;

	private readonly RelayCommand removeAllX;
	private readonly RelayCommand removeAllY;
	public ICommand RemoveAllX => removeAllX;
	public ICommand RemoveAllY => removeAllY;

	private LinesOptions linesOptions;

	public RangeTableViewModel(LinesOptions linesOptions)
	{
		enterPressedX = new(AddIonX);
		enterPressedY = new(AddIonY);
		removeAllX = new(ClearX);
		removeAllY = new(ClearY);
		this.linesOptions = linesOptions;
	}

	private void DummyRangeTable(int rows, int cols)
	{
		AutoGenerateColumns = false;
		RangeTable.Clear();
		RangeTable.Rows.Clear();
		RangeTable.Columns.Clear();

		for (int col = 0; col < cols; col++)
			RangeTable.Columns.Add();
		int totalCount = 0;
		for (int row = 0; row<rows; row++)
		{
			List<object> rowList = new();
			for(int col=0; col<cols; col++)
			{
				rowList.Add($"{totalCount++}");
			}
			RangeTable.Rows.Add(rowList.ToArray());
		}

		AutoGenerateColumns = true;
	}

	private void UpdateRangeTable()
	{
		AutoGenerateColumns = false;

		RangeTable.Clear();
		RangeTable.Rows.Clear();
		RangeTable.Columns.Clear();

		SaxeyAddons.BuildRangeTable(RangeTable, linesOptions, XIons.ToList(), YIons.ToList(), xCharges, yCharges);

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
		if (TextBoxX.Value == null)
			return;

		if(SaxeyAddons.ValidateIonString(TextBoxX.Value, out var match))
		{
			var ionFormula = SaxeyAddons.IonFormulaFromMatch(match, linesOptions.elements, out var chargeCount);
			if(ionFormula != null && chargeCount != null)
			{
				string valueToAdd;
				if (!TextBoxX.Value.Contains('+'))
					valueToAdd = TextBoxX.Value + "+";
				else
					valueToAdd = TextBoxX.Value;

				if(!XIons.Contains(valueToAdd))
				{
					XIons.Add(valueToAdd);
					xCharges.Add((int)chargeCount);
					TextBoxX.Value = "";
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
		XIons.RemoveAt(index);
		xCharges.RemoveAt(index);
		UpdateRangeTable();
	}

	public void AddIonY()
	{
		if (TextBoxY.Value == null)
			return;

		if (SaxeyAddons.ValidateIonString(TextBoxY.Value, out var match))
		{
			var ionFormula = SaxeyAddons.IonFormulaFromMatch(match, linesOptions.elements, out var chargeCount);
			if (ionFormula != null && chargeCount != null)
			{
				string valueToAdd;
				if (!TextBoxY.Value.Contains('+'))
					valueToAdd = TextBoxY.Value + "+";
				else
					valueToAdd = TextBoxY.Value;

				if (!YIons.Contains(valueToAdd))
				{
					YIons.Add(valueToAdd);
					yCharges.Add((int)chargeCount);
					TextBoxY.Value = "";
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
		YIons.RemoveAt(index);
		yCharges.RemoveAt(index);
		UpdateRangeTable();
	}
}
