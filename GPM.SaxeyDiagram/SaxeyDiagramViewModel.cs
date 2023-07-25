using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.Mvvm.Input;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class SaxeyDiagramViewModel : AnalysisViewModelBase<SaxeyDiagramNode>
{
	public const string UniqueId = "GPM.CustomAnalysis.SaxeyDiagram.SaxeyDiagramViewModel";

	private readonly IRenderDataFactory renderDataFactory;
	private readonly IColorMap colorMap;
	private bool optionsChanged = false;

	private readonly AsyncRelayCommand runCommand;
	public ICommand RunCommand => runCommand;

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
		
		var renderData = renderDataFactory.CreateHistogram2D(
			saxeyData,
			new Vector2(Options.Resolution, Options.Resolution),
			colorMap,
			new Vector2(Options.XMin, Options.YMin),
			minValue: CSaxeyDiagram.MinBinValueInclusive);
		var histogram2DViewModel = new Histogram2DContentViewModel(
			"Saxey Diagram",
			renderData);
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

		//Vector3[] linePoints = new Vector3[] { new Vector3(0, 0, 0), new Vector3(5, 5, 5) };

		//var line = renderDataFactory.CreateLine(linePoints, Colors.Red, 1.5f);

		var saxeyAddonsViewModel = new Histogram2DHistogram1DSideBySideViewModel(
			"Time Space and Multi Atom Mass Spectrum",
			sqrtRenderData, multisRenderData);
		Tabs.Add(saxeyAddonsViewModel);

		DataTable rangeTable = (DataTable)data[4];
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
