# TODO: Headless coverage for every Presentation control

**Status:** open  
**When:** next untested control with its own behavior is `TickBar`. `DropDownButton` is still open and adds no members beyond `Button`. `ToggleSplitButton` is the remaining untested button.

New test classes, each marked done below:

- `CircularProgressTests` covers the arc percentage, sweep angle, measure, and `:indeterminate`.
- `ProgressBarTests` covers the indicator size, orientation classes, and indeterminate template positions. An empty range fills the track.
- `RepeatButtonTests` covers `Delay`, `Interval`, and stopping the repeat on release or disable.
- `HyperlinkButtonTests` covers `NavigateUri` through a stub `ILauncher` and the `:visited` class.
- `ToggleSwitchTests` covers on and off content, knob position, and drag past half the knob width.
- `PressHoldButtonTests` covers the hold timer, `HoldCommand`, and cancelling the hold before the duration.
- `ThumbTests` covers `DragStarted`, `DragDelta`, `DragCompleted`, and `AdjustDrag`.

`ButtonTests` gained `ClickMode`, focused Space and Enter, a handled click, the right button, and the button flyout. It stays partial. `HotKey` stays in `HotKeyManagerTests`.

Extend the existing classes under `Tests/Cornerstone.Presentation.UnitTests/Controls`. Do not add a second test class for the same control.

Every public control under `Cornerstone/Cornerstone.Presentation/Controls` gets its missing properties and features covered in that folder. Inherited properties (`Width`, `IsEnabled`, `DataContext`, and the rest of `Control`) are covered once on `Control`.

## What counts

A control is a public type in that folder whose base is `Control`, including panels, pages, shapes, `TreeDataGrid`, and `DockingManager`. Open generics are listed once (`TextEditor`, not `TextEditor<T>`). Abstract types are tested through a small test subclass in the same test class.

Those tests are already the headless control suite. They use `[PresentationTestMethod]` and `ScopedTestBase`, and `UnitTestApplication` supplies the headless renderer and mock window platform. Add missing cases to the existing `{Control}Tests` class. Create that class in `Controls/` only when the control has none.

`[HeadlessTestMethod]` and `Tests/Cornerstone.Presentation.UnitTests/Headless/` belong to the platform session (setup, rendering, threading, popup). Do not copy control coverage there.

A row stays **open** until the existing class:

1. Assigns every public instance property declared on that type (styled, direct, and CLR) to a non-default value and reads it back.
2. Calls every public instance method that can run without a real OS window, clipboard, camera, media device, or WebView2, and asserts the observable result.
3. Drives the features those members expose: selection, expand and collapse, spin, click command, scroll, dock, virtualization attach.

**partial** means a test class already exists and still misses properties or features. `Popup` is partial: `Controls/Primitives/PopupTests.cs` plus platform cases in `Headless/PopupTests.cs`.

## Out of scope

- A real HWND, clipboard, camera, microphone, WebView2, or media decode. Assert in-process state and the null or stub path. Device and window hosts below say this on the row.
- Theme gallery tests in `Cornerstone.UnitTests` (`RunOnUi` and `CornerstoneTheme`).
- A parallel `Controls/Headless/` suite that retests the same control.
- Automation peers, converters, event args, enums, interfaces, theme files, and private nested helpers.

## Foundations

| Control | Existing tests | Status |
|---------|-----------------|--------|
| Control | LoadedTests | partial |
| TemplatedControl | Primitives/TemplatedControlTests | open |
| Decorator | DecoratorTests | open |
| Border | BorderTests | open |
| Panel | PanelTests | open |
| ContentControl | ContentControlTests | open |
| ContentPresenter | Presenters/ContentPresenterTests* | open |
| UserControl | UserControlTests | open |
| ItemsControl | ItemsControlTests | open |
| HeaderedContentControl | | open |
| HeaderedItemsControl | HeaderedItemsControlTests | open |
| SelectingItemsControl | Primitives/SelectingItemsControlTests* | open |
| HeaderedSelectingItemsControl | | open |
| RangeBase | Primitives/RangeBaseTests | open |
| Shape | Shapes/ShapeTests | open |

## Layout

