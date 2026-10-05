namespace Cornerstone.Text.Parsing.VisualStudio.Solution.Classic.Parsers;

internal interface ISolutionTextParser<out TReturned>
{
	#region Methods

	TReturned Parse(string slnText);

	#endregion
}