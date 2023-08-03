using Cameca.CustomAnalysis.Interface;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GPM.CustomAnalysis.SaxeyDiagram;
public static class SaxeyAddons
{
	//if running this method, assume that already ran through regex and it is good
	public static IonFormula? IonFormulaFromMatch(Match match, Dictionary<string, IElement> elements, out int? chargeCount)
	{
		List<IonFormula.Component> components = new();
		chargeCount = null;

		var groups = match.Groups.Values.ToList();

		foreach(var fullSymbol in groups[1].Captures.ToList())
		{
			var fullSymArr = fullSymbol.Value.ToCharArray();
			int numericIndex = 0;
			foreach( var sym in fullSymArr)
			{
				if (sym >= 48 && sym <= 57)
					break;
				numericIndex++;
			}

			string element = fullSymbol.Value.Substring(0, numericIndex);
			int num;
			if (numericIndex == fullSymbol.Value.Length)
				num = 1;
			else
				num = int.Parse(fullSymbol.Value.Substring(numericIndex, fullSymbol.Value.Length - numericIndex));

			if(!elements.ContainsKey(element))
			{
				MessageBox.Show($"{element} is not a valid element");
				return null;
			}

			components.Add(new IonFormula.Component(element, num));
		}

		chargeCount = groups[2].Value.Length;
		if (chargeCount == 0)
			chargeCount = 1;

		IonFormula formula = new(components);
		return formula;
	}

	/*
	 * Make sure has only alphanumerics and + signs
	 * also, once theres a plus it can only be plusses
	 */
	public static bool ValidateIonString(string ionString, out Match match)
	{
		Regex regex = new(@"(?:([A-Z][a-z]?\d*))+([+]{0,5})");
		match = regex.Match(ionString);
		if(!match.Success || match.Value != ionString)
		{
			MessageBox.Show("Invalid format for Ion");
			return false;
		}
		return true;
	}

	public static (Dictionary<string, List<float>>, Dictionary<string, List<float>>) MakeSymbolToMassDict(LinesOptions linesOptions)
	{
		var selectedSymbols = linesOptions.selectedSymbols;
		var selectedCharges = linesOptions.selectedCharges;
		var elements = linesOptions.elements;
		var calculator = linesOptions.calculator;
		var calculatorOptions = linesOptions.calculatorOptions;

		//TODO: clean this up perhaps
		List<IonFormula> selectedIons1 = new();
		List<IonFormula> selectedIons2 = new();
		foreach (var symbol in selectedSymbols)
		{
			ValidateIonString(symbol.Item1, out var match1);
			ValidateIonString(symbol.Item2, out var match2);
			selectedIons1.Add(IonFormulaFromMatch(match1, elements, out var _)!);
			selectedIons2.Add(IonFormulaFromMatch(match2, elements, out var _)!);
		}

		Dictionary<string, List<float>> symbolToMassDict1 = new();
		Dictionary<string, List<float>> symbolToMassDict2 = new();

		for (int i = 0; i < selectedIons1.Count; i++)
		{
			//1
			var sym1 = selectedSymbols[i].Item1;
			var formula1 = selectedIons1[i];
			var charge1 = selectedCharges[i].Item1;
			if (!symbolToMassDict1.ContainsKey(sym1))
				symbolToMassDict1.Add(sym1, new());

			var isotopes1 = calculator.GetIsotopes(formula1, calculatorOptions);

			foreach (var isotope in isotopes1)
				symbolToMassDict1[sym1].Add((float)isotope.Mass / charge1);

			//2
			var sym2 = selectedSymbols[i].Item2;
			var formula2 = selectedIons2[i];
			var charge2 = selectedCharges[i].Item2;
			if(!symbolToMassDict2.ContainsKey(sym2))
				symbolToMassDict2.Add(sym2, new());

			var isotopes2 = calculator.GetIsotopes(formula2, calculatorOptions);

			foreach (var isotope in isotopes2)
				symbolToMassDict2[sym2].Add((float)isotope.Mass / charge2);
		}

		return (symbolToMassDict1, symbolToMassDict2);
	}

