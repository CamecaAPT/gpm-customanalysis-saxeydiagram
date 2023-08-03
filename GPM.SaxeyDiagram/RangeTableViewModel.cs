using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class RangeTableViewModel : IDisposable
{
	//public string Title { get; }

	public DataTable RangeTable { get; set; }

	public ObservableCollection<string> XIons { get; set; } = new();
	public ObservableCollection<string> YIons { get; set; } = new();

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

	public RangeTableViewModel(DataTable rangeTable)
	{
		//RangeTable = rangeTable;
		//Title = title;
		enterPressedX = new(AddIonX);
		enterPressedY = new(AddIonY);
		removeAllX = new(ClearX);
		removeAllY = new(ClearY);
		RangeTable = rangeTable;
	}

	public void Dispose()
	{
		//RangeTable.Dispose();
	}

	public void ClearX()
	{
		XIons.Clear();
	}

	public void ClearY()
	{
		YIons.Clear();
	}

	public void AddIonX()
	{
		XIons.Add(TextBoxX.Value);
		TextBoxX.Value = "";
	}

	public void RemoveIonX()
	{
		var index = XIons.IndexOf(SelectionX);
		XIons.RemoveAt(index);
	}

	public void AddIonY()
	{
		YIons.Add(TextBoxY.Value);
		TextBoxY.Value = "";
	}

	public void RemoveIonY()
	{
		var index = YIons.IndexOf(SelectionY);
		YIons.RemoveAt(index);
	}
}
