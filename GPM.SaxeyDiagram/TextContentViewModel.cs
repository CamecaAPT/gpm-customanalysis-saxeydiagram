namespace GPM.CustomAnalysis.SaxeyDiagram;

internal class TextContentViewModel
{
	public string Title { get; }
	public string Content { get; }

	public TextContentViewModel(string title, string content)
	{
		Title = title;
		Content = content;
	}
}
