namespace Cornerstone.Data.TypeActivators;

/// <inheritdoc />
public class StringTypeActivator : TypeActivator<string>
{
	#region Methods

	/// <inheritdoc />
	public override object CreateInstance(params object[] arguments)
	{
		return arguments.Length > 0
			? arguments[0].ToString()
			: string.Empty;
	}

	#endregion
}