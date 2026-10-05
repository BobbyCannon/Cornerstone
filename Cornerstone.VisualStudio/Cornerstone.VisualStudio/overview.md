# Cornerstone

Live designer and IntelliSense for Cornerstone and Avalonia markup in Visual Studio.

## Designer

- `.cxaml` opens in the Cornerstone designer.
- `.axaml` opens in the Avalonia designer.
- The source and a live preview share the document tab.
- `.xaml` files (WPF, MAUI, and other XAML stacks) stay with the Visual Studio XAML editor.

## Editor

- Completion for elements, attributes, property values, bindings, and style classes.
- Go To Definition on types, binding paths, enum values, brushes, and style classes.
- The completion engine runs in a separate .NET process so Visual Studio does not load designer metadata on its UI thread.

## Also included

- Cornerstone and Avalonia project templates.
- Avalonia C# snippets.
- View → Other Windows → Cornerstone lists the editor host and the preview process for each open designer.

## Requirements

- Visual Studio 2022 17.14 or Visual Studio 2026. 64-bit.
- The .NET desktop development workload.
- The .NET 10 runtime available as `dotnet` on PATH. The editor host and the .NET previewer start with `dotnet exec`.
