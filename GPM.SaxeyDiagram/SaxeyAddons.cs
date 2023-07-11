using CommunityToolkit.HighPerformance;
using System;

namespace GPM.CustomAnalysis.SaxeyDiagram;
public static class SaxeyAddons
{
	public static ReadOnlyMemory2D<float> BuildSqrtChart(float[] map, int sideLength, float resolution, out float newResolution)
	{
		float physicalSideLength = sideLength * resolution;
		float newPhysicalSideLength = (float)Math.Sqrt(physicalSideLength);

		int newSideLength = sideLength;
		newResolution = (float)newPhysicalSideLength / newSideLength;

		float[] newArr = new float[newSideLength * newSideLength];
		
		for(int i=0; i<map.Length; i++)
		{
			int x = i / sideLength;
			int y = i % sideLength;

			float physicalX = x * resolution;
			float physicalY = y * resolution;

			physicalX = (float)Math.Sqrt(physicalX);
			physicalY = (float)Math.Sqrt(physicalY);

			x = (int)(physicalX / newResolution);
			y = (int)(physicalY / newResolution);

			int index = (newSideLength * x) + y;
			
			if(index < newArr.Length)
				newArr[index] += map[i];
		}

		ReadOnlyMemory2D<float> toRet = new(newArr, newSideLength, newSideLength);
		return toRet;
	}
}
