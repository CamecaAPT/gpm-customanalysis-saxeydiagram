using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GPM.CustomAnalysis.SaxeyDiagram;
public static class SaxeyAddons
{
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

	public static ReadOnlyMemory<Vector2> BuildMultisHistogram(List<Vector2> points, float maxSqrtMassToCharge)
	{
		float resolution = .01f;
		int start = 0;
		int end = (int)(maxSqrtMassToCharge * maxSqrtMassToCharge) + 1;

		int boxes = (int)((end - start) / resolution) + 1;
		Vector2[] histogramData = new Vector2[boxes];

		List<Vector2> histogramList = new();

		int inCount = 0;
		int outCount = 0;

		SortedDictionary<int, int> map = new SortedDictionary<int, int>();

		foreach(var point in points)
		{ 
			var deltaTOF = Math.Sqrt(point.Y) - Math.Sqrt(point.X);
			var deltaTOFSquare = deltaTOF * deltaTOF;
			int index = (int)(deltaTOFSquare / resolution);
			if (index * resolution <= end && index * resolution >= start)
			{
				histogramData[index].X = index * resolution;
				histogramData[index].Y++;
				//histogramList.Add(new Vector2(index * resolution, 10));
				inCount++;
				if (!map.ContainsKey(index))
					map.Add(index, 0);
				map[index]++;
			}
			else
				outCount++;
		}

		foreach (var indexCountPair in map)
		{
			//if (indexCountPair.Key == 0) continue;
			histogramList.Add(new Vector2(indexCountPair.Key * resolution, indexCountPair.Value));
		}

		//Log10ScaleTransformation(histogramList);

		return new ReadOnlyMemory<Vector2>(histogramList.ToArray());
		//return new ReadOnlyMemory<Vector2>(histogramData);
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
