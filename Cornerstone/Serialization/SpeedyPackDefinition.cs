namespace Cornerstone.Serialization;

public class SpeedyPackDefinition<T>
{
	#region Properties

	public T ArrayDelimiter { get; set; }
	public T ArrayEnd { get; set; }
	public T ArrayStart { get; set; }
	public T ObjectDelimiter { get; set; }
	public T ObjectEnd { get; set; }
	public T ObjectPropertyDelimiter { get; set; }
	public bool ObjectPropertyName { get; set; }
	public T ObjectStart { get; set; }
	public T StringEnd { get; set; }
	public T StringEscape { get; set; }
	public T StringStart { get; set; }

	#endregion
}