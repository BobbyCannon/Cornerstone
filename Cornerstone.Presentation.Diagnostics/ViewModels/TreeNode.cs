#region References

using System;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Presentation.Diagnostics.ViewModels;

public abstract class TreeNode : ViewModel, IDisposable
{
	#region Fields

	private string _classes;
	private readonly IDisposable _classesSubscription;
	private bool _isExpanded;

	#endregion

	#region Constructors

	protected TreeNode(PresentationObject cornerstoneObject, TreeNode parent, string customTypeName = null)
	{
		_classes = string.Empty;

		Parent = parent;
		Type = customTypeName ?? cornerstoneObject.GetType().Name;
		Visual = cornerstoneObject;
		//FontWeight = IsRoot ? FontWeight.Bold : FontWeight.Normal;
		ElementName = (cornerstoneObject as INamed)?.Name;

		if (cornerstoneObject is StyledElement { Classes: { } classes })
		{
			_classesSubscription = ((IObservable<object>) classes.GetWeakCollectionChangedObservable())
				.StartWith(null)
				.Subscribe(_ =>
				{
					Classes = classes.Count > 0
						? $"({string.Join(" ", classes)})"
						: string.Empty;
				});
		}
	}

	#endregion

	#region Properties

	public abstract TreeNodeCollection Children { get; }

	public string Classes
	{
		get => _classes;
		private set => SetProperty(ref _classes, value);
	}

	public string ElementName { get; }

	public bool IsExpanded
	{
		get => _isExpanded;
		set => SetProperty(ref _isExpanded, value);
	}

	public TreeNode Parent { get; }

	public string Type { get; private set; }

	public PresentationObject Visual { get; }

	private bool IsRoot =>
		Visual is TopLevel 
		|| Visual is ContextMenu
		|| Visual is IPopupHost;

	#endregion

	#region Methods

	public void Dispose()
	{
		_classesSubscription?.Dispose();
		Children.Dispose();
	}

	#endregion
}