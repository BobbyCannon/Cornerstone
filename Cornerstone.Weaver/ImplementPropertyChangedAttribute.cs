#region References

using System;

#endregion

namespace Cornerstone.Weaver;

/// <summary>
/// Include a <see cref="Type" /> for notification.
/// The INotifyPropertyChanged interface is added to the type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ImplementPropertyChangedAttribute : Attribute;