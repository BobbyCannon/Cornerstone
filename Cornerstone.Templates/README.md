# Cornerstone Templates

`dotnet new` project templates for the [Cornerstone](https://github.com/BobbyCannon/Cornerstone) framework.

## Install

```bash
dotnet new install Cornerstone.Templates
```

From a local package (repo build):

```bash
dotnet pack Cornerstone.Templates/Cornerstone.Templates.csproj -c Release -o ./artifacts
dotnet new install ./artifacts/Cornerstone.Templates.*.nupkg
```

## Templates

| Short name | Description |
|------------|-------------|
| `cornerstone-avalonia` | Avalonia desktop app with AppBootstrap + Keystone (Bus : State : Engine) |

## Create a project

```bash
dotnet new cornerstone-avalonia -n MyApp
cd MyApp
dotnet restore
dotnet run
```

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `-n` / `--name` | (folder name) | Project and root namespace |
| `--CornerstoneVersion` | `3.0.0` | NuGet version of Cornerstone packages |
| `--AvaloniaVersion` | `12.1.1` | Avalonia package version |

Example:

```bash
dotnet new cornerstone-avalonia -n Acme.Shell --CornerstoneVersion 3.0.0 --AvaloniaVersion 12.1.1
```

## What you get

- Host `Main` → `AppBootstrap.Initialize`
- `CornerstoneApplication<AppKeystone>` with DI registration
- Minimal Keystone: `AppState`, `AppBus`, `AppEngine`, `AppKeystone`, `AppViewModel`
- Desktop window shell

See Cornerstone documentation: AppBootstrap, Keystone, Lifecycle, CornerstoneApplication.

## Uninstall

```bash
dotnet new uninstall Cornerstone.Templates
```
