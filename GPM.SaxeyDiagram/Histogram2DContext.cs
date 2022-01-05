using Cameca.CustomAnalysis.Interface.View;

namespace GPM.CustomAnalysis.SaxeyDiagram
{
    internal class Histogram2DContext : IHistogram2DContext
    {
        private readonly float[] saxeyMap;

        public int XBins { get; }
        public double XLowEdge { get; }
        public double XBinWidth { get; }
        public int YBins { get; }
        public double YLowEdge { get; }
        public double YBinWidth { get; }
        public float? OverrideMin { get; }

        public float GetBinValue(uint xBin, uint yBin)
        {
            return saxeyMap[yBin * XBins + xBin];
        }

        public Histogram2DContext(SaxeyDiagramOptions options, float[] saxeyMap)
        {
            this.saxeyMap = saxeyMap;
            XBins = options.EdgeSize;
            XLowEdge = options.XMin;
            XBinWidth = options.Resolution;
            YBins = options.EdgeSize;
            YLowEdge = options.YMin;
            YBinWidth = options.Resolution;
            OverrideMin = options.PlotZeroAsWhite ? (float?)0.0001f : null;
        }
    }
}
