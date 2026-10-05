# Host a documentation reader

How to ship a desktop app that opens a markdown catalog in `DocumentationReader`. The working copy is `Cornerstone.Documentation`: `Program.cs`, `App.cxaml`, and the markdown embed in `Cornerstone.Documentation.csproj`.

Reader chrome, links, and the catalog rules live in [Documentation Reader](../Controls/DocumentationReader.md).

---

## What the host does

`DocumentationReaderHost.Run` is the shared WinExe entry in `Cornerstone.Presentation.Documentation`.

1. `AppBootstrap.Initialize` with your application name and assembly.
2. If the arguments request export, write the static site and return. The window does not open.
3. Otherwise start the `AppBuilder` you pass in with the classic desktop lifetime.

Your `Application` derives from `DocumentationReaderApplication`. After the framework finishes starting, that class loads settings, builds the catalog, applies an optional `.md` argument, and sets `MainWindow` to a `DocumentationReaderHostWindow`. Closing the window writes `ApplicationSettings.json` (placement and reading width).

The window constructs a `DocumentationReader`, assigns it to `Content`, and sets `Reader.Catalog`. Title and icon come from the options.

---

## Minimal program

```csharp
public static AppBuilder BuildCornerstoneApp()
{
	return AppBuilder
		.Configure<App>()
		.UsePlatformDetect()
		.LogToTrace();
}

[STAThread]
public static int Main(string[] args)
{
	return DocumentationReaderHost.Run(args, new DocumentationReaderHostOptions
	{
		ApplicationName = "Cornerstone.Documentation",
		ApplicationAssembly = typeof(Program).Assembly,
		WindowTitle = "Cornerstone Documentation",
		WindowIcon = "/Assets/Cornerstone.ico"
	}, BuildCornerstoneApp());
}
```

`ApplicationName` and `ApplicationAssembly` are required. `ApplicationName` becomes `DocumentationCatalog.Name`, which is also the export folder name and the settings folder when application data is shared.

`App` loads the Cornerstone theme and otherwise stays empty:

```csharp
public class App : DocumentationReaderApplication
{
	public override void Initialize()
	{
		CornerstoneXamlLoader.Load(this);
		base.Initialize();
	}
}
```

`App.cxaml` applies `CornerstoneTheme`. Do not put the reader in that file. The host window owns the reader.

`WindowIcon` is a Cornerstone resource path on `ApplicationAssembly`, resolved as a `csres://` URI. Package the icon with `CornerstoneResource`.

---

## Package the markdown

The default catalog is every `.md` embedded in `ApplicationAssembly`. The resource `LogicalName` is the catalog id, with `/` separators (`Readme.md`, `HowTo/DocumentationReaderHost.md`). The entry document defaults to `Readme.md` (`EntryRelativePath`).

`Cornerstone.Documentation.csproj` stamps that name on each markdown file, then copies those items into `EmbeddedResource` before build. The same files stay visible in the project as `None`.

`appsettings.json` stays a loose file beside the executable (`CopyToOutputDirectory`, `ExcludeFromSingleFile`). It is the export allowlist, not the catalog.

Set `ContentRoot` only when the catalog should come from a directory on disk (tests). Set `BuildCatalog` when the default resource or directory load is the wrong shape. `ResourceNamePrefix` strips a prefix from embedded logical names when the assembly embeds more than the documentation tree.

A CLI argument that ends in `.md` becomes the entry document when that id is in the catalog. `ResolveOpenDocumentId` can map the argument onto a catalog id first.

---

## Export

```text
dotnet run --project Cornerstone.Documentation -- --export <parent-directory>
```

The site is written to `<parent-directory>/<ApplicationName>/`. Exit code `0` is success. Exit code `1` is a failed write.

An empty allowlist exports every packaged page. Any prefix limits the site to matching ids. The host unions three sources: `ExportIncludePaths` on the options, `Documentation:ExportIncludePaths` in `appsettings.json`, and repeatable `--export-include`.

`BareExportDefaultsToSiteFolder` makes a bare `--export` write `./site/<ApplicationName>/`. `Cornerstone.Documentation` leaves that off. The repo-root Documentation app turns it on.

---

## Reader inside another app

Skip `DocumentationReaderHost.Run` when the reader is one tab of a larger shell. Build a `DocumentationCatalog` and assign `Reader.Catalog`. `Cornerstone.Sample` does this in `TabDocumentation`: directory copy when the host has a filesystem, embedded resources for browser and mobile, then a source walk for local development.

The host window is the path when the process exists to show documentation.
