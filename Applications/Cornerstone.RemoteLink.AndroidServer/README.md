# RemoteLink Android server

Device-side process for the RemoteLink **Android** tab. The PC app (`Cornerstone.RemoteLink`) lists devices with `adb` and will later `adb push` this jar, start it with `app_process`, and speak the RemoteLink protocol (magic `RLNK`, version 1).

This is **not** a Visual Studio C# project. It is Gradle + Java. You can still keep the folder in the same repo and edit the `.java` files in VS 2026 as text.

## What the jar is

A small Java program that will run **on the phone** as the `shell` user (same trick scrcpy uses):

```text
adb push remotelink-server.jar /data/local/tmp/
adb reverse tcp:27182 tcp:27182
adb shell CLASSPATH=/data/local/tmp/remotelink-server.jar app_process / cornerstone.remotelink.server.RemoteLinkServer
```

It is not an APK. On the phone it uses MediaCodec + SurfaceControl (or VirtualDisplay) through reflection, so this tree still builds with a desktop JDK. Taps still go through `adb shell input` as well as the control socket.

## Build on Windows (VS 2026 machine)

1. Install a **JDK 17+** (Eclipse Temurin or Microsoft Build of OpenJDK).
2. Install the **Android SDK build-tools** (`d8`). `ANDROID_HOME` or the usual `Android\Sdk` / `android-sdk` folders are searched.
3. Optional: Gradle. The app build uses `build-jar.ps1` (javac + d8), not `gradle jar`.

`app_process` runs ART. The jar must contain `classes.dex`, not desktop `.class` files (those abort with exit 134).

From this folder:

```text
.\build-jar.ps1
```

The RemoteLink app build copies `build\libs\remotelink-server.jar` next to the exe (the app also searches `build\libs` when debugging). Connect then `adb push`es the jar and starts:

```text
app_process /system/bin cornerstone.remotelink.server.RemoteLinkServer
```

Building `Cornerstone.RemoteLink.csproj` compiles a DEX jar and copies it next to the app.

## Protocol

Keep `Protocol.java` in lockstep with `Cornerstone.RemoteLink/Android/Protocol/RemoteLinkProtocol.cs`.
