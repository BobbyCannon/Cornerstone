# Build

## Configuration order

```
Directory.Build.props
        │
        ▼
     SDK Props
        │
        ▼
 Project (.csproj)
        │
        ▼
Directory.Build.targets
        │
        ▼
Build.Release.props
        │
        ├── Release.Browser.props
        ├── Release.Android.props
        ├── Release.iOS.props
        ├── Release.Aot.props
        ├── Release.R2R.props
        │
        ▼
CLI  /p:… (dotnet build/publish)