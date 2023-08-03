# GPM.CustomAnalysis.SaxeyDiagram

Custom analysis to display a multi-hit event correlation table.

## Time Space and Multi-hit Atom Mass Spectrum Usage
Add Pairs of Ions to be graphed on any combination of the three charts: 
- The original Saxey Diagram
- The Time Space (linearized) Saxey Diagram
- The Multi-hit Atom Mass Spectrum

Double click on the Ion Pair to remove it from the graphs, or click 'Remove Ions' to remove all at once.
When adding ion pairs, you can give the charge state by giving 1-5 '+' signs. If no plus signs are given, a charge state of 1 is assumed.
Correct capitalization formatting is required to remove ambiquity from differentiating ions (e.g. CO vs Co)
Ions with multiple commonly occuring isotopes will have all of them graphed with their respective pair(s) of other ions.

Additionally, for the multi-hit atom mass spectrum, there is an option specifying the bin size for the histogram.

## Range Table
The range table is automatically populated from the Ion Pair selections made for the line graphing. The first ion goes on the Y axis, and the second on the X axis.
Ions with multiple commonly occurring isotopes will have all of them listed on their respective axis.

### Other Notes
- Lines will not be drawn on the mirrored plot versions of the Saxey Diagram.
