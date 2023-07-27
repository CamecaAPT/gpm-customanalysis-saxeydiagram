using Cameca.CustomAnalysis.Interface;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Media3D;

namespace GPM.CustomAnalysis.SaxeyDiagram;
public static class SaxeyAddons
{
	//Dummy Data
	private static List<float> DTOFsOfInterest = new() { 20.8f, 9.1f, 5.9f, 2.4f, .3f};

	private static List<(float, float)> massToChargePairs = new() { (14, 69), (28, 69), (34.5f, 69), (14, 28), (28, 34.5f) };

	private static Dictionary<string, float> symbolToMassDict = new()
	{
		{ "N+", 14 },
		{ "N2+", 28 },
		{ "Ga+", 69 },
		{ "Ga++", 34.5f }
	};

	/*
	 * Dummy Data
	 */
	public static bool ValidateIon(string ion, List<string> selectedIons)
	{
		return ValidateIon(ion, selectedIons, symbolToMassDict);
	}

	public static bool ValidateIon(string ion, List<string> selectedIons, Dictionary<string, float> symbolToMassDict)
	{
		if(selectedIons.Contains(ion))
		{
			MessageBox.Show("Ion already added.");
			return false;
		}

		if(!symbolToMassDict.Keys.Contains(ion))
		{
			MessageBox.Show("Unknown Ion");
			return false;
		}

		return true;
	}


	/*
	 * CURRENTLY IS HARDCODED DUMMY DATA
	 */
	public static List<Vector3[]> GetLinesSaxey(float maxHeight)
	{
		return GetLinesSaxey(massToChargePairs, maxHeight);
	}
	public static List<Vector3[]> GetLinesSaxey(List<string> selectedSymbols, float maxHeight)
	{
		return GetLinesSaxey(selectedSymbols, symbolToMassDict, maxHeight);
	}

	public static List<Vector3[]> GetLinesSaxey(List<(float, float)> massToChargePairs, float maxHeight)
	{
		List<Vector3[]> lines = new();

		//y = ( sqrt(x) + (m2/c2 - m1/c1) )^2

		//this is essentially the resolution of the line
		const float deltaX = .1f;
		
		foreach(var massToChargePair in massToChargePairs)
		{
			List<Vector3> line = new();
			float xVal = 0f;
			float yVal = 0f;
			var dtof = Math.Abs(Math.Sqrt(massToChargePair.Item1) - Math.Sqrt(massToChargePair.Item2));
			do
			{
				yVal = (float)Math.Pow(Math.Sqrt(xVal) + dtof, 2);
				line.Add(new Vector3(xVal, -1, yVal));
				xVal += deltaX;
			} while (yVal <= maxHeight && xVal <= maxHeight);
			lines.Add(line.ToArray());
		}

		return lines;
	}

	public static List<Vector3[]> GetLinesSaxey(List<string> selectedSymbols, Dictionary<string, float> symbolToMassDict, float maxHeight)
	{
		List<Vector3[]> lines = new();

		//y = ( sqrt(x) + (m2/c2 - m1/c1) )^2

		//this is essentially the resolution of the line
		const float deltaX = .1f;

		for(int i=0; i<selectedSymbols.Count; i++)
		{
			var ion1 = selectedSymbols[i];
			var ion1Mass = symbolToMassDict[ion1];
			for(int j=i+1; j<selectedSymbols.Count; j++)
			{
				var ion2 = selectedSymbols[j];
				var ion2Mass = symbolToMassDict[ion2];
				List<Vector3> line = new();
				float xVal = 0f;
				float yVal = 0f;
				var dtof = Math.Abs(Math.Sqrt(ion1Mass) - Math.Sqrt(ion2Mass));
				do
				{
					yVal = (float)Math.Pow(Math.Sqrt(xVal) + dtof, 2);
					line.Add(new Vector3(xVal, -1, yVal));
					xVal += deltaX;
				} while (yVal <= maxHeight && xVal <= maxHeight);
				lines.Add(line.ToArray());
			}
		}

		return lines;
	}

	/*
	 * CURRENTLY IS HARDCODED DUMMY DATA
	 */
	public static List<Vector3[]> GetLines2D(float maxHeight)
	{
		return GetLines2D(massToChargePairs, maxHeight);
	}
	public static List<Vector3[]> GetLines2D(List<string> selectedSymbols, float maxHeight)
	{
		return GetLines2D(selectedSymbols, symbolToMassDict, maxHeight);
	}

