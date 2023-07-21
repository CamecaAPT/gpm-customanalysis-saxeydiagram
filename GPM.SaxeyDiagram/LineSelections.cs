using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPM.CustomAnalysis.SaxeyDiagram;

public class LineSelections : BindableBase
{
	private void UpdateAll()
	{
		all = saxeyDiagram && linearizedDiagram && calculatedMassSpectrum;
		RaisePropertyChanged(nameof(All));
	}

	private bool saxeyDiagram = false;
	public bool SaxeyDiagram
	{
		get => saxeyDiagram;
		set => SetProperty(ref saxeyDiagram, value, UpdateAll);
	}

	private bool linearizedDiagram = true;
	public bool LinearizedDiagram
	{
		get => linearizedDiagram;
		set => SetProperty(ref linearizedDiagram, value, UpdateAll);
	}

	private bool calculatedMassSpectrum = true;
	public bool CalculatedMassSpectrum
	{
		get => calculatedMassSpectrum;
		set => SetProperty(ref calculatedMassSpectrum, value, UpdateAll);
	}

	private bool all;
	public bool All
	{
		get => all;
		set
		{
			SetProperty(ref all, value);
			SaxeyDiagram = value;
			LinearizedDiagram = value;
			CalculatedMassSpectrum = value;
		}
	}
}
