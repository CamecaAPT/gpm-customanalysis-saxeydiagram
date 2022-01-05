using System;
using Cameca.CustomAnalysis.Interface.CustomAnalysis;
using Prism.Ioc;
using Prism.Modularity;

namespace GPM.CustomAnalysis.SaxeyDiagram
{
    [ModuleDependency("IvasModule")]
    public class SaxeyDiagramModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register any additional dependencies with the Unity IoC container
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var customAnalysisService = containerProvider.Resolve<ICustomAnalysisService>();

            customAnalysisService.Register<SaxeyDiagramCustomAnalysis, SaxeyDiagramOptions>(
                new CustomAnalysisDescription("GPM_SaxeyDiagram", "GPM Saxey Diagram", new Version()));
        }
    }
}
