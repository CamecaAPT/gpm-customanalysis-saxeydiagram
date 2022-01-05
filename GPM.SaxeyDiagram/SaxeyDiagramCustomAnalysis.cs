using System.Windows.Media;
using Cameca.CustomAnalysis.Interface.CustomAnalysis;
using Cameca.CustomAnalysis.Interface.IonData;
using Cameca.CustomAnalysis.Interface.View;

namespace GPM.CustomAnalysis.SaxeyDiagram
{
    internal class SaxeyDiagramCustomAnalysis : ICustomAnalysis<SaxeyDiagramOptions>
    {
        private readonly IColorMapProvider colorMapProvider;
        private readonly IIonDataReader ionDataReader;

        public SaxeyDiagramCustomAnalysis(IColorMapProvider colorMapProvider, IIonDataReader ionDataReader)
        {
            this.colorMapProvider = colorMapProvider;
            this.ionDataReader = ionDataReader;
        }

        public IIonData Run(IIonData ionData, IAnalysisTreeNode parentNode, SaxeyDiagramOptions options, IViewBuilder viewBuilder)
		{
			// Check for proper APT sections.  TODO - generate them if needed.
            var sectionInfo = ionData.GetIonDataInfo().SectionInfo;

            if (!sectionInfo.ContainsKey("Mass") || !sectionInfo.ContainsKey("Multiplicity") && !sectionInfo.ContainsKey("pulse"))
            {
                viewBuilder.AddText("Error", "Missing section(s) in the APT file.  \"Mass\" and \"Multiplicity\" are required.");
                return ionData;
            }

            bool hasMultiplicity = sectionInfo.ContainsKey("Multiplicity");

            // Init Saxey diagram
            var saxey = new CSaxeyDiagram();

            // Build Saxey diagram
            saxey.Build(options, ionDataReader, ionData, hasMultiplicity);

            // Display Result in IVAS
            var histogram2dContext = new Histogram2DContext(options, saxey.Map);
            var colorMap = colorMapProvider.CreateDefaultBright();
            colorMap.OutOfRangeBottom = Colors.White;

            viewBuilder.AddHistogram2D(histogram2dContext, "Saxey Diagram", "M/n (da)", "M/n (da)", colorMap);

            if (options.ExportToCsv)
            {
                var csvName = ionData.Filename + ".SP.csv";
                saxey.ExportToCsvTable(csvName, out _, options);
            }

            return ionData;
		}
    }
}
