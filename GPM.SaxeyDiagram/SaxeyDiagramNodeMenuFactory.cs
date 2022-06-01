using Cameca.CustomAnalysis.Interface;
using Prism.Commands;
using Prism.Events;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class SaxeyDiagramNodeMenuFactory : IAnalysisMenuFactory
{
	public const string UniqueId = "GPM.CustomAnalysis.SaxeyDiagram.SaxeyDiagramNodeMenuFactory";

	private readonly IEventAggregator _eventAggregator;

	public SaxeyDiagramNodeMenuFactory(IEventAggregator eventAggregator)
	{
		_eventAggregator = eventAggregator;
	}

	public IMenuItem CreateMenuItem(IAnalysisMenuContext context) => new MenuAction(
		SaxeyDiagramNode.DisplayInfo.Title,
		new DelegateCommand(() => _eventAggregator.PublishCreateNode(
			SaxeyDiagramNode.UniqueId,
			context.NodeId,
			SaxeyDiagramNode.DisplayInfo.Title,
			SaxeyDiagramNode.DisplayInfo.Icon)),
		SaxeyDiagramNode.DisplayInfo.Icon);

	public AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
