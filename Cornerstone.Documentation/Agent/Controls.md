# Controls (Agent)

How control types are grouped in `Cornerstone.Presentation` before the public namespace lock. XAML tags do not follow the C# namespace. Every kind is mapped onto `https://github.com/BobbyCannon/Cornerstone` with `XmlnsDefinition`, so `<Button>` stays `<Button>`.

**Not this page:** theme color, mode, and density. See [Themes.md](Themes.md).

## Rule

The control class an app names lives in `Cornerstone.Presentation.Controls`, in the `Controls` folder. `Terminal`, `TextBlock`, `Window`, `Button`, and `Grid` are root types. Kind folders keep everything that is not that control: event args, enums, view models, and helpers. Theme dictionaries live in `Theme/Controls`, one file per control name (`Theme/Controls/Menu.cxaml`).

`TreeDataGrid` and `DockingManager` stay in the folders with those names. Moving either class into the root namespace would hide that folder's namespace.

A new control class goes in the root namespace. Its helpers go in an existing kind folder. A new kind is a new folder, one `XmlnsDefinition` line, and one row in the table below.

`ControlsRootNamespaceTests` fails when a public root type appears or disappears without an edit to `ControlsRootNamespaceSnapshot.txt`. `DevToolsExtensions` in the Diagnostics assembly uses the same root namespace because the InitializeComponent generator requires it. That type is not a file in the Controls folder.

## Kinds to use

| Kind | Namespace | What belongs | Do not add |
|------|-----------|--------------|------------|
| Elements | `Controls.Elements` | The `Controls` collection. The element classes (`Control`, `Panel`, `Border`, and the rest) are root types. | — |
| Layout | `Controls.Layout` | Grid, stack, dock, canvas, wrap, relative, flex, virtualizing panels, LayoutGrid, AutoGrid, ResponsiveGrid, SizeChangedEventArgs, and RequestBringIntoViewEventArgs. Moved. | Input controls |
| Input | `Controls.Input` | Buttons, text boxes, combo, check, radio, slider, toggles, NumberBox, calendar, date and time pickers, ShortcutBox, the input-pane behavior, autocomplete populate events, LostFocusBehavior, DefaultFocus, PixelPointEventArgs, and the radio-button group manager. Moved. `ToggleButton` stays in Primitives. | Item hosts such as ListBox |
| Items | `Controls.Items` | ItemsControl, ListBox, TreeView, TabControl, Menu, TableView, Carousel, PipsPager, container event args, SelectionMode, SelectionChangedEventArgs, ItemCollection, ItemContainerGenerator, and ItemsSourceView. Moved. `SelectingItemsControl` and `TabStrip` stay in Primitives. | Layout panels |
| Navigation | `Controls.Navigation` | Page, PageNavigator, breadcrumb, drawer, SplitView, NavigationMenu, the navigation and modal event args, BarLayoutBehavior, the default page template, and safe-area padding. Moved. | Window chrome |
| Charts | `Controls.Charts` | ChannelControl and LineChart. Moved. Themes are `Theme/Controls/ChannelControl.cxaml` and `LineChart.cxaml`. | — |
| Documents | `Controls.Documents` | Inline text elements, MarkdownView and its presenters, PreviewCodeSnippet, and SelectableTextBlock. Moved. | The text editor. TextBlock is `Controls.Text` |
| Color pickers | `Controls.ColorPickers` | ColorPicker, ColorView, and the palette types. Moved. The namespace is plural so it does not hide the Color or Colors types. | — |
| Web | `Controls.Web` | WebView, its cookies, navigation events, adapter, and BrowserThreads. Moved. The namespace is Web so it does not hide the WebView type. | — |
| Pull to refresh | `Controls.PullToRefresh` | RefreshContainer, RefreshVisualizer, and the pull gesture types. Moved. | — |
| Command bars | `Controls.CommandBars` | CommandBar and its buttons. Moved. The namespace is plural so it does not hide the CommandBar type. | — |
| Progress | `Controls.Progress` | ProgressBar, CircularProgress, ActivityControl, and ActivityItem. Moved. | — |
| Ink | `Controls.Ink` | InkCanvas and InkCanvasStroke. Moved. | — |
| Overlays | `Controls.Overlays` | Flyout, FlyoutPresenter, PopupView, ToolTip, and PlacementMode. Moved. | Popup and FlyoutBase, which stay in Primitives |
| Game input | `Controls.GameInput` | GamepadView and JoystickControl. Moved. The namespace is GameInput so it does not hide the Gamepad type. | — |
| Grouping | `Controls.Grouping` | Expander, ExpandDirection, GroupBox, and Separator. Moved. The namespace is Grouping so it does not hide those types. | HeaderedContentControl, which stays in Primitives |
| Feedback | `Controls.Feedback` | PlatformFeedback, FeedbackAction, FeedbackType, and IPlatformFeedback. Moved. | — |
| Acrylic | `Controls.Acrylic` | ExperimentalAcrylicBorder and AcrylicPlatformCompensationLevels. Moved. | ExperimentalAcrylicMaterial, which stays in Media |
| Property grid | `Controls.PropertyGrid` | PropertyGridControl and the cell factories. PropertyGridControl moved into the existing folder. | — |
| Chrome | `Controls.Chrome` | Window, WindowBase, TopLevel, TopLevelHost, PresentationSource, window decorations, NativeMenu, tray icons, WindowIcon, the window state, startup, transparency, and resize types, and Screens. Moved into the existing folder. | — |
| Scrolling | `Controls.Scrolling` | ScrollViewer, ScrollChangedEventArgs, and IScrollAnchorProvider. Moved. | ScrollBar, which stays in Primitives |
| Icons | `Controls.Icons` | Image, IconElement, and PathIcon. Moved. The namespace is Icons so it does not hide the Image type. | — |
| Validation | `Controls.Validation` | DataValidationErrors. Moved. | — |
| Platform | `Controls.Platform` | Win32, MacOS, and X11 attached properties, and PlatformInhibitionType. Moved into the existing folder. | Native control hosts, which are `Controls.NativeHosts` |
| Labels | `Controls.Labels` | Label and ByteSizeLabel. Moved. The namespace is plural so it does not hide the Label type. | TextBlock, which is `Controls.Text` |
| Native hosts | `Controls.NativeHosts` | NativeControlHost, PausableNativeHost, native surface snapshots, and NativeDock. Moved. | The Window type |
| Theming | `Controls.Theming` | ThemeVariantScope, IThemeVariantProvider, and ThemeBrushes. Moved. | Theme palettes, which stay in Cornerstone.Presentation.Theme.Theming |
| Application lifetimes | `Controls.ApplicationLifetimes` | Desktop lifetime types, DesktopApplicationExtensions, and ShutdownMode. Moved into the existing folder. | — |
| Transitioning | `Controls.Transitioning` | TransitioningContentControl and TransitionCompletedEventArgs. Moved. The namespace is Transitioning so it does not hide the Transitions type. | — |
| Resources | `Controls.Resources` | Resource dictionaries, resource hosts, and resource providers. The namespace is Resources so it does not hide ResourceDictionary. | — |
| Naming | `Controls.Naming` | Name scope, NameScopeLocator, and ResolveByNameAttribute. The namespace is Naming so it does not hide NameScope. | — |
| Style classes | `Controls.StyleClasses` | Classes, pseudo-classes, and their extensions. The namespace is StyleClasses so it does not hide Classes. | — |
| Design time | `Controls.DesignTime` | The Design helper. The namespace is DesignTime so it does not hide Design. | — |

