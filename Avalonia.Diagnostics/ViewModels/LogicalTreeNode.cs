#region References

using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Diagnostics.Controls;
using Avalonia.Diagnostics.Views;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class LogicalTreeNode : TreeNode
{
	#region Constructors

	public LogicalTreeNode(AvaloniaObject avaloniaObject, TreeNode parent)
		: base(avaloniaObject, parent)
	{
		Children = avaloniaObject switch
		{
			ILogical logical => new LogicalTreeNodeCollection(this, logical),
			TopLevelGroup host => new TopLevelGroupHostLogical(this, host),
			_ => TreeNodeCollection.Empty
		};
	}

	#endregion

	#region Properties

	public override TreeNodeCollection Children { get; }

	#endregion

	#region Methods

	public static LogicalTreeNode[] Create(object control)
	{
		return control is AvaloniaObject logical
			? [new LogicalTreeNode(logical, null)]
			: [];
	}

	#endregion

	#region Classes

	public class LogicalTreeNodeCollection : TreeNodeCollection
	{
		#region Fields

		private readonly ILogical _control;
		private IDisposable _subscription;

		#endregion

		#region Constructors

		public LogicalTreeNodeCollection(TreeNode owner, ILogical control)
			: base(owner)
		{
			_control = control;
		}

		#endregion

		#region Methods

		public override void Dispose()
		{
			base.Dispose();
			_subscription?.Dispose();
		}

		protected override void Initialize(AvaloniaList<TreeNode> nodes)
		{
			_subscription = _control.LogicalChildren.ForEachItem(
				(i, item) => nodes.Insert(i, new LogicalTreeNode((AvaloniaObject) item, Owner)),
				(i, item) => nodes.RemoveAt(i),
				() => nodes.Clear());
		}

		#endregion
	}

	public class TopLevelGroupHostLogical : TreeNodeCollection
	{
		#region Fields

		private readonly TopLevelGroup _group;
		private readonly CompositeDisposable _subscriptions = new(1);

		#endregion

		#region Constructors

		public TopLevelGroupHostLogical(TreeNode owner, TopLevelGroup host) :
			base(owner)
		{
			_group = host;
		}

		#endregion

		#region Methods

		public override void Dispose()
		{
			_subscriptions?.Dispose();
			base.Dispose();
		}

		protected override void Initialize(AvaloniaList<TreeNode> nodes)
		{
			for (var i = 0; i < _group.Items.Count; i++)
			{
				var window = _group.Items[i];
				if (window is MainWindow)
				{
					continue;
				}
				nodes.Add(new LogicalTreeNode(window, Owner));
			}

			void GroupOnAdded(object sender, TopLevel e)
			{
				if (e is MainWindow)
				{
					return;
				}

				nodes.Add(new LogicalTreeNode(e, Owner));
			}

			void GroupOnRemoved(object sender, TopLevel e)
			{
				if (e is MainWindow)
				{
					return;
				}

				nodes.Add(new LogicalTreeNode(e, Owner));
			}

			_group.Added += GroupOnAdded;
			_group.Removed += GroupOnRemoved;

			_subscriptions.Add(new Disposable.AnonymousDisposable(() =>
			{
				_group.Added -= GroupOnAdded;
				_group.Removed -= GroupOnRemoved;
			}));
		}

		#endregion
	}

	#endregion
}