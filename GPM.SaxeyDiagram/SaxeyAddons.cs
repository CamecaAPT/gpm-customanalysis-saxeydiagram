using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;

namespace GPM.CustomAnalysis.SaxeyDiagram;
public static class SaxeyAddons
{
	private static List<string> massToChargeXIons = new() { "Ga+++(1)", "Ga+++(2)", "N2+", "NH2+", "Ga++(1)", "Ga++(2)", "GaN++(1)", "GaN++(2)", "N3+", "Ga+(1)", "Ga+(2)" }; 
	private static List<float> massToChargeX = new() { 23, 23.7f, 28, 29, 34.5f, 35.5f, 41.5f, 42.5f, 42, 69, 71 };

	private static List<string> massToChargeYIons = new() { "H+", "H2+", "N++", "N+", "Ga+++(1)", "Ga+++(2)", "N2+", "NH2+", "NH2+", "Ga++(1)", "Ga++(2)", "GaN3++(1)", "GaN3++(2)", "Ga+(1)", "Ga+(2)" };
	private static List<float> massToChargeY = new() { 1, 2, 7, 14, 23, 23.7f, 28, 29, 34.5f, 35.5f, 55.5f, 56.5f, 69, 71 };

	public class Ion
	{
		public string Symbol { get; set; }
		public float Mass { get; set; }

		public Ion(string symbol, float mass)
		{
			Symbol = symbol;
			Mass = mass;
		}
	}

	private static List<Ion> rangeChartIons = new()
	{
		new Ion("N+", 14),
		new Ion("Ga+", 69),
		new Ion("N2+", 28),
		new Ion("Ga++", 34.5f)
	};

	/*
	 * Dummy Data Method
	 */
	public static DataTable BuildRangeTable()
	{
		//return BuildRangeTable(massToChargeX, massToChargeY);
		return BuildRangeTable(rangeChartIons);
	}

	public static DataTable BuildRangeTable(List<Ion> ions)
	{
		DataTable rangeTable = new();

		//Add Columns
		rangeTable.Columns.Add("_");
		rangeTable.Columns.Add("__");
		foreach(var ion in ions)
			rangeTable.Columns.Add(ion.Symbol);

		//add secondary column information (ion weight)
		List<object> row = new() { "", "" };
		foreach (var ion in ions)
			row.Add(ion.Mass.ToString("f2"));
		rangeTable.Rows.Add(row.ToArray());

		for(int i=0; i<ions.Count; i++)
		{
			Ion ion1 = rangeChartIons[i];
			row = new() { ion1.Symbol, ion1.Mass.ToString("f2") };

			//add spaces
			for (int k = 0; k < i; k++)
				row.Add("");

			for(int j=i; j<ions.Count; j++)
			{
				Ion ion2 = rangeChartIons[j];
				var dtofSquared = Math.Pow(Math.Sqrt(ion1.Mass) - Math.Sqrt(ion2.Mass), 2);
				row.Add(dtofSquared.ToString("f2"));
			}
			rangeTable.Rows.Add(row.ToArray());
		}

		return rangeTable;
	}

	public static DataTable BuildRangeTable(List<float> massToChargeX, List<float> massToChargeY)
	{
		DataTable rangeTable = new();

		//Add Columns
		rangeTable.Columns.Add("_");
		rangeTable.Columns.Add("__");
		foreach (var xLabel in massToChargeXIons)
			rangeTable.Columns.Add(xLabel);

		for(int i=0; i<massToChargeY.Count; i++)
		{
			var massY = massToChargeY[i];
			List<object> row = new();
			row.Add(massToChargeYIons[i]);
			row.Add(massY.ToString("f2"));
			foreach(var massX in massToChargeX)
			{
				var dtofSquared = Math.Pow(Math.Sqrt(massX) - Math.Sqrt(massY), 2);
				row.Add(dtofSquared.ToString("f2"));
			}
			rangeTable.Rows.Add(row.ToArray());
		}


		return rangeTable;
	}

	public static ReadOnlyMemory2D<float> BuildSqrtChart(List<Vector2> points, int origSideLength, float origResolution, out float newResolution, out float newPhysicalSideLength)
	{
		float physicalSideLength = origSideLength * origResolution;
		newPhysicalSideLength = (float)Math.Sqrt(physicalSideLength);
		int newSideLength = origSideLength;
		newResolution = newPhysicalSideLength / newSideLength;
		
		float[] histogramArray = new float[newSideLength * newSideLength];


		foreach(var point in points)
		{
			float physX = (float)Math.Sqrt(point.X);
			float physY = (float)Math.Sqrt(point.Y);

			int x = (int)(physX / newResolution);
			int y = (int)(physY / newResolution);

			int index = (int)((newSideLength * x) + y);

			if (index < histogramArray.Length)
				if (x < y)
					histogramArray[index]++;
		}

		Log10ScaleTransformation(histogramArray);
		NormalizeMap(histogramArray);

		return new ReadOnlyMemory2D<float>(histogramArray, newSideLength, newSideLength);
	}

	public static ReadOnlyMemory<Vector2> BuildMultisHistogram(List<Vector2> points, float maxSqrtMassToCharge, float resolution)
	{
		int start = 0;
		int end = (int)(maxSqrtMassToCharge * maxSqrtMassToCharge) + 1;

		int boxes = (int)((end - start) / resolution) + 1;
		int[] histogramData = new int[boxes];

		List<Vector2> histogramList = new();

		SortedDictionary<int, int> map = new();

		foreach(var point in points)
		{ 
			var deltaTOF = Math.Sqrt(point.Y) - Math.Sqrt(point.X);
			var deltaTOFSquare = deltaTOF * deltaTOF;
			int index = (int)(deltaTOFSquare / resolution);
			if (index * resolution <= end && index * resolution >= start)
			{
				histogramData[index]++;
			}
		}

		for(int index = 0; index < histogramData.Length; index++)
		{
			float box = index * resolution;
			int count = histogramData[index];
			histogramList.Add(new Vector2(box, count));
		}

		return new ReadOnlyMemory<Vector2>(histogramList.ToArray());
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
}
