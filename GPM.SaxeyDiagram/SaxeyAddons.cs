using Cameca.CustomAnalysis.Interface;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GPM.CustomAnalysis.SaxeyDiagram;
public static class SaxeyAddons
{
	//Dummy Data
	private static List<float> DTOFsOfInterest = new() { 20.8f, 9.1f, 5.9f, 2.4f, .3f};

	/*
	 * CURRENTLY IS HARDCODED DUMMY DATA
	 */
	public static List<Vector3[]> GetLinesSaxey(float maxHeight)
	{
		List<Vector3[]> lines = new();

		//y = ( sqrt(x) + (m2/c2 - m1/c1) )^2

		//this is essentially the resolution of the line
		const float deltaX = .1f;
		
		
		foreach(var dtof in DTOFsOfInterest)
		{
			List<Vector3> line = new();
			float xVal = 0f;
			float yVal = 0f;
			do
			{
				line.Add(new Vector3(xVal, -1, yVal));
				xVal += deltaX;
				yVal = (float)Math.Pow(Math.Sqrt(xVal) + Math.Sqrt(dtof),2);
			} while (yVal <= maxHeight && xVal <= maxHeight);
			lines.Add(line.ToArray());
		}

		return lines;
	}

	/*
	 * CURRENTLY IS HARDCODED DUMMY DATA
	 */
	public static List<Vector3[]> GetLines2D(float height)
	{
		float h = (float)Math.Sqrt(height);

		List<Vector3[]> lines = new();

		foreach(float val in DTOFsOfInterest)
		{
			float sqrtD = (float)Math.Sqrt(val);
			Vector3 point1 = new Vector3(0, -1, sqrtD);
			Vector3 point2 = new Vector3(h - sqrtD, -1, h);

			Vector3[] arr = new Vector3[2];
			arr[0] = point1;
			arr[1] = point2;
			lines.Add(arr);
		}

		return lines;
	}

	/*
	 * CURRENTLY IS HARDCODED DUMMY DATA
	 */
	public static List<Vector3[]> GetLines1D(int maxHeight)
	{
		List<Vector3[]> lines = new();

		foreach(float val in DTOFsOfInterest)
		{
			Vector3 point1 = new(val, -1, 0);
			Vector3 point2 = new(val, -1, maxHeight);

			Vector3[] arr = new Vector3[2];
			arr[0] = point1;
			arr[1] = point2;
			lines.Add(arr);
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
