using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.Mvvm.Input;
using Prism.Common;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class SaxeyDiagramViewModel : AnalysisViewModelBase<SaxeyDiagramNode>
{
	public const string UniqueId = "GPM.CustomAnalysis.SaxeyDiagram.SaxeyDiagramViewModel";

	private readonly IRenderDataFactory renderDataFactory;
	private readonly IColorMap colorMap;
	private bool optionsChanged = false;

	private readonly AsyncRelayCommand runCommand;
	public ICommand RunCommand => runCommand;

	private readonly RelayCommand addLine;
	public ICommand AddLineCommand => addLine;

	private readonly RelayCommand removeLines;
	public ICommand RemoveLinesCommand => removeLines;

	public ObservableCollection<string> SelectedIons
	{
		get => Options.IonSelections;
		set => Options.IonSelections = value;
	}

	public List<int> ChargeCounts 
	{
		get => Options.ChargeCounts;
		private set => Options.ChargeCounts = value;
	}

	public ObservableObject<string> IonName { get; set; } = new();

	public string ListBoxSelection { get; set; } = "";

	private readonly RelayCommand listViewDoubleClick;
	public ICommand ListViewDoubleClick => listViewDoubleClick;

	public SaxeyDiagramOptions Options => Node?.Options ?? new ();

	public ObservableCollection<object> Tabs { get; } = new();
	
	private object? selectedTab;
	public object? SelectedTab
	{
		get => selectedTab;
		set => SetProperty(ref selectedTab, value);
	}

	public SaxeyDiagramViewModel(
		IAnalysisViewModelBaseServices services,
		IRenderDataFactory renderDataFactory,
		IColorMapFactory colorMapFactory) : base(services)
	{
		this.renderDataFactory = renderDataFactory;
		runCommand = new AsyncRelayCommand(OnRun, UpdateSelectedEventCountsEnabled);
		colorMap = CreateBrightColorMap(colorMapFactory);
		addLine = new RelayCommand(OnAddLine);
		removeLines = new RelayCommand(OnRemoveLines);
		listViewDoubleClick = new RelayCommand(OnListViewDoubleClick);
	}

	protected override void OnCreated(ViewModelCreatedEventArgs eventArgs)
	{
		base.OnCreated(eventArgs);
		if (Node is { } node)
		{
			node.Options.PropertyChanged += OptionsOnPropertyChanged;
			node.Options.EventSelections.PropertyChanged += OptionsEventSelectionsOnPropertyChanged;
		}
	}

	protected override void OnActivated(ViewModelActivatedEventArgs eventArgs)
	{
		base.OnActivated(eventArgs);
		if (!Tabs.Any())
			RunCommand.Execute(null);
	}

	private void OnAddLine()
	{
		if(SaxeyAddons.ValidateIonString(IonName.Value, out var match))
		{
			var ionFormula = SaxeyAddons.IonFormulaFromMatch(match, Node.Elements, out var charge);

			//if the ionFormula given is correct
			if(ionFormula != null)
			{
				string ionToAdd;
				if (!IonName.Value.Contains("+"))
					ionToAdd = IonName.Value + "+";
				else
					ionToAdd = IonName.Value;

				if (SelectedIons.Contains(ionToAdd))
					MessageBox.Show("Ion Already Added");
				else
				{
					SelectedIons.Add(ionToAdd);
					ChargeCounts.Add((int)charge!);
					//IonFormulas.Add(ionFormula);
					IonName.Value = "";
					runCommand.Execute(null);
				}
			}
		}
	}

	public void OnListViewDoubleClick()
	{
		var index = SelectedIons.IndexOf(ListBoxSelection);
		//if index not found
		if(index != -1)
		{
			SelectedIons.RemoveAt(index);
			ChargeCounts.RemoveAt(index);
			runCommand.Execute(null);
		}
	}

	private void OnRemoveLines()
	{
		SelectedIons.Clear();
		ChargeCounts.Clear();
		runCommand.Execute(null);
	}

	private async Task OnRun()
	{
		foreach (var item in Tabs)
		{
			if (item is IDisposable disposable)
				disposable.Dispose();
		}
		Tabs.Clear();

		// This shouldn't happen but check for safety
        if (Node is null) return;

		Node.IonLineAndChartSelection = SelectedIons.ToList();
		var data = await Node.Run();

		if (data == null)
		{
			var errorViewModel = new TextContentViewModel(
				"Error",
				"Missing section(s) in the APT file.  \"Multiplicity\" or \"pulse\" is required.");
			Tabs.Add(errorViewModel);
			SelectedTab = errorViewModel;
			return;
		}

		ReadOnlyMemory2D<float> saxeyData = (ReadOnlyMemory2D<float>)data[0];

		var elements = ((IElementDataSet)data[5]).Elements;

		var calculator = (IIonFormulaIsotopeCalculator)data[6];

		LinesOptions linesOptions = new LinesOptions()
		{
			elements = Node.Elements,
			calculator = calculator,
			selectedCharges = ChargeCounts,
			selectedSymbols = SelectedIons.ToList(),
			calculatorOptions = new IonFormulaIsotopeOptions() { MinimumIsotopeAbundance = .01 }
		};

		List<ILineRenderData> saxeyLines = new();
		if (Options.LineSelections.SaxeyDiagram)
		{
			List<Vector3[]> lineSaxeyPoints = SaxeyAddons.GetLinesSaxey(linesOptions, Options.MassExtent);
			foreach (var line in lineSaxeyPoints)
				saxeyLines.Add(renderDataFactory.CreateLine(line, Colors.Red, 3f));
		}

		var renderData = renderDataFactory.CreateHistogram2D(
			saxeyData,
			new Vector2(Options.Resolution, Options.Resolution),
			colorMap,
			new Vector2(Options.XMin, Options.YMin),
			minValue: CSaxeyDiagram.MinBinValueInclusive);
		var histogram2DViewModel = new Histogram2DContentViewModel(
			"Saxey Diagram",
			renderData, saxeyLines);
		Tabs.Add(histogram2DViewModel);

		ReadOnlyMemory2D<float> sqrtData = (ReadOnlyMemory2D<float>)data[1];
		float newResolution = (float)data[2];
		var sqrtRenderData = renderDataFactory.CreateHistogram2D(
			sqrtData,
			new Vector2(newResolution, newResolution),
			colorMap,
			new Vector2(Options.XMin, Options.YMin),
			minValue: CSaxeyDiagram.MinBinValueInclusive);
		

		ReadOnlyMemory<Vector2> multisData = (ReadOnlyMemory<Vector2>)data[3];
		var multisRenderData = renderDataFactory.CreateHistogram(multisData, Colors.Black, .5f);

		List<ILineRenderData> lines2D = new();
		if (Options.LineSelections.LinearizedDiagram)
		{
			List<Vector3[]> line2DPoints = SaxeyAddons.GetLines2D(linesOptions, Options.MassExtent);
			foreach (var line in line2DPoints)
				lines2D.Add(renderDataFactory.CreateLine(line, Colors.Red, 3f));
		}

		List<ILineRenderData> lines1D = new();
		if (Options.LineSelections.CalculatedMassSpectrum)
		{
			int maxHeight = (int)data[4];
			List<Vector3[]> line1DPoints = SaxeyAddons.GetLines1D(linesOptions, maxHeight);
			foreach (var line in line1DPoints)
				lines1D.Add(renderDataFactory.CreateLine(line, Colors.Red, 3f));
		}

		var saxeyAddonsViewModel = new Histogram2DHistogram1DSideBySideViewModel(
			"Time Space and Multi Atom Mass Spectrum",
			sqrtRenderData, multisRenderData, lines2D, lines1D);
		Tabs.Add(saxeyAddonsViewModel);

		DataTable rangeTable = SaxeyAddons.BuildRangeTable(linesOptions);
		var rangeTableViewModel = new RangeTableViewModel("Range Table", rangeTable);
		if (rangeTable.Rows.Count > 0)
			Tabs.Add(rangeTableViewModel);
		else
			Tabs.Add(new TextContentViewModel("Range Table", "Select at least one ion for the range table to be populated"));

		SelectedTab = histogram2DViewModel;
	}

	private void OptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(SaxeyDiagramOptions.PlotZeroAsWhite))
		{
			colorMap.OutOfRangeBottom = Options.PlotZeroAsWhite ? Colors.White : colorMap.Bottom;
		}
		else
		{
			optionsChanged = true;
			runCommand.NotifyCanExecuteChanged();
		}
	}

	private void OptionsEventSelectionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		=> optionsChanged = true;


	private bool UpdateSelectedEventCountsEnabled() => !Tabs.Any() || optionsChanged;

	private IColorMap CreateBrightColorMap(IColorMapFactory colorMapFactory) => colorMapFactory.CreateColorMap(
		outOfRangeTop: Colors.DeepPink,
		top: Colors.Red,
		colorStops: new IColorStop[]
		{
			colorMapFactory.CreateColorStop(0.7f, Colors.Orange),
			colorMapFactory.CreateColorStop(0.55f, Colors.Yellow),
			colorMapFactory.CreateColorStop(0.29f, Colors.Green),
		},
		bottom: Colors.Blue,
		outOfRangeBottom: Colors.Transparent,
		nanColor: Colors.Transparent);
}
