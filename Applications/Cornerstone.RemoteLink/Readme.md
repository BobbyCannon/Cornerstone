# Cornerstone.RemoteLink

Windows desktop **remote screen session**. Three tabs:

- **AirPlay** — advertises this PC over mDNS and shows an iPhone/iPad screen (TCP 5000 AirTunes, 7000 AirPlay).
- **Android** — lists devices with `adb`. Building this project compiles `remotelink-server.jar` (needs JDK 17+) and copies it next to the exe. Connect pushes that jar and the phone encodes H.264. See `Cornerstone.RemoteLink.AndroidServer/README.md`.
- **VNC** — RFB client. Connect to host:port with optional VNC password.

## Run

```text
dotnet run --project Cornerstone/Applications/Cornerstone.RemoteLink
```

Windows only (`net10.0-windows…`).

AirPlay: firewall must allow TCP 5000 and 7000. On the device: Control Center → Screen Mirroring → pick this PC (same Wi-Fi).

VNC: enter host and port (default 5900) on the VNC tab and Connect.

Android: USB debugging on, one `adb` server. Connect without a jar is input-only. With `remotelink-server.jar`, Connect pushes it, reverses port 27182, and the phone encodes H.264 (MediaCodec). See `Cornerstone.RemoteLink.AndroidServer/README.md`.

## Acknowledgements

RemoteLink exists because other people already solved the hard protocols and then published the work. The AirPlay and VNC tabs are in-tree copies of those projects, adapted enough to sit in this shell. We are grateful they made that possible.

**These forks are for personal use only.** They are not a product, not a drop-in replacement, and not something we keep in sync with upstream. If you want AirPlay or VNC in your own app, **use the original repositories**. File issues and pull requests there. Star or sponsor them if their work helped you. Do not treat RemoteLink as the source of truth. I will maintain this project but only for what I want or if I happen to get a pull request.

Neither project is affiliated with Cornerstone.

### AirPlay — Airplay2OnWindows

**Use this:** [YimingZhanshen/Airplay2OnWindows](https://github.com/YimingZhanshen/Airplay2OnWindows)  
**License:** MIT  
**Copy in this repo:** `AirPlay/` (personal fork, not maintained as a product)

Open-source AirPlay 2 receiver for Windows: screen mirroring (H.264), audio, mDNS/Bonjour so an iPhone or iPad can pick this PC in Control Center. RemoteLink’s AirPlay tab is based on that receiver (RTSP, AirTunes, mirroring stream, pairing/crypto, plist, and related pieces).

Airplay2OnWindows itself builds on [SteeBono/airplayreceiver](https://github.com/SteeBono/airplayreceiver) and other public AirPlay work; their credits still apply.

### VNC — MarcusW.VncClient

**Use this:** [MarcusWichelmann/MarcusW.VncClient](https://github.com/MarcusWichelmann/MarcusW.VncClient)  
**License:** MIT (copyright Marcus Wichelmann; copy under `Vnc/Client/LICENSE`)  
**Copy in this repo:** `Vnc/Client/` (protocol) and `Vnc/Controls/` (Cornerstone view, input, framebuffer) — personal fork, not maintained as a product

A high-performance, fully managed C# RFB client: Tight/ZRLE and related encodings, pointer and keyboard, authentication, reconnect. RemoteLink’s VNC tab is a copy of that library plus its Cornerstone adapter, wired as a shell tab.

If you can type, click, and see a remote desktop on the VNC tab, that is Marcus Wichelmann’s protocol stack.