	public static DataTable BuildRangeTable(LinesOptions linesOptions)
	{
		/*
		 * For now, Item1 will be on the side and item2 on top
		 */

		DataTable rangeTable = new();
		var selectedSymbols = linesOptions.selectedSymbols;

		if (selectedSymbols.Count == 0) return rangeTable;

		var symbolToMassDict = MakeSymbolToMassDict(linesOptions);

		//Add Columns
		rangeTable.Columns.Add();
		rangeTable.Columns.Add();
		foreach (var symbolMassesPair in symbolToMassDict.Item2)
		{
			foreach(var _ in symbolMassesPair.Value)
				rangeTable.Columns.Add();
		}

		//add what we want for column headers
		List<object> row = new() { "", "" };
		foreach (var symbolMassesPair in symbolToMassDict.Item2)
		{
			foreach(var _ in symbolMassesPair.Value)
				row.Add(symbolMassesPair.Key);
		}
		rangeTable.Rows.Add(row.ToArray());

		//add secondary column information (ion weight)
		row = new() { "", "" };
		foreach (var symbolMassesPair in symbolToMassDict.Item2)
		{
			foreach(var mass in symbolMassesPair.Value)
				row.Add(mass.ToString("f2"));
		}
		rangeTable.Rows.Add(row.ToArray());

		HashSet<(float, float)> addedToTableSet = new();
		var keys1 = symbolToMassDict.Item1.Keys.ToList();
		var keys2 = symbolToMassDict.Item2.Keys.ToList();
		int rowCount = 0;
		for (int i = 0; i < keys1.Count; i++)
		{
			var ion1Formula = keys1[i];
			var ion1Masses = symbolToMassDict.Item1[ion1Formula];

			foreach(var mass1 in ion1Masses)
			{
				row = new() { ion1Formula, mass1.ToString("f2") };

				//add spaces
				//for (int k = 0; k < rowCount; k++)
				//	row.Add("");

				for (int j = 0; j < keys2.Count; j++)
				{
					var ion2Formula = keys2[j];
					var ion2Masses = symbolToMassDict.Item2[ion2Formula];

					foreach(var mass2 in ion2Masses)
					{
						var dtofSquared = Math.Pow(Math.Sqrt(mass1) - Math.Sqrt(mass2), 2);
						//string toAdd;
						//if (dtofSquared == 0)
						//	toAdd = "";
						//else
						//	toAdd = dtofSquared.ToString("f2");

						if(dtofSquared == 0)
							row.Add(dtofSquared.ToString("f2"));
						else if (!addedToTableSet.Contains((mass1, mass2)) && !addedToTableSet.Contains((mass2, mass1)))
						{
							row.Add(dtofSquared.ToString("f2"));
							addedToTableSet.Add((mass1, mass2));
							addedToTableSet.Add((mass2, mass1));
						}
						else
							row.Add("");

						
					}
				}
				rangeTable.Rows.Add(row.ToArray());
				rowCount++;
			}
		}

		return rangeTable;
	}

	public static List<Vector3[]> GetLinesSaxey(LinesOptions linesOptions, float maxHeight)
	{
		List<Vector3[]> lines = new();

		//var selectedSymbols = linesOptions.selectedSymbols;
		//var selectedCharges = linesOptions.selectedCharges;
		//var symbolToMassDict = MakeSymbolToMassDict(linesOptions);

		////y = ( sqrt(x) + (m2/c2 - m1/c1) )^2

		////this is essentially the resolution of the line
		//const float deltaX = .1f;

		//HashSet<(float, float)> addedLinesSet = new();
		//for (int i = 0; i < selectedSymbols.Count; i++)
		//{
		//	var ion1Sym = selectedSymbols[i];
		//	var ion1Charge = selectedCharges[i];
		//	var ion1Masses = symbolToMassDict[ion1Sym];

		//	foreach(var mass1 in ion1Masses)
		//	{
		//		for (int j = i + 1; j < selectedSymbols.Count; j++)
		//		{
		//			var ion2Sym = selectedSymbols[j];
		//			var ion2Charge = selectedCharges[j];
		//			var ion2Masses = symbolToMassDict[ion2Sym];

		//			foreach(var mass2 in ion2Masses)
		//			{
		//				if(!addedLinesSet.Contains((mass1, mass2)) && !addedLinesSet.Contains((mass2, mass1)))
		//				{
		//					List<Vector3> line = new();
		//					float xVal = 0f;
		//					float yVal = 0f;
		//					var dtof = Math.Abs(Math.Sqrt(mass1) - Math.Sqrt(mass2));
		//					addedLinesSet.Add((mass1, mass2));
		//					addedLinesSet.Add((mass2, mass1));
		//					do
		//					{
		//						yVal = (float)Math.Pow(Math.Sqrt(xVal) + dtof, 2);
		//						line.Add(new Vector3(xVal, -1, yVal));
		//						xVal += deltaX;
		//					} while (yVal <= maxHeight && xVal <= maxHeight);
		//					lines.Add(line.ToArray());
		//				}
		//			}
		//		}
		//	}
		//}

		return lines;
	}

