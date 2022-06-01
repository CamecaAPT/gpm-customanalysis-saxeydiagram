using System;
using System.Collections.ObjectModel;
using Cameca.CustomAnalysis.Interface;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class Histogram2DContentViewModel : IDisposable
{
	public string Title { get; }

	public ObservableCollection<IRenderData> Histogram2DRenderData { get; }

	public Histogram2DContentViewModel(string title, IHistogram2DRenderData content)
	{
		Title = title;
		Histogram2DRenderData = new ObservableCollection<IRenderData> { content };
	}

	public void Dispose()
	{
		foreach (var item in Histogram2DRenderData)
		{
			if (item is IDisposable disposable)
				disposable.Dispose();
		}
	}
}
