namespace Cornerstone.Data.TypeActivators;

/// <inheritdoc />
public class StringTypeActivator : TypeActivator<string>
{
	#region Constructors

	/// <inheritdoc />
	public StringTypeActivator() : base(CreateInstanceInternal)
	{
	}

	#endregion

	#region Methods

	private static string CreateInstanceInternal(params object[] arguments)
	{
		return (arguments.Length > 0)
			&& (arguments[0] is string sValue)
				? sValue
				: string.Empty;
	}

	#endregion
}