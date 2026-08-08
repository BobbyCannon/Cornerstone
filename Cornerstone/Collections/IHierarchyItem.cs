#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Collections;

public interface IHierarchyItem
{
	#region Methods

	bool CanHaveChildren();

	bool CanOrder();

	IPresentationList GetChildren();

	int GetOrder();

	IHierarchyItem GetParent();

	void SetOrder(int value);

	void SetParent(IHierarchyItem parent);

	#endregion
}