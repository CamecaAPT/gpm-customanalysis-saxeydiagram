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

		List<string> badElements = new();

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
				badElements.Add(element);
				//MessageBox.Show($"{element} is not a valid element");
				//return null;
			}

			components.Add(new IonFormula.Component(element, num));
		}
		
		if(badElements.Count > 0)
		{
			StringBuilder sb = new();
			for(int i=0; i<badElements.Count; i++)
			{
				sb.Append(badElements[i]);
				if (i + 1 < badElements.Count)
					sb.Append(", ");
			}

			string phrase = badElements.Count == 1 ? "is not a valid element" : "are not valid elements";

			MessageBox.Show($"{sb} {phrase}");
			return null;
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
	public static bool ValidateIonString(string ionString, out Match match, int? ionNum = null)
	{
		Regex regex = new(@"(?:([A-Z][a-z]?\d*))+([+]{0,5})");
		match = regex.Match(ionString);
		if(!match.Success || match.Value != ionString)
		{
			string ionNumStr = ionNum == null ? "" : ((int)ionNum).ToString();
			MessageBox.Show($"Invalid format for Ion {ionNumStr}");
			return false;
		}
		return true;
	}

	public static Dictionary<string, List<float>> MakeSymbolToMassDict(LinesOptions linesOptions, List<string> symbols, List<int> charges)
	{
		var elements = linesOptions.elements;
		var calculator = linesOptions.calculator;
		var calculatorOptions = linesOptions.calculatorOptions;

		//TODO: clean this up perhaps
		List<IonFormula> selectedIons = new();
		foreach (var symbol in symbols)
		{
			ValidateIonString(symbol, out var match);
			selectedIons.Add(IonFormulaFromMatch(match, elements, out var _)!);
		}

		Dictionary<string, List<float>> symbolToMassDict = new();

		for (int i = 0; i < selectedIons.Count; i++)
		{
			//1
			var sym = symbols[i];
			var formula = selectedIons[i];
			var charge = charges[i];
			if (!symbolToMassDict.ContainsKey(sym))
				symbolToMassDict.Add(sym, new());

			var isotopes1 = calculator.GetIsotopes(formula, calculatorOptions);

			foreach (var isotope in isotopes1)
				symbolToMassDict[sym].Add((float)isotope.Mass / charge);
		}

		return symbolToMassDict;
	}

	public static void BuildRangeTable(DataTable rangeTable, LinesOptions linesOptions, List<string> symbolsX, List<string> symbolsY, List<int> chargesX, List<int> chargesY, int decimalPlaces)
	{
		/*
		 * For now, Item1 will be on the side and item2 on top
		 */

		if (symbolsX.Count == 0 || symbolsY.Count == 0) return;


		var symbolToMassDictX = MakeSymbolToMassDict(linesOptions, symbolsX, chargesX);
		var symbolToMassDictY = MakeSymbolToMassDict(linesOptions, symbolsY, chargesY);

		//Add Columns
		rangeTable.Columns.Add();
		rangeTable.Columns.Add();
		foreach (var symbolMassesPair in symbolToMassDictX)
		{
			foreach(var _ in symbolMassesPair.Value)
				rangeTable.Columns.Add();
		}

		//add what we want for column headers
		List<object> row = new() { "(mass/charge)", "" };
		foreach (var symbolMassesPair in symbolToMassDictX)
		{
			foreach(var _ in symbolMassesPair.Value)
				row.Add(symbolMassesPair.Key);
		}
		rangeTable.Rows.Add(row.ToArray());

		//add secondary column information (ion weight)
		row = new() { "", "" };
		foreach (var symbolMassesPair in symbolToMassDictX)
		{
			foreach(var mass in symbolMassesPair.Value)
				row.Add(mass.ToString($"f{decimalPlaces}"));
		}
		rangeTable.Rows.Add(row.ToArray());

		var keysX = symbolToMassDictX.Keys.ToList();
		var keysY = symbolToMassDictY.Keys.ToList();
		int rowCount = 0;
		for (int i = 0; i < keysY.Count; i++)
		{
			var ion1Formula = keysY[i];
			var ion1Masses = symbolToMassDictY[ion1Formula];

			foreach(var mass1 in ion1Masses)
			{
				row = new() { ion1Formula, mass1.ToString($"f{decimalPlaces}") };

				for (int j = 0; j < keysX.Count; j++)
				{
					var ion2Formula = keysX[j];
					var ion2Masses = symbolToMassDictX[ion2Formula];

					foreach(var mass2 in ion2Masses)
					{
						var dtofSquared = Math.Pow(Math.Sqrt(mass1) - Math.Sqrt(mass2), 2);
						if (dtofSquared != 0)
							row.Add(dtofSquared.ToString($"f{decimalPlaces}"));
						else
							row.Add("");
					}
				}
				rangeTable.Rows.Add(row.ToArray());
				rowCount++;
			}
		}
	}

	public static (List<Vector3[]>, List<string>, List<Color>) GetDissociationLine(LinesOptions linesOptions, LineDefinition lineDef, (int, int) chargePair, float maxY)
	{
		List<Vector3[]> pointsList = new();
		List<string> namesList = new();
		List<Color> colorsList = new();

		var symbolToMassDict1 = MakeSymbolToMassDict(linesOptions, new() { lineDef.Ion1 }, new() { chargePair.Item1 });
		var symbolToMassDict2 = MakeSymbolToMassDict(linesOptions, new() { lineDef.Ion2 }, new() { chargePair.Item2 });

		const float step = .01f;

		foreach (var mass1 in symbolToMassDict1[lineDef.Ion1])
		{
			var ma = mass1 * chargePair.Item1;
			var na = chargePair.Item1;
			foreach(var mass2 in symbolToMassDict2[lineDef.Ion2])
			{
				List<Vector3> line = new();

				var mb = mass2 * chargePair.Item2;
				var nb = chargePair.Item2;

				if(mass1 > mass2)
				{
					var tempM = mb;
					var tempN = nb;
					mb = ma;
					nb = na;
					ma = tempM;
					na = tempN;
				}

				double t = 0;
				double x;
				double y = mb / ((mb / (ma + mb) * t * (na + nb) + (1 - t) * nb));
				
				if (y > maxY)
					t = (mb - (nb * maxY)) / ((mb / (ma + mb)) * (na + nb) * maxY - (nb * maxY));

				while (t < 1)
				{
					x = ma / ((ma / (ma + mb) * t * (na + nb) + (1 - t) * na));
					y = mb / ((mb / (ma + mb) * t * (na + nb) + (1 - t) * nb));

					line.Add(new((float)x, -1, (float)y));
					t += step;
				}

				pointsList.Add(line.ToArray());

				StringBuilder sb = new();
				sb.Append($"({lineDef.Ion1}");
				if (symbolToMassDict1[lineDef.Ion1].Count > 1)
					sb.Append($"[{mass1.ToString("f1")}]");
				sb.Append($", {lineDef.Ion2}");
				if (symbolToMassDict2[lineDef.Ion2].Count > 1)
					sb.Append($"[{mass2.ToString("f2")}]");
				sb.Append(") - Dissociation");

				namesList.Add(sb.ToString());


				colorsList.Add(lineDef.Color);
			}
		}


		return (pointsList, namesList, colorsList);
	}

	public static (List<Vector3[]>, List<string>, List<Color>) GetLinesSaxey(LinesOptions linesOptions, List<LineDefinition> selectedSymbols, List<(int, int)> selectedCharges, float maxHeight)
	{
		List<Vector3[]> lines = new();
		List<string> lineLabels = new();
		List<Color> lineColors = new();

		List<string> selectedSymbols1 = new();
		List<string> selectedSymbols2 = new();
		List<int> selectedCharges1 = new();
		List<int> selectedCharges2 = new();
		List<Color> selectedColors = new();
		for (int i = 0; i < selectedSymbols.Count; i++)
		{
			if (selectedSymbols[i].IsDissociation)
			{
				(var points, var name, var color) = GetDissociationLine(linesOptions, selectedSymbols[i], selectedCharges[i], maxHeight);
				lines.AddRange(points);
				lineLabels.AddRange(name);
				lineColors.AddRange(color);
			}

			if (!selectedSymbols[i].IsVisible) continue;

			selectedSymbols1.Add(selectedSymbols[i].Ion1);
			selectedSymbols2.Add(selectedSymbols[i].Ion2);

			selectedCharges1.Add(selectedCharges[i].Item1);
			selectedCharges2.Add(selectedCharges[i].Item2);

			selectedColors.Add(selectedSymbols[i].LineColor.Color);
		}

		var symbolToMassDict1 = MakeSymbolToMassDict(linesOptions, selectedSymbols1, selectedCharges1);
		var symbolToMassDict2 = MakeSymbolToMassDict(linesOptions, selectedSymbols2, selectedCharges2);

		//y = ( sqrt(x) + (m2/c2 - m1/c1) )^2

		//this is essentially the resolution of the line
		const float deltaX = .1f;

		HashSet<(float, float)> addedLinesSet = new();
		for (int i = 0; i < selectedSymbols1.Count; i++)
		{
			var ion1Sym = selectedSymbols1[i];
			var ion1Masses = symbolToMassDict1[ion1Sym];

			var ion2Sym = selectedSymbols2[i];
			var ion2Masses = symbolToMassDict2[ion2Sym];

			foreach(var mass1 in ion1Masses)
			{
				foreach(var mass2 in ion2Masses)
				{
					if (!addedLinesSet.Contains((mass1, mass2)) && !addedLinesSet.Contains((mass2, mass1)))
					{
						List<Vector3> line = new();
						float xVal = 0f;
						float yVal = 0f;
						var dtof = Math.Abs(Math.Sqrt(mass1) - Math.Sqrt(mass2));
						addedLinesSet.Add((mass1, mass2));
						addedLinesSet.Add((mass2, mass1));
						do
						{
							yVal = (float)Math.Pow(Math.Sqrt(xVal) + dtof, 2);
							line.Add(new Vector3(xVal, -1, yVal));
							xVal += deltaX;
						} while (yVal <= maxHeight && xVal <= maxHeight);
						lines.Add(line.ToArray());

						StringBuilder sb = new();
						sb.Append($"({ion1Sym}");
						if(ion1Masses.Count > 1)
							sb.Append($"[{mass1.ToString("f1")}]");
						sb.Append($", {ion2Sym}");
						if (ion2Masses.Count > 1)
							sb.Append($"[{mass2.ToString("f2")}]");
						sb.Append(')');

						lineLabels.Add(sb.ToString());

						lineColors.Add(selectedColors[i]);
					}
				}
			}
		}

		return (lines, lineLabels, lineColors);
	}

	public static (List<Vector3[]>, List<string>, List<Color>) GetLines2D(LinesOptions linesOptions, List<LineDefinition> selectedSymbols, List<(int, int)> selectedCharges, float height)
	{
		List<Vector3[]> lines = new();
		List<string> lineLabels = new();
		List<Color> lineColors = new();
		float h = (float)Math.Sqrt(height);

		List<string> selectedSymbols1 = new();
		List<string> selectedSymbols2 = new();
		List<int> selectedCharges1 = new();
		List<int> selectedCharges2 = new();
		List<Color> selectedColors = new();
		for (int i = 0; i < selectedSymbols.Count; i++)
		{
			if (!selectedSymbols[i].IsVisible) continue;

			selectedSymbols1.Add(selectedSymbols[i].Ion1);
			selectedSymbols2.Add(selectedSymbols[i].Ion2);

			selectedCharges1.Add(selectedCharges[i].Item1);
			selectedCharges2.Add(selectedCharges[i].Item2);

			selectedColors.Add(selectedSymbols[i].LineColor.Color);
		}

		var symbolToMassDict1 = MakeSymbolToMassDict(linesOptions, selectedSymbols1, selectedCharges1);
		var symbolToMassDict2 = MakeSymbolToMassDict(linesOptions, selectedSymbols2 ,selectedCharges2);

		HashSet<(float, float)> addedLinesSet = new();
		for (int i = 0; i < selectedSymbols1.Count; i++)
		{
			var ion1 = selectedSymbols1[i];
			var ion1Masses = symbolToMassDict1[ion1];

			var ion2 = selectedSymbols2[i];
			var ion2Masses = symbolToMassDict2[ion2];

			foreach(var mass1 in ion1Masses)
			{
				foreach(var mass2 in ion2Masses)
				{
					if (!addedLinesSet.Contains((mass1, mass2)) && !addedLinesSet.Contains((mass2, mass1)))
					{
						addedLinesSet.Add((mass1, mass2));
						addedLinesSet.Add((mass2, mass1));

						float dtof = (float)Math.Abs(Math.Sqrt(mass1) - Math.Sqrt(mass2));
						Vector3 point1 = new Vector3(0, -1, dtof);
						Vector3 point2 = new Vector3(h - dtof, -1, h);

						Vector3[] arr = new Vector3[2];
						arr[0] = point1;
						arr[1] = point2;
						lines.Add(arr);

						StringBuilder sb = new();
						sb.Append($"({ion1}");
						if (ion1Masses.Count > 1)
							sb.Append($"[{mass1.ToString("f1")}]");
						sb.Append($", {ion2}");
						if (ion2Masses.Count > 1)
							sb.Append($"[{mass2.ToString("f2")}]");
						sb.Append(')');

						lineLabels.Add(sb.ToString());

						lineColors.Add(selectedColors[i]);
					}
				}
			}
		}


		return (lines, lineLabels, lineColors);
	}

	public static (List<Vector3[]>, List<string>, List<Color>) GetLines1D(LinesOptions linesOptions, List<LineDefinition> selectedSymbols, List<(int, int)> selectedCharges, int maxHeight)
	{
		List<Vector3[]> lines = new();
		List<string> lineLabels = new();
		List<Color> lineColors = new();

		List<string> selectedSymbols1 = new();
		List<string> selectedSymbols2 = new();
		List<int> selectedCharges1 = new();
		List<int> selectedCharges2 = new();
		List<Color> selectedColors = new();
		for (int i = 0; i < selectedSymbols.Count; i++)
		{
			if (!selectedSymbols[i].IsVisible) continue;

			selectedSymbols1.Add(selectedSymbols[i].Ion1);
			selectedSymbols2.Add(selectedSymbols[i].Ion2);

			selectedCharges1.Add(selectedCharges[i].Item1);
			selectedCharges2.Add(selectedCharges[i].Item2);

			selectedColors.Add(selectedSymbols[i].LineColor.Color);
		}

		var symbolToMassDict1 = MakeSymbolToMassDict(linesOptions, selectedSymbols1, selectedCharges1);
		var symbolToMassDict2 = MakeSymbolToMassDict(linesOptions, selectedSymbols2 ,selectedCharges2);

		HashSet<(float, float)> addedLinesSet = new();

		for (int i = 0; i < selectedSymbols1.Count; i++)
		{
			var ion1 = selectedSymbols1[i];
			var ion1Masses = symbolToMassDict1[ion1];

			var ion2 = selectedSymbols2[i];
			var ion2Masses = symbolToMassDict2[ion2];

			foreach(var mass1 in ion1Masses)
			{
				foreach(var mass2 in ion2Masses)
				{
					if (!addedLinesSet.Contains((mass1, mass2)) && !addedLinesSet.Contains((mass2, mass1)))
					{
						addedLinesSet.Add((mass1, mass2));
						addedLinesSet.Add((mass2, mass1));

						float dtof = (float)Math.Abs(Math.Sqrt(mass1) - Math.Sqrt(mass2));
						var dtofSquared = dtof * dtof;
						Vector3 point1 = new(dtofSquared, -1, 0);
						Vector3 point2 = new(dtofSquared, -1, maxHeight);

						Vector3[] arr = new Vector3[2];
						arr[0] = point1;
						arr[1] = point2;
						lines.Add(arr);

						StringBuilder sb = new();
						sb.Append($"({ion1}");
						if (ion1Masses.Count > 1)
							sb.Append($"[{mass1.ToString("f1")}]");
						sb.Append($", {ion2}");
						if (ion2Masses.Count > 1)
							sb.Append($"[{mass2.ToString("f2")}]");
						sb.Append(')');

						lineLabels.Add(sb.ToString());

						lineColors.Add(selectedColors[i]);
					}
				}
			}
		}

		return (lines, lineLabels, lineColors);
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
	public Dictionary<string, IElement> elements;
	public IIonFormulaIsotopeCalculator calculator;
	public IonFormulaIsotopeOptions calculatorOptions;
}
