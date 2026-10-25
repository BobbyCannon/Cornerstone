# Cornerstone Sample

`Cornerstone.Sample` is the desktop and cross-platform demo. The Media Player tab and the VLC tab both play the embedded Big Buck Bunny clip, a local file, or an HTTPS URL.

On Windows, Android, and iOS the native player libraries ship with the app. On Linux they do not. Playback uses the system LibVLC library (`libvlc.so.5`) and the VLC plugins next to it. Wayland copies frames into the Cornerstone surface and letterboxes them so the picture is not stretched. X11 draws into the child window. There is no `VideoLAN.LibVLC.Linux` package for the desktop build.

If LibVLC is missing, or VLC starts without its plugins, the Media Player status line reports the failure. The VLC tab does the same when you press Play. Install `vlc` and `vlc-plugin-ffmpeg`, then run the sample again. The base `vlc` package plays audio. H.264 video, including the embedded sample, needs `vlc-plugin-ffmpeg`, which supplies `libavcodec_plugin.so` and FFmpeg 4.4.

## SteamOS

SteamOS is Arch-based and the root filesystem is read-only. `apt` and `dnf` do not apply. In Desktop Mode:

```bash
sudo steamos-readonly disable
sudo pacman-key --init
sudo pacman-key --populate archlinux holo
sudo pacman -S vlc vlc-plugin-ffmpeg
sudo steamos-readonly enable
```

This does not survive a SteamOS update. After an update, run it again. Plugins are installed under `/usr/lib/vlc/plugins`.

The sample WebView on Linux uses system WebKitGTK the same way. On SteamOS, install `webkit2gtk-4.1` with the same read-only steps, in place of `vlc`.

## Other Linux distributions

Debian and Ubuntu:

```bash
sudo apt install libvlc5 vlc
```

Fedora:

```bash
sudo dnf install vlc
```

Arch:

```bash
sudo pacman -S vlc vlc-plugin-ffmpeg
```

WebKitGTK package names differ. Debian and Ubuntu use `libwebkit2gtk-4.1-0`. Fedora uses `webkit2gtk4.1`. Arch uses `webkit2gtk-4.1`. A Wayland-only session also needs XWayland so the hidden GTK host can start.
