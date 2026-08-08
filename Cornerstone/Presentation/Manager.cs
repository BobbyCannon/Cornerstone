#region References

using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a manager.
/// </summary>
[SourceReflection]
public partial class Manager : ViewModel, IManager
{
	#region Methods

	public virtual void Update()
	{
	}

	#endregion
}

/// <summary>
/// Represents a manager.
/// </summary>
public interface IManager : IViewModel
{
	#region Methods

	/// <summary>
	/// The method to call on a worker thread to process the manager.
	/// </summary>
	void Update();

	#endregion
}