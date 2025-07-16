using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.HighPerformance;

namespace GPM.CustomAnalysis.SaxeyDiagram;

[DefaultView(SaxeyDiagramViewModel.UniqueId, typeof(SaxeyDiagramViewModel))]
internal class SaxeyDiagramNode : AnalysisNodeBase
{
	public class SaxeyDisplayInfo : INodeDisplayInfo
	{
		public string Title { get; } = "GPM Saxey Diagram";
		public ImageSource? Icon { get; } = null;
	}

	public static SaxeyDisplayInfo DisplayInfo { get; } = new();

	public const string UniqueId = "GPM.CustomAnalysis.SaxeyDiagram.SaxeyDiagramNode";

	private readonly IElementDataSetService _elementDataSetService;
	private readonly INodeElementDataSetProvider _nodeElementDataSetProvider;
	private readonly IIonFormulaIsotopeCalculator _ionFormulaIsotopeCalculator;
	public Dictionary<string, IElement> Elements { get; private set; }

	public SaxeyDiagramNode(IAnalysisNodeBaseServices services, IElementDataSetService elementDataSetService, INodeElementDataSetProvider nodeElementDataSetProvider, IIonFormulaIsotopeCalculator ionFormulaIsotopeCalculator) : base(services)
	{
		_elementDataSetService = elementDataSetService;
		_nodeElementDataSetProvider = nodeElementDataSetProvider;
		_ionFormulaIsotopeCalculator = ionFormulaIsotopeCalculator;
		Elements = new();
	}

	public SaxeyDiagramOptions Options { get; private set; } = new();

	public List<string> IonLineAndChartSelection { get; set; } = new();

	// [0] = Map
	// [1] = sqrtChart
	// [2] = newResolution
	// [3] = multisChart
	// [4] = maxHeight
	// [5] = elementDataset
	// [6] = _ionFormulaIsotopeCalculator

	public async Task<List<object>?> Run()
	{
		List<object> toRet = new();

		if (await Services.IonDataProvider.GetIonData(InstanceId) is not { } ionData)
			return null;

		var sectionInfo = new HashSet<string>(ionData.Sections.Keys);
		if (!(IsIonDataValid(ionData)))
		{
			return null;
		}

		bool hasMultiplicity = sectionInfo.Contains("Multiplicity");

		// Init Saxey diagram
		var saxey = new CSaxeyDiagram();

		// Build Saxey diagram
		saxey.Build(Options, ionData, hasMultiplicity);

		toRet.Add(new ReadOnlyMemory2D<float>(saxey.Map, Options.EdgeSize, Options.EdgeSize));

		var sqrtChart = SaxeyAddons.BuildSqrtChart(saxey.Points, Options.EdgeSize, Options.Resolution, out var newResolution, out var newPhysicalSideLength);

		toRet.Add(sqrtChart);
		toRet.Add(newResolution);

		var multisChart = SaxeyAddons.BuildMultisHistogram(saxey.Points, newPhysicalSideLength, Options.DToFBinSize, out int maxHeight);

		toRet.Add(multisChart);
		toRet.Add(maxHeight);

		var nodeElementDataset = _nodeElementDataSetProvider.Resolve(Services.IdProvider.Get(this));
		var elementDataset = _elementDataSetService.GetElementDataSet(nodeElementDataset!.ElementDataSetId);
		if (elementDataset != null)
		{
			toRet.Add(elementDataset);
			toRet.Add(_ionFormulaIsotopeCalculator);
		}
		else
			throw new Exception("problem with getting the element dataset");

		if (Options.ExportToCsv)
		{
			var csvName = ionData.Filename + ".SP.csv";
			if (Options.ExportRawMap)
			{
				saxey.ExportToCsvTable(csvName, saxey.RawMap, out string? err);
			}
			else
			{
				saxey.ExportToCsvTable(csvName, saxey.Map, out string? err);
			}
		}


		return toRet;
	}

	protected override byte[]? GetSaveContent()
	{
		var serializer = new XmlSerializer(typeof(SaxeyDiagramOptions));
		using var stringWriter = new StringWriter();
		serializer.Serialize(stringWriter, Options);
		return Encoding.UTF8.GetBytes(stringWriter.ToString());
	}

    protected override void OnCreated(NodeCreatedEventArgs eventArgs)
    {
        base.OnCreated(eventArgs);

        Options.PropertyChanged += OptionsOnPropertyChanged;

		if (eventArgs.Trigger == EventTrigger.Load && eventArgs.Data is { } data)
        {
            var xmlData = Encoding.UTF8.GetString(data);
            var serializer = new XmlSerializer(typeof(SaxeyDiagramOptions));
            using var stringReader = new StringReader(xmlData);
            if (serializer.Deserialize(stringReader) is SaxeyDiagramOptions loadedOptions)
            {
				foreach(var line in loadedOptions.IonSelections)
				{
					line.SetColor(line.Color);
				}

            	Options = loadedOptions;
            }
		}

		//Create the element dictionary for easy lookup
		var nodeElementDataset = _nodeElementDataSetProvider.Resolve(Services.IdProvider.Get(this));
		var elementDataset = _elementDataSetService.GetElementDataSet(nodeElementDataset!.ElementDataSetId)!;
		Elements = new();
		foreach (var element in elementDataset.Elements)
			Elements.Add(element.Symbol, element);
	}

	private void OptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (CanSaveState is { } canSaveState)
		{
			canSaveState.CanSave = true;
		}
	}

	private bool IsIonDataValid(IIonData ionData)
	{
		var sectionInfo = new HashSet<string>(ionData.Sections.Keys);
		return sectionInfo.Contains(IonDataSectionName.Mass)
		       && (sectionInfo.Contains("Multiplicity") || sectionInfo.Contains("pulse"));
	}
}
