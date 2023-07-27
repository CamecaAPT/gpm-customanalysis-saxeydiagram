using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class RangeTableViewModel : IDisposable
{
	public string Title { get; }

	public DataTable RangeTable { get; set; }

	public RangeTableViewModel(string title, DataTable rangeTable)
	{
		RangeTable = rangeTable;
		Title = title;
	}

	public void Dispose()
	{
		RangeTable.Dispose();
	}
}