These folders are already a kind. Their control classes are root types. Helpers stay in the folder: Text (view models, margins, and the text-change event args; `TextBlock` and `Terminal` are root types), TreeDataGrid, Primitives, DockingManager, MediaPlayer, Camera, BookmarkBar, Notifications, Templates, Presenters, Automation, Embedding, Remote, Selection, Converters. Shape classes (`Shape`, `Arc`, `Ellipse`, `Line`, `Path`, `Polygon`, `Polyline`, `Rectangle`, `Sector`) live in `Controls.Shapes`. The Path theme is `Theme/Controls/Path.cxaml`.

XAML tags stay on the default Cornerstone xmlns. The XAML compiler looks up `ResolveByNameAttribute` in `Controls.Naming` and `ResourceDictionary` in `Controls.Resources`.

## Adding a control

1. Put the control class in the `Controls` folder, namespace `Cornerstone.Presentation.Controls`. Put helpers in the kind folder.
2. Add `XmlnsDefinition` for a new helper namespace. The default Cornerstone xmlns must resolve the tag. The root namespace is already mapped.
3. Put the theme in `Theme/Controls/<Control>.cxaml` as a `Styles` file and `StyleInclude` it from `Theming/Controls.cxaml`. The expected shape is [Themes.md](Themes.md) (Control theme files). An `x:Class` view stays next to its code-behind until it is migrated.
4. Inside this assembly, an `x:Class` control calls `CornerstoneXamlLoader.Load(this)`. `InitializeComponent()` is generated for app projects, not for Presentation.
5. Add the new root type to `ControlsRootNamespaceSnapshot.txt`.

## Pitfalls

- Do not name a kind after a type (`Controls.ResponsiveGrid`). A test namespace with that last segment hides the type. `ResponsiveGrid` belongs in `Controls.Layout`.
- Inside any `Controls.*` namespace the simple name `Controls` is the namespace. The control collection is `Elements.Controls`.
- An explicit `clr-namespace:Cornerstone.Presentation.Controls` is the root control namespace. A helper that still lives in a kind needs that kind's namespace. The default xmlns resolves both.
- Selectors such as `Button` keep working for the same reason.
- No `global using`. Call sites take a normal `using` for the kind.
- `Theme.Controls` is an empty xmlns. Do not put new types there.
