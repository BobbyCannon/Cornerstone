#region References

using System;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Diagnostics.Views;
using Avalonia.Reactive;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class VisualTreeNode : TreeNode
{
	#region Constructors

	public VisualTreeNode(AvaloniaObject avaloniaObject, TreeNode parent, string customName = null)
		: base(avaloniaObject, parent, customName)
	{
		Children = avaloniaObject switch
		{
			Visual visual => new VisualTreeNodeCollection(this, visual),
			Controls.Application host => new ApplicationHostVisuals(this, host),
			_ => TreeNodeCollection.Empty
		};

		if (Visual is StyledElement styleable)
		{
			IsInTemplate = styleable.TemplatedParent != null;
		}
	}

	#endregion

	#region Properties

	public override TreeNodeCollection Children { get; }

	public bool IsInTemplate { get; }

	#endregion

	#region Methods

	public static VisualTreeNode[] Create(object control)
	{
		return control is AvaloniaObject visual ? [new VisualTreeNode(visual, null)] : [];
	}

	#endregion

	#region Classes

	public class ApplicationHostVisuals : TreeNodeCollection
	{
		#region Fields

		private readonly Controls.Application _application;
		private CompositeDisposable _subscriptions = new(2);

		#endregion

		#region Constructors

		public ApplicationHostVisuals(TreeNode owner, Controls.Application host) :
			base(owner)
		{
			_application = host;
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
			if (_application.ApplicationLifetime is Lifetimes.ISingleViewApplicationLifetime single &&
				single.MainView is not null)
			{
				nodes.Add(new VisualTreeNode(single.MainView, Owner));
			}
			if (_application.ApplicationLifetime is Lifetimes.IClassicDesktopStyleApplicationLifetime classic)
			{
				for (var i = 0; i < classic.Windows.Count; i++)
				{
					var window = classic.Windows[i];
					if (window is MainWindow)
					{
						continue;
					}
					nodes.Add(new VisualTreeNode(window, Owner));
				}
				_subscriptions = new CompositeDisposable(2)
				{
					Window.WindowOpenedEvent.AddClassHandler(typeof(Window), (s, e) =>
					{
						if (s is MainWindow)
						{
							return;
						}
						nodes.Add(new VisualTreeNode((AvaloniaObject) s!, Owner));
					}),
					Window.WindowClosedEvent.AddClassHandler(typeof(Window), (s, e) =>
					{
						if (s is MainWindow)
						{
							return;
						}
						var item = nodes.FirstOrDefault(node => ReferenceEquals(node.Visual, s));
						if (!(item is null))
						{
							nodes.Remove(item);
						}
					})
				};
			}
		}

		#endregion
	}

	public class VisualTreeNodeCollection : TreeNodeCollection
	{
		#region Fields

		private readonly Visual _control;
		private readonly CompositeDisposable _subscriptions = new(2);

		#endregion

		#region Constructors

		public VisualTreeNodeCollection(TreeNode owner, Visual control)
			: base(owner)
		{
			_control = control;
		}

		#endregion

		#region Methods

		public override void Dispose()
		{
			_subscriptions.Dispose();
		}

		protected override void Initialize(AvaloniaList<TreeNode> nodes)
		{
			_subscriptions.Clear();

			if (GetHostedPopupRootObservable(_control) is { } popupRootObservable)
			{
				VisualTreeNode childNode = null;

				_subscriptions.Add(
					popupRootObservable
						.Subscribe(popupRoot =>
						{
							if (popupRoot != null)
							{
								childNode = new VisualTreeNode(
									popupRoot.Value.Root,
									Owner,
									popupRoot.Value.CustomName);

								nodes.Add(childNode);
							}
							else if (childNode != null)
							{
								nodes.Remove(childNode);
							}
						}));
			}

			_subscriptions.Add(
				_control.VisualChildren.ForEachItem(
					(i, item) => nodes.Insert(i, new VisualTreeNode(item, Owner)),
					(i, item) => nodes.RemoveAt(i),
					() => nodes.Clear()));
		}

		private static IObservable<PopupRoot?> GetHostedPopupRootObservable(Visual visual)
		{
			static IObservable<PopupRoot?> GetPopupHostObservable(
				IPopupHostProvider popupHostProvider,
				string providerName = null)
			{
				return Observable.Create<IPopupHost>(observer =>
					{
						void Handler(IPopupHost args)
						{
							observer.OnNext(args);
						}

						popupHostProvider.PopupHostChanged += Handler;
						return Disposable.Create(() => popupHostProvider.PopupHostChanged -= Handler);
					})
					.StartWith(popupHostProvider.PopupHost)
					.Select(popupHost =>
					{
						if (popupHost is Control control)
						{
							return new PopupRoot(
								control,
								providerName != null ? $"{providerName} ({control.GetType().Name})" : null);
						}

						return (PopupRoot?) null;
					});
			}

			return visual switch
			{
				Popup p => GetPopupHostObservable(p),
				Control c => new IObservable<object>[]
					{
						c.GetObservable(Control.ContextFlyoutProperty),
						c.GetObservable(Control.ContextMenuProperty),
						c.GetObservable(FlyoutBase.AttachedFlyoutProperty),
						c.GetObservable(ToolTipDiagnostics.ToolTipProperty),
						c.GetObservable(Button.FlyoutProperty)
					}.CombineLatest()
					.Select(items =>
					{
						var contextFlyout = items[0] as IPopupHostProvider;
						var contextMenu = items[1] as ContextMenu;
						var attachedFlyout = items[2] as IPopupHostProvider;
						var toolTip = items[3] as IPopupHostProvider;
						var buttonFlyout = items[4] as IPopupHostProvider;

						if (contextMenu != null)
							//Note: ContextMenus are special since all the items are added as visual children.
							//So we don't need to go via Popup
						{
							return Observable.Return<PopupRoot?>(new PopupRoot(contextMenu));
						}

						if (contextFlyout != null)
						{
							return GetPopupHostObservable(contextFlyout, "ContextFlyout");
						}

						if (attachedFlyout != null)
						{
							return GetPopupHostObservable(attachedFlyout, "AttachedFlyout");
						}

						if (toolTip != null)
						{
							return GetPopupHostObservable(toolTip, "ToolTip");
						}

						if (buttonFlyout != null)
						{
							return GetPopupHostObservable(buttonFlyout, "Flyout");
						}

						return Observable.Return<PopupRoot?>(null);
					})
					.Switch(),
				_ => null
			};
		}

		#endregion

		#region Structures

		private struct PopupRoot
		{
			#region Constructors

			public PopupRoot(Control root, string customName = null)
			{
				Root = root;
				CustomName = customName;
			}

			#endregion

			#region Properties

			public string CustomName { get; }

			public Control Root { get; }

			#endregion
		}

		#endregion
	}

	#endregion
}