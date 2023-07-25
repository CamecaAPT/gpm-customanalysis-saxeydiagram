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

	/*
	 * Dummy Data Method
	 */
	public static DataTable BuildRangeTable()
	{
		return BuildRangeTable(massToChargeX, massToChargeY);
	}

	public static DataTable BuildRangeTable(List<float> massToChargeX, List<float> massToChargeY)
	{
		DataTable rangeTable = new();

		//Add Columns
		rangeTable.Columns.Add("_");
		rangeTable.Columns.Add("__");
		foreach (var xLabel in massToChargeXIons)
			rangeTable.Columns.Add(xLabel);

		//List<object> cols = new();
		//cols.Add("");
		//foreach (var mass in massToChargeX)
		//{
		//	cols.Add(mass.ToString("f2"));
		//}
		//rangeTable.Rows.Add(cols.ToArray());

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
