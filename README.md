<img align="left" src="https://github.com/BobbyCannon/Cornerstone/blob/master/Cornerstone.png?raw=true"
    height="64" width="64" style="margin-bottom: 8px; margin-right: 8px;" />

**Cornerstone** is a shared .NET 10 framework for desktop and cross-platform apps: process bootstrap, Keystone (Bus, State, Engine), lifecycle, sync and storage, and an optional UI runtime. Markup is `.cxaml`, compiled at build time.

![GitHub](https://img.shields.io/github/license/BobbyCannon/Cornerstone?style=flat-square&color=purple)
![.NET](https://img.shields.io/badge/.NET-10+-blueviolet?style=flat-square&color=purple)

---

## Shape of an app

Process entry builds one stack. Domain work stays in Keystone. Presentation is optional.

```
Process entry (console / desktop / mobile / browser / service)
  └─ AppBootstrap.Initialize(…)     process DI, runtime info, platform
       └─ App services
            ├─ Keystone — Bus : State : Engine
            ├─ Lifecycle — Initialize → Load → Start → Process → Stop → …
            ├─ Sync and storage
            └─ Presentation (optional)
                 ├─ ViewModels + ViewIntegration
                 └─ AppDispatcher (attached ViewModels)
```

State is the model. The engine's processors mutate that state. The bus carries the messages. Processors run off the UI thread. When a view is attached, ViewModels and AppDispatcher project that state onto the screen.

| Piece | Role |
|-------|------|
| **AppBootstrap** | Process-wide dependency injection and infrastructure (once per process) |
| **Keystone** | Domain Bus, State, and Engine, and its lifecycle tree |
| **LifecycleTracker** | Hierarchical Initialize → Load → Start, and reverse teardown |
| **Sync and storage** | Entity sync between a client and a server, and a source-generated SQL table mapper. Entity Framework is a separate package. |
| **CornerstoneApplication** | Shell that wires bootstrap, the dispatcher, and Keystone start and stop |
| **AppDispatcher** | Optional UI projection loop (attached ViewModels only) |

---

## Native controls without compromise

Most UI stacks treat native controls as a foreign window glued on top of everything else. Buttons, menus, and popups cannot sit over a live WebView, map, or video. Workarounds are snapshots, pause-and-cover, or "don't overlap." You are expected to pick: native content, or a real UI.

Cornerstone does not make that trade. Native and Cornerstone controls live as **peers** — equal in every way. A WebView, a map, a video player, and a toolbar are just controls in the same tree. You can stack them, overlap them, and mix first-party and **third-party** native SDKs without giving one side of the UI away.

Details: [Native layering](Cornerstone.Documentation/Presentation/NativeLayering.md).

---

## See it

Sample apps in `Applications/` run on Desktop, Android, iOS, and in the browser. Projects in `Integrations/` host a third-party SDK. An app takes an integration with a project reference and builds it from this tree. Each SDK keeps its own license.

| Project | What it shows |
|---------|----------------|
| **[Cornerstone.Navigator](Applications/Cornerstone.Navigator/)** | A browser around WebView: address bar, back and forward, and favorites. |
| **[Cornerstone.MediaPlayer](Applications/Cornerstone.MediaPlayer/)** | `MediaPlayerControl` and playback chrome on the same four hosts. |
| **[Cornerstone.Esri](Integrations/Cornerstone.Esri/)** | ArcGIS Runtime `MapView` and `SceneView`. GIS types stay Esri's. |
| **[Cornerstone.Vlc](Integrations/Cornerstone.Vlc/)** | LibVLCSharp `VideoView`. Playback types stay LibVLCSharp's. |
| **[Cornerstone.Mapsui](Integrations/Cornerstone.Mapsui/)** | Mapsui map control. |

---

## Start

```
dotnet new install Cornerstone.Templates
dotnet new cornerstone.app -n MyApp
```

| Template | What you get |
|----------|----------------|
| `cornerstone.basic` | Desktop window with AppBootstrap |
| `cornerstone.app` | Desktop app with AppBootstrap and Keystone |
| `cornerstone.all` | The same Keystone host on Desktop, Android, Browser, and iOS |

A UI app references `Cornerstone` and `Cornerstone.Presentation`. The analyzer ships inside `Cornerstone`, so both references are required to compile `.cxaml`. Default theme and product controls are already in Presentation. Entity Framework, PowerShell, and automation ship as their own packages.

Templates: [Cornerstone.Templates/README.md](Cornerstone.Templates/README.md). Documentation: [Cornerstone.Documentation/Readme.md](Cornerstone.Documentation/Readme.md).

---

The aim is a framework for developers by developers: fast, open, free, and reliable, and software that makes building applications enjoyable. A release may miss a goal. If you run into an issue, we would love to hear about it.

---

## License

Cornerstone is MIT. See [license.txt](license.txt).

`Cornerstone.Presentation` includes source from the other projects. The upstream license and the other third-party notices are in [Cornerstone.Presentation/NOTICE.md](Cornerstone.Presentation/NOTICE.md).
