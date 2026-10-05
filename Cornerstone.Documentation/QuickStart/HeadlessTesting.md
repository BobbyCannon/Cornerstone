# Getting started with headless testing

Headless presentation runs the UI runtime without opening a window. A dedicated thread owns the dispatcher, the platform registers clipboard, keyboard, render loop, and settings, and each test method is queued onto that thread. Constructing a control on the MSTest worker thread does not reach that loop, and calling `Dispatcher.UIThread.Invoke` from the worker deadlocks because nothing is pumping there.

Two suites use this. Pick the one that matches the code you are testing.

## Presentation runtime tests

`Cornerstone.Presentation.UnitTests` is the regression suite for the UI runtime. The assembly does not run tests in parallel, because they share one session.

Start with `Tests/Cornerstone.Presentation.UnitTests/Headless/SetupTests.cs`. `SetupTearDownShouldWork` is marked `[HeadlessTestMethod]`. That attribute tells the generated test runner to start `HeadlessUnitTestSession` for the assembly and run the method on the session thread. The method itself only checks that setup and teardown ran once; it is the smallest proof that the session is alive.

`RenderingTests` in the same folder is the next read. Those methods exercise the headless platform (measure, render, frame capture) instead of only proving the session started.

The app that boots that platform is `Helpers/HeadlessUnitTestApplication.cs`. `Start` enters a locator scope, resets the dispatcher, and calls `AppBuilder.Configure<HeadlessUnitTestApplication>().UseHeadless(...).SetupUnsafe()`. Popups default to dedicated top-levels, matching desktop. The session uses `SetupUnsafe`, so `OnFrameworkInitializationCompleted` does not run; styles are added on the application constructor.

`[HeadlessTestMethod]` is what dispatches onto the session. `[PresentationTestMethod]` stays on the test thread and is for helpers that do not need the platform.

## Product and theme tests

Tests that need `Cornerstone.Presentation.Theme` live in `Cornerstone.UnitTests`, not in the fork suite.

`TestAppBuilder` is the assembly entry point (`[assembly: PresentationTestApplication(typeof(TestAppBuilder))]`). `BuildCornerstoneApp` configures a small `Application`, calls `UseSkia()` and `UseHeadless`, and leaves drawing headless (`UseHeadlessDrawing = false`) so layout and measure stay close to a real app. `Initialize` adds `CornerstoneTheme`. Isolation is `PerTest`, so a test that touched Cornerstone off the session does not leak a dispatcher into the next one.

Derive from the base class in `CornerstonePresentationUnitTest.cs` (`CornerstoneCornerstoneUnitTest`). `AssemblyInitialize` starts the session before any test constructs UI objects. Put control construction, XAML load, and measure inside `RunOnUi`. That method dispatches onto the session, waits up to 60 seconds, and promotes theme resources so `StaticResource` resolves on controls that have not been loaded into a tree.

## What to copy

For a new runtime test, add a `[TestClass]` in `Cornerstone.Presentation.UnitTests` and mark methods that touch the platform with `[HeadlessTestMethod]`.

For a new product or theme test, add a `[TestClass]` in `Cornerstone.UnitTests` that derives from `CornerstoneCornerstoneUnitTest`, and do the UI work inside `RunOnUi`.

Project shape, generated `Main`, and CS5001 / CSG003 are covered in [Testing.md](../Testing.md).
