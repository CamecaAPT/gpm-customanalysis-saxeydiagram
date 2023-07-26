using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace GPM.CustomAnalysis.SaxeyDiagram;

public class SaxeyDiagramOptions : BindableBase
{
	/// <inheritdoc />
	public SaxeyDiagramOptions()
	{
		UpdateResolution();
	}

	private void UpdateResolution() => Resolution = MassExtent / EdgeSize;

	private float massExtent = 50;
	[Display(Name = "Mass Extent (Da)", Description = "Extent of plot, max mass is min + extent")]
	public float MassExtent
	{
		get => massExtent;
		set => SetProperty(ref massExtent, value, UpdateResolution);
	}

	private int edgeSize = 1000;
	[Display(Name = "Edge Size", Description = "Number of pixels in width and height")]
	public int EdgeSize
	{
		get => edgeSize;
		set => SetProperty(ref edgeSize, value, UpdateResolution);
	}

	private float xMin = 0;
	[Display(Name = "X Minimum (Da)", Description = "Minimum mass on X axis")]
	public float XMin
	{
		get => xMin;
		set => SetProperty(ref xMin, value);
	}

	private float yMin = 0;
	[Display(Name = "Y Minimum (Da)", Description = "Minimum mass on Y axis")]
	public float YMin
	{
		get => yMin;
		set => SetProperty(ref yMin, value);
	}

	private float resolution;
	[XmlIgnore]
	[Display(Name = "Resolution (Da)", Description = "Resolution of diagram")]
	public float Resolution
	{
		get => resolution;
		private set => SetProperty(ref resolution, value);
	}

	[Display(Name = "Events to show", Description = "Multiplicities of events to include in the diagram.")]
	public EventSelections EventSelections { get; set; } = new EventSelections();

	private bool plotZeroAsWhite = true;
	[Display(Name = "Plot zero as white")]
	public bool PlotZeroAsWhite
	{
		get => plotZeroAsWhite;
		set => SetProperty(ref plotZeroAsWhite, value);
	}

	private bool exportToCsv = false;
	[Display(Name = "Export CSV", Description = "Creates a CSV file of plot data.")]
	public bool ExportToCsv
	{
		get => exportToCsv;
		set => SetProperty(ref exportToCsv, value);
	}

	private bool mirrorPlot = false;
	[Display(Name = "Mirror Plot", Description = "Leave the bottom half of the plot blank or have a copy of the top reflected.")]
	public bool MirrorPlot
	{
		get => mirrorPlot;
		set => SetProperty(ref mirrorPlot, value);
	}

	private float dtofBinSize = .01f;
	[Display(Name = "DToF Mass Spec Bin Size", Description = "Bin size for the calculated mass spectrum (Delta Time of Flight Squared)")]
	public float DToFBinSize
	{
		get => dtofBinSize;
		set => SetProperty(ref dtofBinSize, value);
	}

	[Display(Name = "Where to show lines", Description = "Which graphs to render lines onto.")]
	public LineSelections LineSelections { get; set; } = new LineSelections();

	//private float atom1mc;
	//[Display(Name = "Atom 1 m/c")]
	//public float Atom1mc
	//{
	//	get => atom1mc;
	//	set => SetProperty(ref atom1mc, value);
	//}

	//private float atom2mc;
	//[Display(Name = "Atom 2 m/c")]
	//public float Atom2mc
	//{
	//	get => atom2mc;
	//	set => SetProperty (ref atom2mc, value);
	//}
}
