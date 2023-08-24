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
using System.Windows.Shapes;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.Mvvm.Input;
using Prism.Common;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class SaxeyDiagramViewModel : AnalysisViewModelBase<SaxeyDiagramNode>
{
	private LinesOptions? linesOptions = null;
	private int? histogramMaxHeight = null;
	private Histogram2DContentViewModel? saxeyViewModel = null;
	private Histogram2DHistogram1DSideBySideViewModel? sideBySideViewModel = null;

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

	public ObservableCollection<LineDefinition> SelectedIons
	{
		get => Options.IonSelections;
		set => Options.IonSelections = value;
	}

	public List<(int, int)> ChargeCounts 
	{
		get => Options.ChargeCounts;
		private set => Options.ChargeCounts = value;
	}

	private string ionName1 = "";
	private string ionName2 = "";
	public string IonName1
	{
		get => ionName1;
		set => SetProperty(ref ionName1, value);
	}
	public string IonName2
	{
		get => ionName2;
		set => SetProperty(ref ionName2, value);
	}

	public LineDefinition? ListBoxSelection { get; set; } = null;

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

	public Color PickColor(Color originalColor, Rectangle rect)
	{
		ColorPickerView window = new(originalColor);
		window.WindowStartupLocation = WindowStartupLocation.Manual;
		var point = rect.PointToScreen(Mouse.GetPosition(rect));
		window.Top = point.Y;
		window.Left = point.X;
		window.ShowDialog();
		return ((ColorPickerViewModel)window.DataContext).Color.Color;
	}

	public void UpdateLines()
	{
		if (saxeyViewModel == null || sideBySideViewModel == null || linesOptions == null || histogramMaxHeight == null)
			return;

		//update colors
		foreach(var line in Options.IonSelections)
		{
			line.Color = line.LineColor.Color;
		}

		//clear lines
		while (saxeyViewModel.Histogram2DRenderData.Count > 1)
			saxeyViewModel.Histogram2DRenderData.RemoveAt(1);
		while (sideBySideViewModel.Histogram2DRenderData.Count > 1)
			sideBySideViewModel.Histogram2DRenderData.RemoveAt(1);
		while (sideBySideViewModel.Histogram1DRenderData.Count > 1)
			sideBySideViewModel.Histogram1DRenderData.RemoveAt(1);

		//then add lines

		/*
		* Saxey Lines
		*/
		List<ILineRenderData> saxeyLines = new();
		if (Options.LineSelections.SaxeyDiagram)
		{
			(var lineSaxeyPoints, var saxeyLabels, var lineColors) = SaxeyAddons.GetLinesSaxey((LinesOptions)linesOptions, SelectedIons.ToList(), ChargeCounts, Options.MassExtent);
			for(int i=0; i<lineSaxeyPoints.Count; i++)
				saxeyLines.Add(renderDataFactory.CreateLine(lineSaxeyPoints[i], lineColors[i], 3f, saxeyLabels[i]));
		}
		saxeyViewModel.Histogram2DRenderData.AddRange(saxeyLines);

		/*
		 * Time Space Lines
		 */
		List<ILineRenderData> lines2D = new();
		if (Options.LineSelections.LinearizedDiagram)
		{
			(var line2DPoints, var lineNames, var lineColors) = SaxeyAddons.GetLines2D((LinesOptions)linesOptions, SelectedIons.ToList(), ChargeCounts, Options.MassExtent);
			for(int i=0; i<line2DPoints.Count; i++)
				lines2D.Add(renderDataFactory.CreateLine(line2DPoints[i], lineColors[i], 3f, lineNames[i]));
		}
		sideBySideViewModel.Histogram2DRenderData.AddRange(lines2D);

		/*
		 * Histogram Lines
		 */
		List<ILineRenderData> lines1D = new();
		if (Options.LineSelections.CalculatedMassSpectrum)
		{
			int maxHeight = (int)histogramMaxHeight;
			(var line1DPoints, var lineNames, var lineColors) = SaxeyAddons.GetLines1D((LinesOptions)linesOptions, SelectedIons.ToList(), ChargeCounts, maxHeight);
			for(int i=0; i<line1DPoints.Count; i++)
				lines1D.Add(renderDataFactory.CreateLine(line1DPoints[i], lineColors[i], 3f, lineNames[i]));
		}
		sideBySideViewModel.Histogram1DRenderData.AddRange(lines1D);
	}

	private void OnAddLine()
	{
		if(IonName1 == null || IonName2 == null || IonName1 == "" || IonName2 == "")
		{
			MessageBox.Show("Fill in both ion boxes");
			return;
		}

		if(SaxeyAddons.ValidateIonString(IonName1, out var match1, 1) && SaxeyAddons.ValidateIonString(IonName2, out var match2, 2))
		{
			var ionFormula1 = SaxeyAddons.IonFormulaFromMatch(match1, Node.Elements, out var charge1);
			var ionFormula2 = SaxeyAddons.IonFormulaFromMatch(match2, Node.Elements, out var charge2);

			//if the ionFormula given is correct
			if(ionFormula1 != null && ionFormula2 != null)
			{
				string ionToAdd1;
				if (!IonName1.Contains('+'))
					ionToAdd1 = IonName1 + "+";
				else
					ionToAdd1 = IonName1;

				string ionToAdd2;
				if (!IonName2.Contains('+'))
					ionToAdd2 = IonName2 + "+";
				else
					ionToAdd2 = IonName2;

				LineDefinition lineDef = new(ionToAdd1, ionToAdd2, Colors.Red);

				if (SelectedIons.Contains(lineDef))
					MessageBox.Show("Ion Pair Already Added");
				else
				{
					SelectedIons.Add(lineDef);
					ChargeCounts.Add(((int)charge1!, (int)charge2!));
					IonName1 = "";
					IonName2 = "";

					UpdateLines();
				}
			}
		}
	}

	public void OnListViewDoubleClick()
	{
		if(ListBoxSelection == null) return;
		var index = SelectedIons.IndexOf(ListBoxSelection);
		//if index found
		if (index != -1)
		{
			SelectedIons.RemoveAt(index);
			ChargeCounts.RemoveAt(index);
			UpdateLines();
		}
	}

	private void OnRemoveLines()
	{
		SelectedIons.Clear();
		ChargeCounts.Clear();
		UpdateLines();
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

		//Node.IonLineAndChartSelection = SelectedIons.ToList();
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

		this.linesOptions = new()
		{
			elements = Node.Elements,
			calculator = calculator,
			calculatorOptions = new IonFormulaIsotopeOptions() { MinimumIsotopeAbundance = .01 }
		};

		this.histogramMaxHeight = (int)data[4];

		/*
		 * Saxey Plot
		 */
		var renderData = renderDataFactory.CreateHistogram2D(
			saxeyData,
			new Vector2(Options.Resolution, Options.Resolution),
			colorMap,
			new Vector2(Options.XMin, Options.YMin),
			minValue: CSaxeyDiagram.MinBinValueInclusive,
			name: "Saxey Plot");
		var histogram2DViewModel = new Histogram2DContentViewModel(
			"Saxey Diagram",
			renderData);
		Tabs.Add(histogram2DViewModel);

		this.saxeyViewModel = histogram2DViewModel;


		/*
		 * Time Space Data
		 */
		ReadOnlyMemory2D<float> sqrtData = (ReadOnlyMemory2D<float>)data[1];
		float newResolution = (float)data[2];
		var sqrtRenderData = renderDataFactory.CreateHistogram2D(
			sqrtData,
			new Vector2(newResolution, newResolution),
			colorMap,
			new Vector2(Options.XMin, Options.YMin),
			minValue: CSaxeyDiagram.MinBinValueInclusive,
			name: "Time Space Plot");
		
		/*
		 * Histogram Data
		 */
		ReadOnlyMemory<Vector2> multisData = (ReadOnlyMemory<Vector2>)data[3];
		var multisRenderData = renderDataFactory.CreateHistogram(multisData, Colors.Black, .5f, name: "Multi-atom Histogram");


		var saxeyAddonsViewModel = new Histogram2DHistogram1DSideBySideViewModel(
			"Time Space and Multi Atom Mass Spectrum",
			sqrtRenderData, multisRenderData);
		Tabs.Add(saxeyAddonsViewModel);

		this.sideBySideViewModel = saxeyAddonsViewModel;

		/*
		 * Range Table
		 */
		var rangeTableView = new RangeTableView((LinesOptions)linesOptions, Options);
		Tabs.Add(rangeTableView);

		UpdateLines();

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