	public static List<Vector3[]> GetLines2D(LinesOptions linesOptions, float height)
	{
		List<Vector3[]> lines = new();
		//float h = (float)Math.Sqrt(height);

		//var selectedSymbols = linesOptions.selectedSymbols;
		//var symbolToMassDict = MakeSymbolToMassDict(linesOptions);

		//HashSet<(float, float)> addedLinesSet = new();
		//for (int i = 0; i < selectedSymbols.Count; i++)
		//{
		//	var ion1 = selectedSymbols[i];
		//	var ion1Masses = symbolToMassDict[ion1];

		//	foreach(var mass1 in ion1Masses)
		//	{
		//		for (int j = i + 1; j < selectedSymbols.Count; j++)
		//		{
		//			var ion2 = selectedSymbols[j];
		//			var ion2Masses = symbolToMassDict[ion2];

		//			foreach(var mass2 in ion2Masses)
		//			{
		//				if(!addedLinesSet.Contains((mass1, mass2)) && !addedLinesSet.Contains((mass2, mass1)))
		//				{
		//					addedLinesSet.Add((mass1, mass2));
		//					addedLinesSet.Add((mass2, mass1));

		//					float dtof = (float)Math.Abs(Math.Sqrt(mass1) - Math.Sqrt(mass2));
		//					Vector3 point1 = new Vector3(0, -1, dtof);
		//					Vector3 point2 = new Vector3(h - dtof, -1, h);

		//					Vector3[] arr = new Vector3[2];
		//					arr[0] = point1;
		//					arr[1] = point2;
		//					lines.Add(arr);
		//				}
		//			}
		//		}
		//	}
		//}


		return lines;
	}

	public static List<Vector3[]> GetLines1D(LinesOptions linesOptions, int maxHeight)
	{
		List<Vector3[]> lines = new();

		//var selectedSymbols = linesOptions.selectedSymbols;
		//var symbolToMassDict = MakeSymbolToMassDict(linesOptions);

		//HashSet<(float, float)> addedLinesSet = new();

		//for (int i = 0; i < selectedSymbols.Count; i++)
		//{
		//	var ion1 = selectedSymbols[i];
		//	var ion1Masses = symbolToMassDict[ion1];

		//	foreach(var mass1 in ion1Masses)
		//	{
		//		for (int j = i + 1; j < selectedSymbols.Count; j++)
		//		{
		//			var ion2 = selectedSymbols[j];
		//			var ion2Masses = symbolToMassDict[ion2];

		//			foreach(var mass2 in ion2Masses)
		//			{
		//				if(!addedLinesSet.Contains((mass1, mass2)) && !addedLinesSet.Contains((mass2, mass1)))
		//				{
		//					addedLinesSet.Add((mass1, mass2));
		//					addedLinesSet.Add((mass2, mass1));

		//					float dtof = (float)Math.Abs(Math.Sqrt(mass1) - Math.Sqrt(mass2));
		//					var dtofSquared = dtof * dtof;
		//					Vector3 point1 = new(dtofSquared, -1, 0);
		//					Vector3 point2 = new(dtofSquared, -1, maxHeight);

		//					Vector3[] arr = new Vector3[2];
		//					arr[0] = point1;
		//					arr[1] = point2;
		//					lines.Add(arr);
		//				}
		//			}
		//		}
		//	}
		//}

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

public struct LinesOptions
{
	public List<(string, string)> selectedSymbols;
	//public List<IonFormula> selectedIons;
	public List<(int, int)> selectedCharges;
	public Dictionary<string, IElement> elements;
	public IIonFormulaIsotopeCalculator calculator;
	public IonFormulaIsotopeOptions calculatorOptions;
}
