#region References

using Cornerstone.Extensions;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Automation;

/// <summary>
/// Represents an element.
/// </summary>
public abstract class Element : ElementHost
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of an element.
	/// </summary>
	/// <param name="application"> The application this element exists in. </param>
	/// <param name="parent"> The parent of this element. </param>
	protected Element(Application application, ElementHost parent)
		: base(application, parent)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the automation id of the element.
	/// </summary>
	public abstract string AutomationId { get; }

	/// <summary>
	/// Gets the full id of the element which include all parent IDs prefixed to this element ID.
	/// Includes full host namespace. Ex. GrandParent,Parent,Element
	/// </summary>
	public string FullId
	{
		get
		{
			using var rented = StringBuilderPool.Rent();
			var builder = rented.Value;

			try
			{
				var element = (ElementHost) this;

				do
				{
					builder.Insert(0, StringExtensions.FirstNotNullOrEmptyValue([element.Id, element.Name, " "]) + ",");
					element = element.Parent;
				} while (element != null);

				builder.Remove(builder.Length - 1, 1);
				return builder.ToString();
			}
			finally
			{
				StringBuilderPool.Return(builder);
			}
		}
	}

	#endregion
}