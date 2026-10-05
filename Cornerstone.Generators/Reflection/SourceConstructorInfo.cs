#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public partial class SourceConstructorInfo
{
	#region Fields

	private Func<object[], object> _invoke;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; set; }
	public SourceAttributeInfo[] Attributes { get; set; }

	/// <summary>
	/// Optional runtime handle. Generated maps must not use this to construct; Native AOT requires <see cref="Invoke"/>.
	/// </summary>
	public ConstructorInfo ConstructorInfo { get; set; }

	/// <summary>
	/// Construct the type. Generated source reflection always assigns a compiled lambda so Native AOT never binds ConstructorInfo.Invoke.
	/// </summary>
	public Func<object[], object> Invoke
	{
		get
		{
			if (_invoke != null)
			{
				return _invoke;
			}

			if (ConstructorInfo == null)
			{
				throw new InvalidOperationException("Source constructor has no compiled Invoke and ConstructorInfo is not available.");
			}

			return ConstructorInfo.Invoke;
		}
		set => _invoke = value;
	}

	public bool IsDependencyConstructor { get; set; }
	public bool IsStatic { get; set; }
	public string Name { get; set; }
	public SourceParameterInfo[] Parameters { get; set; }

	#endregion
}