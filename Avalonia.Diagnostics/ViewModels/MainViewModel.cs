#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Diagnostics.Controls;
using Avalonia.Diagnostics.Views;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.Threading;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class MainViewModel : ViewModel, IDisposable
{
	#region Fields

	private ViewModel _content;
	private IDisposable _currentFocusHighlightAdorner;
	private readonly EventsPageViewModel _events;
	private string _focusedControl;
	private IBrush _focusHighlighter;
	private bool _freezePopups;
	private readonly HotKeyPageViewModel _hotKeys;
	private readonly TreePageViewModel _logicalTree;
	private readonly HashSet<string> _pinnedProperties;
	private IInputElement _pointerOverElement;
	private string _pointerOverElementName;
	private IInputRoot _pointerOverRoot;
	private readonly IDisposable _pointerOverSubscription;
	private readonly AvaloniaObject _root;
	private int _selectedTab;
	private bool _shouldVisualizeMarginPadding;
	private bool _showImplementedInterfaces;
	private bool _showPropertyType;
	private readonly TreePageViewModel _visualTree;

	#endregion

	#region Constructors

	public MainViewModel(AvaloniaObject root)
	{
		_pinnedProperties = [];
		_shouldVisualizeMarginPadding = true;
		_root = root;
		_logicalTree = new TreePageViewModel(this, LogicalTreeNode.Create(root), _pinnedProperties);
		_visualTree = new TreePageViewModel(this, VisualTreeNode.Create(root), _pinnedProperties);
		_events = new EventsPageViewModel(this);
		_hotKeys = new HotKeyPageViewModel();

		UpdateFocusedControl();

		if (KeyboardDevice.Instance is not null)
		{
			KeyboardDevice.Instance.PropertyChanged += KeyboardPropertyChanged;
		}
		SelectedTab = 0;
		if (root is TopLevel topLevel)
		{
			_pointerOverRoot = topLevel;
			_pointerOverSubscription = topLevel.GetObservable(TopLevel.PointerOverElementProperty)
				.Subscribe(x => PointerOverElement = x);
		}
		else
		{
			_pointerOverSubscription = InputManager.Instance!.PreProcess
				.Subscribe(e =>
				{
					if (e is RawPointerEventArgs pointerEventArgs)
					{
						PointerOverRoot = pointerEventArgs.Root;
						PointerOverElement = pointerEventArgs.Root.InputHitTest(pointerEventArgs.Position);
					}
				});
		}
	}

	#endregion

	#region Properties

	public ViewModel Content
	{
		get => _content;
		private set
		{
			if (_content is TreePageViewModel oldTree &&
				value is TreePageViewModel newTree &&
				oldTree?.SelectedNode?.Visual is Control control)
			{
				// HACK: We want to select the currently selected control in the new tree, but
				// to select nested nodes in TreeView, currently the TreeView has to be able to
				// expand the parent nodes. Because at this point the TreeView isn't visible,
				// this will fail unless we schedule the selection to run after layout.
				DispatcherTimer.RunOnce(
					() =>
					{
						try
						{
							newTree.SelectControl(control);
						}
						catch
						{
						}
					},
					TimeSpan.FromMilliseconds(0));
			}

			SetProperty(ref _content, value);
		}
	}

	public string FocusedControl
	{
		get => _focusedControl;
		private set => SetProperty(ref _focusedControl, value);
	}

	public IBrush FocusHighlighter
	{
		get => _focusHighlighter;
		private set => SetProperty(ref _focusHighlighter, value);
	}

	public bool FreezePopups
	{
		get => _freezePopups;
		set => SetProperty(ref _freezePopups, value);
	}

	public IInputElement PointerOverElement
	{
		get => _pointerOverElement;
		private set
		{
			SetProperty(ref _pointerOverElement, value);
			PointerOverElementName = value?.GetType()?.Name;
		}
	}

	public string PointerOverElementName
	{
		get => _pointerOverElementName;
		private set => SetProperty(ref _pointerOverElementName, value);
	}

	public IInputRoot PointerOverRoot
	{
		get => _pointerOverRoot;
		private set => SetProperty(ref _pointerOverRoot, value);
	}

	public int SelectedTab
	{
		get => _selectedTab;
		// [MemberNotNull(nameof(_content))]
		set
		{
			_selectedTab = value;

			switch (value)
			{
				case 1:
					Content = _visualTree;
					break;
				case 2:
					Content = _events;
					break;
				case 3:
					Content = _hotKeys;
					break;
				default:
					Content = _logicalTree;
					break;
			}

			OnPropertyChanged();
		}
	}

	public bool ShouldVisualizeMarginPadding
	{
		get => _shouldVisualizeMarginPadding;
		set => SetProperty(ref _shouldVisualizeMarginPadding, value);
	}

	public bool ShowDetailsPropertyType
	{
		get => _showPropertyType;
		private set => SetProperty(ref _showPropertyType, value);
	}

	public bool ShowDirtyRectsOverlay
	{
		get => GetDebugOverlay(RendererDebugOverlays.DirtyRects);
		set => SetDebugOverlay(RendererDebugOverlays.DirtyRects, value);
	}

	public bool ShowFpsOverlay
	{
		get => GetDebugOverlay(RendererDebugOverlays.Fps);
		set => SetDebugOverlay(RendererDebugOverlays.Fps, value);
	}

	public bool ShowImplementedInterfaces
	{
		get => _showImplementedInterfaces;
		private set => SetProperty(ref _showImplementedInterfaces, value);
	}

	public bool ShowLayoutTimeGraphOverlay
	{
		get => GetDebugOverlay(RendererDebugOverlays.LayoutTimeGraph);
		set => SetDebugOverlay(RendererDebugOverlays.LayoutTimeGraph, value);
	}

	public bool ShowRenderTimeGraphOverlay
	{
		get => GetDebugOverlay(RendererDebugOverlays.RenderTimeGraph);
		set => SetDebugOverlay(RendererDebugOverlays.RenderTimeGraph, value);
	}

	public int? StartupScreenIndex { get; private set; }

	#endregion

	#region Methods

	[DependsOn(nameof(TreePageViewModel.SelectedNode))]
	[DependsOn(nameof(Content))]
	public bool CanShot(object parameter)
	{
		return Content is TreePageViewModel tree
			&& (tree.SelectedNode != null)
			&& tree.SelectedNode.Visual is Visual visual
			&& (visual.VisualRoot != null);
	}

	public void Dispose()
	{
		if (KeyboardDevice.Instance is not null)
		{
			KeyboardDevice.Instance.PropertyChanged -= KeyboardPropertyChanged;
		}
		_pointerOverSubscription.Dispose();
		_logicalTree.Dispose();
		_visualTree.Dispose();
		_currentFocusHighlightAdorner?.Dispose();
		if (TryGetRenderer() is { } renderer)
		{
			renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.None;
		}
	}

	public void EnableSnapshotStyles(bool enable)
	{
		if (Content is TreePageViewModel treeVm && (treeVm.Details != null))
		{
			treeVm.Details.SnapshotFrames = enable;
		}
	}

	public void RequestTreeNavigateTo(Control control, bool isVisualTree)
	{
		var tree = isVisualTree ? _visualTree : _logicalTree;

		var node = tree.FindNode(control);

		if (node != null)
		{
			SelectedTab = isVisualTree ? 1 : 0;

			tree.SelectControl(control);
		}
	}

	public void SelectControl(Control control)
	{
		var tree = Content as TreePageViewModel;

		tree?.SelectControl(control);
	}

	public void SelectFocusHighlighter(object parameter)
	{
		FocusHighlighter = parameter as IBrush;
	}

	public void SetOptions(DevToolsOptions options)
	{
		StartupScreenIndex = options.StartupScreenIndex;
		ShowImplementedInterfaces = options.ShowImplementedInterfaces;
		FocusHighlighter = options.FocusHighlighterBrush;
		SelectedTab = (int) options.LaunchView;

		_hotKeys.SetOptions(options);
	}

	public void ShowHotKeys()
	{
		SelectedTab = 3;
	}

	public void ToggleDirtyRectsOverlay()
	{
		ShowDirtyRectsOverlay = !ShowDirtyRectsOverlay;
	}

	public void ToggleFpsOverlay()
	{
		ShowFpsOverlay = !ShowFpsOverlay;
	}

	public void ToggleLayoutTimeGraphOverlay()
	{
		ShowLayoutTimeGraphOverlay = !ShowLayoutTimeGraphOverlay;
	}

	public void ToggleRenderTimeGraphOverlay()
	{
		ShowRenderTimeGraphOverlay = !ShowRenderTimeGraphOverlay;
	}

	public void ToggleShowDetailsPropertyType(object parameter)
	{
		ShowDetailsPropertyType = !ShowDetailsPropertyType;
	}

	public void ToggleShowImplementedInterfaces(object parameter)
	{
		ShowImplementedInterfaces = !ShowImplementedInterfaces;
		if (Content is TreePageViewModel viewModel)
		{
			viewModel.UpdatePropertiesView();
		}
	}

	public void ToggleVisualizeMarginPadding()
	{
		ShouldVisualizeMarginPadding = !ShouldVisualizeMarginPadding;
	}

	private bool GetDebugOverlay(RendererDebugOverlays overlay)
	{
		return ((TryGetRenderer()?.Diagnostics.DebugOverlays ?? RendererDebugOverlays.None) & overlay) != 0;
	}

	private void KeyboardPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
		{
			UpdateFocusedControl();
		}
	}

	private void SetDebugOverlay(RendererDebugOverlays overlay, bool enable,
		[CallerMemberName] string propertyName = null)
	{
		if (TryGetRenderer() is not { } renderer)
		{
			return;
		}

		var oldValue = renderer.Diagnostics.DebugOverlays;
		var newValue = enable ? oldValue | overlay : oldValue & ~overlay;

		if (oldValue == newValue)
		{
			return;
		}

		renderer.Diagnostics.DebugOverlays = newValue;
		OnPropertyChanged(propertyName);
	}

	private IRenderer TryGetRenderer()
	{
		return _root switch
		{
			TopLevel topLevel => topLevel.Renderer,
			Controls.Application app => app.RendererRoot,
			_ => null
		};
	}

	private void UpdateFocusedControl()
	{
		var element = KeyboardDevice.Instance?.FocusedElement;
		FocusedControl = element?.GetType().Name;
		_currentFocusHighlightAdorner?.Dispose();
		if (FocusHighlighter is IBrush brush
			&& element is InputElement input
			&& !input.DoesBelongToDevTool()
			)
		{
			_currentFocusHighlightAdorner = ControlHighlightAdorner.Add(input, brush);
		}
	}

	#endregion
}