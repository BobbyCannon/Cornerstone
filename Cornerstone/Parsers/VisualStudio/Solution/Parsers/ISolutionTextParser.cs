namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

internal interface ISolutionTextParser<out TReturned>
{
	#region Methods

	TReturned Parse(string slnText);

	#endregion
}