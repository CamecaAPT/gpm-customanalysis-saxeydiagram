using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

	//private ObservableCollection<(float, float)> massToChargePairs;
	//public ObservableCollection<(float, float)> MassToChargePairs => massToChargePairs;

	public ObservableCollection<string> SelectedIons { get; set; } = new();

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
		//massToChargePairs = new();
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
		if(SaxeyAddons.ValidateIon(IonName.Value, SelectedIons.ToList()))
		{
			SelectedIons.Add(IonName.Value);
			IonName.Value = "";
			runCommand.Execute(null);
		}
	}

	public void OnListViewDoubleClick()
	{
		if(SelectedIons.Contains(ListBoxSelection))
		{
			SelectedIons.Remove(ListBoxSelection);
			runCommand.Execute(null);
		}
	}

	private void OnRemoveLines()
	{
		SelectedIons.Clear();
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

		List<ILineRenderData> saxeyLines = new();
		if (Options.LineSelections.SaxeyDiagram)
		{
			//List<Vector3[]> lineSaxeyPoints = SaxeyAddons.GetLinesSaxey(Options.MassExtent);
			//List<Vector3[]> lineSaxeyPoints = SaxeyAddons.GetLinesSaxey(massToChargePairs.ToList(), Options.MassExtent);
			List<Vector3[]> lineSaxeyPoints = SaxeyAddons.GetLinesSaxey(SelectedIons.ToList(), Options.MassExtent);
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
			//List<Vector3[]> line2DPoints = SaxeyAddons.GetLines2D(Options.MassExtent);
			//List<Vector3[]> line2DPoints = SaxeyAddons.GetLines2D(massToChargePairs.ToList(), Options.MassExtent);
			List<Vector3[]> line2DPoints = SaxeyAddons.GetLines2D(SelectedIons.ToList(), Options.MassExtent);
			foreach (var line in line2DPoints)
				lines2D.Add(renderDataFactory.CreateLine(line, Colors.Red, 3f));
		}

		List<ILineRenderData> lines1D = new();
		if (Options.LineSelections.CalculatedMassSpectrum)
		{
			int maxHeight = (int)data[4];

			//List<Vector3[]> line1DPoints = SaxeyAddons.GetLines1D(maxHeight);
			//List<Vector3[]> line1DPoints = SaxeyAddons.GetLines1D(massToChargePairs.ToList(), maxHeight);
			List<Vector3[]> line1DPoints = SaxeyAddons.GetLines1D(SelectedIons.ToList(), maxHeight);
			foreach (var line in line1DPoints)
				lines1D.Add(renderDataFactory.CreateLine(line, Colors.Red, 3f));
		}



		var saxeyAddonsViewModel = new Histogram2DHistogram1DSideBySideViewModel(
			"Time Space and Multi Atom Mass Spectrum",
			sqrtRenderData, multisRenderData, lines2D, lines1D);
		Tabs.Add(saxeyAddonsViewModel);

		DataTable rangeTable = (DataTable)data[5];
		var rangeTableViewModel = new RangeTableViewModel("Range Table", rangeTable);
		Tabs.Add(rangeTableViewModel);

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
