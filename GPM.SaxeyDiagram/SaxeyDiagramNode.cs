using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

	public SaxeyDiagramNode(IAnalysisNodeBaseServices services) : base(services)
	{
	}

	public SaxeyDiagramOptions Options { get; private set; } = new();

	public async Task<List<ReadOnlyMemory2D<float>>?> Run()
	{
		List<ReadOnlyMemory2D<float>> toRet = new();

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

		toRet.Add(new ReadOnlyMemory2D<float>(saxey.Map, Options.EdgeSize, Options.EdgeSize));

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
            	Options = loadedOptions;
            }
		}
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
