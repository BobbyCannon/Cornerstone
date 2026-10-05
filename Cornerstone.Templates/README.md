# Cornerstone Templates

`dotnet new` project templates for the [Cornerstone](https://github.com/BobbyCannon/Cornerstone) framework.

Live, buildable template sources live in `Cornerstone/Templates`. Edit those projects (ProjectReference to the repo). `Convert-Templates.ps1` copies them here and rewrites references to NuGet packages. Packing this project runs that script first.

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
| `cornerstone.basic` | Desktop app with AppBootstrap and a window. No Keystone |
| `cornerstone.app` | Desktop app with AppBootstrap + Keystone (Bus : State : Engine) |
| `cornerstone.all` | Cross-platform app (Desktop, Android, Browser, iOS) with the same Keystone host |

## Create a project

```bash
dotnet new cornerstone.app -n MyApp
cd MyApp
dotnet restore
dotnet run
```

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `-n` / `--name` | (folder name) | Project and root namespace |
| `--cornerstone-version` | `3.0.0` | NuGet version of Cornerstone packages |

Example:

```bash
dotnet new cornerstone.app -n Acme.Shell --cornerstone-version 3.0.0
```

## What you get

- Host `Main` → `AppBootstrap.Initialize`
- `cornerstone.basic`: `CornerstoneApplication` and a window view model. No Keystone
- `cornerstone.app` and `cornerstone.all`: `CornerstoneApplication<AppKeystone>` with DI registration
- Minimal Keystone: `AppState`, `AppBus`, `AppEngine`, `AppKeystone`, `AppViewModel`
- Desktop window shell

See Cornerstone documentation: AppBootstrap, Keystone, Lifecycle, CornerstoneApplication.

## Uninstall

```bash
dotnet new uninstall Cornerstone.Templates
```
