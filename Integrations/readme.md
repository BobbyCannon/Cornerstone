# Integrations

Optional examples of hosting a third-party SDK inside Cornerstone. These projects are not part of `Cornerstone` or `Cornerstone.Presentation`, and they are not NuGet packages. An app takes one with a project reference and builds it from this tree.

Each SDK keeps its own license. Shipping the integration in a product means complying with that license.

| Project | SDK |
|---------|-----|
| [Cornerstone.Esri](Cornerstone.Esri/) | ArcGIS Runtime `MapView` / `SceneView` |
| [Cornerstone.Mapsui](Cornerstone.Mapsui/) | Mapsui map control |
| [Cornerstone.Vlc](Cornerstone.Vlc/) | LibVLCSharp `VideoView`. LGPL-2.1-or-later. Call `UseCornerstoneVlc` from the host. |
