namespace Cornerstone.Presentation;

/// <summary>
/// How a collection dispatch binding reconciles the destination with the source.
/// </summary>
public enum CollectionReconcileMode
{
	/// <summary>
	/// Add missing / remove extras only (see <c> ReconcileList </c>).
	/// Does not update existing item fields or force full reorder of survivors beyond appending new items.
	/// </summary>
	List = 0,

	/// <summary>
	/// Add / remove / update items and align order (see <c> ReconcileListAndItems </c>).
	/// </summary>
	ListAndItems = 1
}