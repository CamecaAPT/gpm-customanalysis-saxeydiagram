using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace GPM.CustomAnalysis.SaxeyDiagram;

public class EventSelections : BindableBase
{
	/// <inheritdoc />
	public EventSelections()
	{
		UpdateAll();
	}

	private void UpdateAll()
	{
		all = doubles && triples && quads && fivePlus;
		RaisePropertyChanged(nameof(All));
	}

	private bool doubles = true;
	public bool Doubles
	{
		get => doubles;
		set => SetProperty(ref doubles, value, UpdateAll);
	}

	private bool triples = true;
	public bool Triples
	{
		get => triples;
		set => SetProperty(ref triples, value, UpdateAll);
	}

	private bool quads = true;
	public bool Quads
	{
		get => quads;
		set => SetProperty(ref quads, value, UpdateAll);
	}

	private bool fivePlus = true;
	[Display(Name = ">= 5")]
	public bool FivePlus
	{
		get => fivePlus;
		set => SetProperty(ref fivePlus, value, UpdateAll);
	}

	private bool all;
	[XmlIgnore]
	public bool All
	{
		get => all;
		set => SetProperty(ref all, value, () =>
		{
			doubles = true;
			triples = value;
			quads = value;
			fivePlus = value;
			RaisePropertyChanged(nameof(Doubles));
			RaisePropertyChanged(nameof(Triples));
			RaisePropertyChanged(nameof(Quads));
			RaisePropertyChanged(nameof(FivePlus));
		});
	}

	public bool Plot(int eventCount)
	{
		if (all) return true;
		if (eventCount == 2) return doubles;
		if (eventCount == 3) return triples;
		if (eventCount == 4) return quads;
		if (eventCount >= 5) return fivePlus;
		throw new ArgumentException($"Invalid value {eventCount}", nameof(eventCount));
	}
}