| Control | Existing tests | Status |
|---------|-----------------|--------|
| Canvas | CanvasTests | open |
| StackPanel | StackPanelTests | open |
| DockPanel | DockPanelTests | open |
| Grid | GridTests | open |
| WrapPanel | WrapPanelTests | open |
| UniformGrid | Primitives/UniformGridTests | open |
| FlexPanel | FlexPanelTests | open |
| RelativePanel | RelativePanelTests | open |
| GridPanel | | open |
| LayoutGrid | | open |
| AutoGrid | | open |
| ResponsiveGrid | | open |
| ReversibleStackPanel | ReversibleStackPanelTests | open |
| SplitPanel | | open |
| SplitPanelLine | | open |
| DockSplitPanel | | open |
| AdaptiveFillLayout | | open |
| Viewbox | ViewboxTests | open |
| LayoutTransformControl | LayoutTransformControlTests | open |
| GridSplitter | GridSplitterTests | open |
| VirtualizingPanel | | open |
| VirtualizingStackPanel | VirtualizingStackPanelTests | open |
| VirtualizingCarouselPanel | VirtualizingCarouselPanelTests | open |
| GridVirtualizingPanel | | open |
| DateTimePickerPanel | | open |
| AdornerLayer | | open |
| OverlayLayer | | open |
| VisualLayerManager | Primitives/VisualLayerManagerTests | open |
| InputPaneAwareDecorator | | open |
| ExperimentalAcrylicBorder | | open |
| ThemeVariantScope | | open |
| Margin | | open |

## Buttons and input

| Control | Existing tests | Status |
|---------|-----------------|--------|
| Button | ButtonTests | partial |
| RepeatButton | RepeatButtonTests | done |
| ToggleButton | Primitives/ToggleButtonTests | open |
| CheckBox | CheckBoxTests | open |
| RadioButton | RadioButtonTests | open |
| HyperlinkButton | HyperlinkButtonTests | done |
| DropDownButton | | open |
| SplitButton | SplitButtonTests | open |
| ToggleSplitButton | | open |
| PressHoldButton | PressHoldButtonTests | done |
| ToggleSwitch | ToggleSwitchTests | done |
| Thumb | ThumbTests | done |
| Track | Primitives/TrackTests | open |
| Slider | SliderTests | open |
| TickBar | | open |
| ScrollBar | Primitives/ScrollBarTests | open |
| ProgressBar | ProgressBarTests | done |
| CircularProgress | CircularProgressTests | done |
| Spinner | | open |
| ButtonSpinner | | open |
| NumericUpDown | NumericUpDownTests | open |
| NumberBox | | open |
| TextBlock | TextBlockTests | open |
| AccessText | | open |
| SelectableTextBlock | SelectableTextBlockTests | open |
| TextBox | TextBoxTests, TextBoxTestsDataValidation, TextBoxTestsInput | open |
| MaskedTextBox | MaskedTextBoxTests | open |
| ShortcutBox | | open |
| Label | LabelTests | open |
| ByteSizeLabel | | open |
| TextPresenter | Presenters/TextPresenterTests | open |
| CaretVisual | | open |
| TextSelectionHandle | | open |
| TextSelectorLayer | | open |
| AutoCompleteBox | AutoCompleteBoxTests | open |
| Calendar | CalendarTests | open |
| CalendarButton | | open |
| CalendarDayButton | | open |
| CalendarItem | | open |
| CalendarDatePicker | CalendarDatePickerTests | open |
| DatePicker | DatePickerTests | open |
| DatePickerPresenter | | open |
| TimePicker | TimePickerTests | open |
| TimePickerPresenter | | open |
| PickerPresenterBase | | open |
| ColorView | | open |
| ColorPicker | | open |
| ColorPreviewer | | open |
| ColorSlider | | open |
| ColorSpectrum | | open |
| InkCanvas | | open |
| JoystickControl | | open |
| GamepadView | | open |

## Items and chrome

