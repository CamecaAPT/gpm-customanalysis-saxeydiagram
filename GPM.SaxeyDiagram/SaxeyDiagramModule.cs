using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Ioc;
using Prism.Modularity;

namespace GPM.CustomAnalysis.SaxeyDiagram;

public class SaxeyDiagramModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.AddCustomAnalysisUtilities();

        containerRegistry.Register<object, SaxeyDiagramNode>(SaxeyDiagramNode.UniqueId);
        containerRegistry.RegisterInstance<INodeDisplayInfo>(SaxeyDiagramNode.DisplayInfo, SaxeyDiagramNode.UniqueId);
        containerRegistry.Register<IAnalysisMenuFactory, SaxeyDiagramNodeMenuFactory>(SaxeyDiagramNodeMenuFactory.UniqueId);
        containerRegistry.Register<object, SaxeyDiagramViewModel>(SaxeyDiagramViewModel.UniqueId);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();
        extensionRegistry.RegisterAnalysisView<SaxeyDiagramView, SaxeyDiagramViewModel>(AnalysisViewLocation.Top);
    }
}