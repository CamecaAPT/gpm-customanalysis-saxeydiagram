using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class CSaxeyDiagram
{
	public const float MinBinValueInclusive = 0.0001f;
	public const float ReplacedOutOfRangeBinValue = 0f;

	private int pixels;

	private SaxeyDiagramOptions? nullableOptions = null;
	private SaxeyDiagramOptions options => nullableOptions ?? throw new InvalidOperationException($"{nameof(CSaxeyDiagram)}.{nameof(Build)} must be called before running this analysis");

	public float[] Map { get; private set; } = Array.Empty<float>();
	public List<Vector2> Points { get; private set; } = new();

	public void Build(SaxeyDiagramOptions saxeyDiagramOptions, IIonData ionData, bool hasMultiplicity)
	{
		nullableOptions = saxeyDiagramOptions;
		pixels = (options.EdgeSize + 2) * (options.EdgeSize + 2);
		Map = new float[pixels];

		if (hasMultiplicity)
		{
			BuildFromMultiplicitySection(ionData);
		}
		else
		{
			BuildFromPulseSection(ionData);
		}

		Log10ScaleTransformation(Map);
		NormalizeMap(Map);

		ReplaceLowerValues(MinBinValueInclusive, ReplacedOutOfRangeBinValue);
	}

	private void BuildFromMultiplicitySection(IIonData ionData)
	{
		var multiEventMasses = new List<float>();
		foreach (var chunk in ionData.CreateSectionDataEnumerable(IonDataSectionName.Mass, "Multiplicity"))
		{
			if (chunk.Length == 0)
			{
				break;
			}

			var multiplicities = chunk.ReadSectionData<int>("Multiplicity");
			var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass);
			for (int i = 0; i < chunk.Length; ++i)
			{
				int multiplicity = multiplicities.Span[i];


				if (multiplicity == 1 && multiEventMasses.Count == 0)
				{
					continue;
				}

				if (multiEventMasses.Count == 0 || multiplicity == 0)
				{
					float mass = masses.Span[i];

					multiEventMasses.Add(mass);
					continue;
				}

				ProcessEvent(multiEventMasses);

				if (multiplicity != 1)
				{
					float mass = masses.Span[i];

					multiEventMasses.Add(mass);
				}
			}
		}

		// Perhaps catch the very last multi event
		if (multiEventMasses.Count != 0)
		{
			ProcessEvent(multiEventMasses);
		}
	}

	private void BuildFromPulseSection(IIonData ionData)
	{
		var multiEventMasses = new List<float>();

		float prevMass = 0;
		float prevPulse = -1;

		foreach (var chunk in ionData.CreateSectionDataEnumerable(IonDataSectionName.Mass, "pulse"))
		{
			if (chunk.Length == 0)
			{
				break;
			}

			var pulses = chunk.ReadSectionData<float>("pulse");
			var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass);
			for (int i = 0; i < chunk.Length; ++i)
			{
				var mass = masses.Span[i];
				var pulse = pulses.Span[i];

				if (pulse == prevPulse)
				{
					multiEventMasses.Add(prevMass);
				}
				else if (multiEventMasses.Count != 0)
				{
					multiEventMasses.Add(prevMass);
					ProcessEvent(multiEventMasses);
				}

				prevMass = mass;
				prevPulse = pulse;
			}
		}

		// Perhaps catch the very last multi event
		if (multiEventMasses.Count != 0)
		{
			multiEventMasses.Add(prevMass);
			ProcessEvent(multiEventMasses);
		}
	}

	private static void NormalizeMap(float[] histogramArray)
	{
		// Adjust amplitude of Map memory
		float fMaxValue = 0;
		for (int i = 0; i < histogramArray.Length; i++)
		{
			fMaxValue = Math.Max(histogramArray[i], fMaxValue);
		}

		for (int i = 0; i < histogramArray.Length; i++)
		{
			histogramArray[i] /= fMaxValue;
		}
	}

	private static void Log10ScaleTransformation(float[] histogramArray)
	{
		for (int i = 0; i < histogramArray.Length; i++)
		{
			if (histogramArray[i] > 0)
				histogramArray[i] = (float)Math.Log10(1 + histogramArray[i]);
		}
	}


	private void ReplaceLowerValues(float minCutoff, float replaceValue)
	{
		for (int i = 0; i < pixels; i++)
		{
			if (Map[i] < minCutoff)
			{
				Map[i] = replaceValue;
			}
		}
	}

	private void ProcessEvent(List<float> multiEventMasses)
	{
		// We have the whole event, sort and plot.
		multiEventMasses.Sort();
		int events = multiEventMasses.Count;

		// Treat selected type of events
		if (options.EventSelections.Plot(events))
		{
			for (int j = 0; j < events - 1; j++)
				for (int k = j + 1; k < events; k++)
				{
					if(options.MirrorPlot)
					{
						if (multiEventMasses[j] >= options.XMin && multiEventMasses[j] < options.XMin + options.MassExtent)
							if (multiEventMasses[k] >= options.YMin && multiEventMasses[k] < options.YMin + options.MassExtent)
							{
								int b = (int)((multiEventMasses[j] - options.XMin) / options.Resolution);
								int a = (int)((multiEventMasses[k] - options.YMin) / options.Resolution);

								int index = a * options.EdgeSize + b;
								if (index < pixels)
								{
									Map[index]++;
									Points.Add(new Vector2(multiEventMasses[k], multiEventMasses[j]));
								}

							}
					}
					if (multiEventMasses[k] >= options.XMin && multiEventMasses[k] < options.XMin + options.MassExtent)
						if (multiEventMasses[j] >= options.YMin && multiEventMasses[j] < options.YMin + options.MassExtent)
						{
							int b = (int)((multiEventMasses[j] - options.YMin) / options.Resolution);
							int a = (int)((multiEventMasses[k] - options.XMin) / options.Resolution);

							int index = b * options.EdgeSize + a;
							if (index < pixels)
							{
								Map[index]++;
								Points.Add(new Vector2(multiEventMasses[j], multiEventMasses[k]));
							}
						}
				}

		}

		multiEventMasses.Clear();
	}

	/// <summary>
	/// Export histogram data to a CSV file as a table
	/// </summary>
	/// <param name="filename"></param>
	/// <param name="error">Error text if failure (CSV open, for instance)</param>
	internal bool ExportToCsvTable(string filename, [NotNullWhen(false)] out string? error)
	{

		try
		{
			float fResolution = options.MassExtent / (float)options.EdgeSize;
			using var writer = new StreamWriter(filename);
			Console.WriteLine("a");
			for (int j = 0; j < options.EdgeSize; j++)
			{
				if (j == 0)
				{
					writer.Write(@"y [Da]\x [Da]");
					//continue;
				}

				writer.Write(',');
				double y = options.YMin + j * fResolution;
				writer.Write(y);
			}

			writer.Write(writer.NewLine);

			for (int i = 0; i < options.EdgeSize; i++)
			{
				double x = options.XMin + i * fResolution;
				writer.Write(x);
				for (int j = 0; j < options.EdgeSize; j++)
				{

					writer.Write(',');
					writer.Write(Map[j * options.EdgeSize + i]);

				}

				writer.Write(writer.NewLine);
			}
		}
		catch (Exception e)
		{
			error = e.Message;
			return false;
		}

		error = null;
		return true;
	}
}
