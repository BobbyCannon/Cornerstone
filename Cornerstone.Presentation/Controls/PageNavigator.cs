#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Navigation;

#endregion

namespace Cornerstone.Presentation.Controls;

public partial class PageNavigator : ContentControl
{
	#region Fields

	private TopLevel _backTopLevel;
	private bool _navigating;
	private readonly List<PageNavigatorPage> _stack;
	private BreadcrumbTrail _trail;

	#endregion

	#region Constructors

	public PageNavigator()
	{
		_stack = [];
		Crumbs = new ObservableCollection<BreadcrumbSegment>();
		GoBackCommand = new RelayCommand(_ => OnBackRequested());
		RootTitle = "Home";
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial bool CanGoBack { get; set; }

	public ObservableCollection<BreadcrumbSegment> Crumbs { get; }

	public ICommand GoBackCommand { get; }

	[StyledProperty]
	public partial string RootTitle { get; set; }

	#endregion

	#region Methods

	public static PageNavigator GetPageNavigator(Visual visual)
	{
		while (visual is not null)
		{
			if (visual is PageNavigator navigator)
			{
				return navigator;
			}

			visual = visual.GetVisualParent();
		}

		return null;
	}

	public void GoBack()
	{
		if (_stack.Count <= 1)
		{
			return;
		}

		_stack.RemoveAt(_stack.Count - 1);
		Show(_stack[_stack.Count - 1]);
	}

	public void GoHome()
	{
		if (_stack.Count <= 1)
		{
			EnsureRoot();
			SyncTrail();
			return;
		}

		while (_stack.Count > 1)
		{
			_stack.RemoveAt(_stack.Count - 1);
		}

		Show(_stack[0]);
	}

	public void Navigate(string title, Control page)
	{
		if (page is null)
		{
			return;
		}

		EnsureRoot();
		_stack.Add(new PageNavigatorPage(title, page));
		Show(_stack[_stack.Count - 1]);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		if (_trail is not null)
		{
			_trail.CrumbClicked -= OnTrailCrumbClicked;
		}

		_trail = e.NameScope.Find<BreadcrumbTrail>("PART_Trail");
		if (_trail is not null)
		{
			_trail.CrumbClicked += OnTrailCrumbClicked;
		}

		EnsureRoot();
		EnsureDesignPreview();
		SyncTrail();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		SystemBack.Subscribe(this, ref _backTopLevel, TopLevelOnBackRequested);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		SystemBack.Unsubscribe(ref _backTopLevel, TopLevelOnBackRequested);
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == ContentProperty) && !_navigating)
		{
			EnsureRoot();
			SyncTrail();
		}

		if (change.Property == RootTitleProperty)
		{
			if (_stack.Count > 0)
			{
				_stack[0].Title = RootTitle ?? "Home";
			}

			SyncTrail();
		}
	}

	private void EnsureDesignPreview()
	{
		if (!Design.IsDesignMode || (_stack.Count != 1))
		{
			return;
		}

		_stack.Add(new PageNavigatorPage("Buttons", _stack[0].Content));
	}

	private void EnsureRoot()
	{
		if ((_stack.Count > 0) || Content is not Control content)
		{
			return;
		}

		_stack.Add(new PageNavigatorPage(RootTitle ?? "Home", content));
	}

	private void TopLevelOnBackRequested(object sender, RoutedEventArgs e)
	{
		if (!SystemBack.TryClaim(this, e, CanGoBack))
		{
			return;
		}

		OnBackRequested();
		e.Handled = true;
	}

	private void OnBackRequested()
	{
		if (!CanGoBack)
		{
			return;
		}

		BackRequested?.Invoke(this, EventArgs.Empty);
		GoBack();
	}

	private void OnTrailCrumbClicked(object sender, BreadcrumbClickedEventArgs e)
	{
		if ((e.Index < 0) || (e.Index >= (_stack.Count - 1)))
		{
			return;
		}

		while (_stack.Count > (e.Index + 1))
		{
			_stack.RemoveAt(_stack.Count - 1);
		}

		Show(_stack[_stack.Count - 1]);
	}

	private void Show(PageNavigatorPage page)
	{
		_navigating = true;
		try
		{
			Content = page.Content;
		}
		finally
		{
			_navigating = false;
		}

		SyncTrail();
	}

	private void SyncTrail()
	{
		CanGoBack = _stack.Count > 1;
		Crumbs.Clear();
		for (var i = 0; i < _stack.Count; i++)
		{
			var page = _stack[i];
			var isCurrent = i == (_stack.Count - 1);
			Crumbs.Add(new BreadcrumbSegment(page.Title, page.Title, isCurrent, i > 0));
		}

		_trail?.SetItems(Crumbs);
	}

	#endregion

	#region Events

	public event EventHandler BackRequested;

	#endregion
}