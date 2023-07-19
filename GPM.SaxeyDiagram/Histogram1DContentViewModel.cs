using Cameca.CustomAnalysis.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPM.CustomAnalysis.SaxeyDiagram;
internal class Histogram2DHistogram1DSideBySideViewModel : IDisposable
{
	public string Title { get; }

	public ObservableCollection<IRenderData> Histogram2DRenderData { get; }

	public ObservableCollection<IRenderData> Histogram1DRenderData { get; }

	public Histogram2DHistogram1DSideBySideViewModel(string title, IHistogram2DRenderData content2D, IHistogramRenderData content1D, List<ILineRenderData> lines2D, List<ILineRenderData> lines1D)
	{
		Title = title;
		//Histogram2DRenderData = new ObservableCollection<IRenderData>();
		Histogram2DRenderData = new ObservableCollection<IRenderData> { content2D };
		Histogram2DRenderData.AddRange(lines2D);
		//Histogram2DRenderData.Add(content2D);


		Histogram1DRenderData = new ObservableCollection<IRenderData> { content1D };
		Histogram1DRenderData.AddRange(lines1D);
	}

	public void Dispose()
	{
		foreach (var item in Histogram2DRenderData)
		{
			if (item is IDisposable disposable)
				disposable.Dispose();
		}

		foreach(var item in Histogram1DRenderData)
		{
			if(item is IDisposable disposable)
				disposable.Dispose();
		}
	}
}