	public static List<Vector3[]> GetLines2D(List<(float, float)> massToChargePairs, float height)
	{
		float h = (float)Math.Sqrt(height);

		List<Vector3[]> lines = new();

		foreach(var massToChargePair in massToChargePairs)
		{
			float dtof = (float)Math.Abs(Math.Sqrt(massToChargePair.Item1) - Math.Sqrt(massToChargePair.Item2));
			Vector3 point1 = new Vector3(0, -1, dtof);
			Vector3 point2 = new Vector3(h - dtof, -1, h);

			Vector3[] arr = new Vector3[2];
			arr[0] = point1;
			arr[1] = point2;
			lines.Add(arr);
		}

		return lines;
	}

	public static List<Vector3[]> GetLines2D(List<string> selectedSymbols, Dictionary<string, float> symbolToMassDict, float height)
	{
		List<Vector3[]> lines = new();
		float h = (float)Math.Sqrt(height);

		for(int i=0; i<selectedSymbols.Count; i++)
		{
			var ion1 = selectedSymbols[i];
			var ion1Mass = symbolToMassDict[ion1];
			for(int j=i+1; j<selectedSymbols.Count; j++)
			{
				var ion2 = selectedSymbols[j];
				var ion2Mass = symbolToMassDict[ion2];

				float dtof = (float)Math.Abs(Math.Sqrt(ion1Mass) - Math.Sqrt(ion2Mass));
				Vector3 point1 = new Vector3(0, -1, dtof);
				Vector3 point2 = new Vector3(h - dtof, -1, h);

				Vector3[] arr = new Vector3[2];
				arr[0] = point1;
				arr[1] = point2;
				lines.Add(arr);
			}
		}


		return lines;
	}

	/*
	 * CURRENTLY IS HARDCODED DUMMY DATA
	 */
	public static List<Vector3[]> GetLines1D(int maxHeight)
	{
		return GetLines1D(massToChargePairs, maxHeight);
	}
	public static List<Vector3[]> GetLines1D(List<string> selectedSymbols, int maxHeight)
	{
		return GetLines1D(selectedSymbols, symbolToMassDict, maxHeight);
	}

	public static List<Vector3[]> GetLines1D(List<(float, float)> massToChargePairs, int maxHeight)
	{
		List<Vector3[]> lines = new();

		foreach(var massToChargePair in massToChargePairs)
		{
			float dtof = (float)Math.Abs(Math.Sqrt(massToChargePair.Item1) - Math.Sqrt(massToChargePair.Item2));
			var dtofSquared = dtof * dtof;
			Vector3 point1 = new(dtofSquared, -1, 0);
			Vector3 point2 = new(dtofSquared, -1, maxHeight);

			Vector3[] arr = new Vector3[2];
			arr[0] = point1;
			arr[1] = point2;
			lines.Add(arr);
		}

		return lines;
	}

	public static List<Vector3[]> GetLines1D(List<string> selectedSymbols, Dictionary<string, float> symbolToMassDict, int maxHeight)
	{
		List<Vector3[]> lines = new();

		for(int i=0; i<selectedSymbols.Count; i++)
		{
			var ion1 = selectedSymbols[i];
			var ion1Mass = symbolToMassDict[ion1];
			for(int j=i+1; j<selectedSymbols.Count; j++)
			{
				var ion2 = selectedSymbols[j];
				var ion2Mass = symbolToMassDict[ion2];

				float dtof = (float)Math.Abs(Math.Sqrt(ion1Mass) - Math.Sqrt(ion2Mass));
				var dtofSquared = dtof * dtof;
				Vector3 point1 = new(dtofSquared, -1, 0);
				Vector3 point2 = new(dtofSquared, -1, maxHeight);

				Vector3[] arr = new Vector3[2];
				arr[0] = point1;
				arr[1] = point2;
				lines.Add(arr);
			}
		}

		return lines;
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

	public static ReadOnlyMemory<Vector2> BuildMultisHistogram(List<Vector2> points, float maxSqrtMassToCharge, float resolution, out int maxHeight)
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

		maxHeight = 0;

		for(int index = 0; index < histogramData.Length; index++)
		{
			float box = index * resolution;
			int count = histogramData[index];
			if (count > maxHeight)
				maxHeight = count;
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
