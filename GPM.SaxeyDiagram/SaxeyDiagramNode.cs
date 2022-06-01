using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Interface.Resources.IonData;
using Cameca.CustomAnalysis.Utilities;
using Microsoft.Toolkit.HighPerformance;

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

	public SaxeyDiagramNode(IAnalysisNodeBaseServices services) : base(services)
	{
	}

	public SaxeyDiagramOptions Options { get; private set; } = new();

	public async Task<ReadOnlyMemory2D<float>?> Run()
	{
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

		if (Options.ExportToCsv)
		{
			var csvName = ionData.Filename + ".SP.csv";
			saxey.ExportToCsvTable(csvName, out string? err);
		}

		return new ReadOnlyMemory2D<float>(saxey.Map, Options.EdgeSize, Options.EdgeSize);
	}

	protected override byte[]? GetSaveContent()
	{
		var serializer = new XmlSerializer(typeof(SaxeyDiagramOptions));
		using var stringWriter = new StringWriter();
		serializer.Serialize(stringWriter, Options);
		return Encoding.UTF8.GetBytes(stringWriter.ToString());
	}

	protected override void OnLoaded(NodeLoadedEventArgs eventArgs)
	{
		if (eventArgs.Data is not { } data) return;
		var xmlData = Encoding.UTF8.GetString(data);
		var serializer = new XmlSerializer(typeof(SaxeyDiagramOptions));
		using var stringReader = new StringReader(xmlData);
		if (serializer.Deserialize(stringReader) is SaxeyDiagramOptions loadedOptions)
		{
			Options = loadedOptions;
		}
	}

	protected override void OnInstantiated(INodeInstantiatedEventArgs eventArgs)
	{
		base.OnInstantiated(eventArgs);
		Options.PropertyChanged += OptionsOnPropertyChanged;
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