| Control | Existing tests | Status |
|---------|-----------------|--------|
| ListBox | ListBoxTests, ListBoxTestsSingle, ListBoxTestsMultiple | open |
| ListBoxItem | | open |
| ComboBox | ComboBoxTests | open |
| ComboBoxItem | | open |
| TreeView | TreeViewTests, TreeViewBringIntoViewTests | open |
| TreeViewItem | | open |
| MenuBase | | open |
| Menu | | open |
| MenuItem | MenuItemTests | open |
| ContextMenu | ContextMenuTests | open |
| MenuFlyoutPresenter | | open |
| NativeMenuBar | | open |
| TabControl | TabControlTests | open |
| TabItem | | open |
| TabStrip | Primitives/TabStripTests | open |
| TabStripItem | | open |
| Carousel | CarouselTests | open |
| PipsPager | PipsPagerTests | open |
| ItemsPresenter | Presenters/ItemsPresenterTests | open |
| ScrollViewer | ScrollViewerTests, ScrollViewerTestsILogicalScrollable | open |
| ScrollContentPresenter | Presenters/ScrollContentPresenterTests* | open |
| RefreshContainer | | open |
| RefreshVisualizer | PullToRefresh/RefreshVisualizerTests | open |
| Page | | open |
| ContentPage | ContentPageTests | open |
| NavigationPage | NavigationPageTests | open |
| PageNavigationHost | PageNavigationHostTests | open |
| PageNavigator | | open |
| CarouselPage | CarouselPageTests | open |
| DrawerPage | DrawerPageTests | open |
| TabbedPage | TabbedPageTests | open |
| MultiPage | | open |
| SelectingMultiPage | | open |
| NavigationMenu | | open |
| SplitView | SplitViewTests | open |
| Expander | | open |
| GroupBox | | open |
| Separator | | open |
| Popup | Primitives/PopupTests, Headless/PopupTests | partial |
| PopupRoot | Primitives/PopupRootTests | open |
| PopupView | | open |
| OverlayPopupHost | | open |
| FlyoutPresenter | FlyoutTests | open |
| ToolTip | ToolTipTests | open |
| DataValidationErrors | | open |
| CommandBar | CommandBarTests | open |
| CommandBarButton | CommandBarTests | open |
| CommandBarToggleButton | CommandBarTests | open |
| CommandBarSeparator | CommandBarTests | open |
| ActivityControl | | open |
| BreadcrumbTrail | Controls/BreadcrumbTrailTests (leading ellipsis width split) | partial |
| NotificationCard | NotificationsTests | open |
| WindowNotificationManager | NotificationsTests | open |
| IconElement | | open |
| PathIcon | | open |
| Image | ImageTests | open |
| TransitioningContentControl | TransitioningContentControlTests | open |
| PreviewCodeSnippet | | open |
| BrowserThreads | | open |

## Product controls

| Control | Existing tests | Status |
|---------|-----------------|--------|
| MarkdownView | | open |
| MarkdownBlockPresenter | | open |
| MarkdownTablePresenter | | open |
| ChannelControl | | open |
| LineChart | | open |
| PropertyGridControl | | open |
| BookmarkBarControl | | open |
| BookmarkBarItem | | open |
| TextEditor | | open |
| Terminal | | open |
| TextRenderer | | open |
| FoldingMargin | | open |
| LineNumberMargin | | open |
| TableView | TableViewTests | open |
| TableViewCell | | open |
| TableViewCellsPresenter | | open |
| TableViewRow | | open |
| TableViewColumnHeader | TableViewColumnHeaderTests | open |
| TableViewColumnHeadersPresenter | | open |
| Arc | | open |
| Ellipse | Shapes/EllipseTests | open |
| Line | | open |
| Path | Shapes/PathTests | open |
| Polygon | Shapes/PolygonTests | open |
| Polyline | Shapes/PolylineTests | open |
| Rectangle | Shapes/RectangleTests | open |
| Sector | | open |
| TreeDataGrid | | open |
| TreeDataGridCell | | open |
| TreeDataGridCellsPresenter | | open |
| TreeDataGridCheckBoxCell | | open |
| TreeDataGridColumnHeader | | open |
| TreeDataGridColumnHeadersPresenter | | open |
| TreeDataGridColumnarPresenterBase | | open |
| TreeDataGridExpanderCell | | open |
| TreeDataGridHyperlinkButtonCell | | open |
| TreeDataGridPresenterBase | | open |
| TreeDataGridPressHoldButtonCell | | open |
| TreeDataGridRow | | open |
| TreeDataGridRowsPresenter | | open |
| TreeDataGridTemplateCell | | open |
| TreeDataGridTextCell | | open |
| DockableTabView | | open |
| DockingTabControl | | open |
| DockingManager | | open |

## Device and window hosts

Headless coverage here is the in-process surface and the null or stub path. Do not require a real window, camera, player, or browser.

| Control | Existing tests | Status |
|---------|-----------------|--------|
| TopLevel | TopLevelTests | open |
| WindowBase | WindowBaseTests | open |
| Window | WindowTests | open |
| EmbeddableControlRoot | | open |
| NativeControlHost | | open |
| NativeSurfaceHost | | open |
| WebView | | open |
| CameraView | | open |
| CameraNativeHost | | open |
| MediaPlayerControl | | open |
| MediaPlayerNativeHost | | open |

## Related

- `Cornerstone/Cornerstone.Presentation/Controls`
- `Cornerstone/Tests/Cornerstone.Presentation.UnitTests/Headless`
- `Cornerstone/Tests/Cornerstone.Presentation.UnitTests/Controls/ControlsRootNamespaceSnapshot.txt`
- [QuickStart/HeadlessTesting.md](../QuickStart/HeadlessTesting.md)
