#region References

using System;
using System.Reactive.Concurrency;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class TestServices
{
	#region Constructors

	internal TestServices(
		IAssetLoader assetLoader = null,
		IInputManager inputManager = null,
		IGlobalClock globalClock = null,
		Func<IKeyboardDevice> keyboardDevice = null,
		Func<IKeyboardNavigationHandler> keyboardNavigation = null,
		Func<IMouseDevice> mouseDevice = null,
		IRuntimePlatform platform = null,
		IPlatformRenderInterface renderInterface = null,
		IPlatformSettings platformSettings = null,
		ICursorFactory standardCursorFactory = null,
		Func<IStyle> theme = null,
		IFontManagerImpl fontManagerImpl = null,
		ITextShaperImpl textShaperImpl = null,
		IWindowImpl windowImpl = null,
		IWindowingPlatform windowingPlatform = null,
		Func<IAccessKeyHandler> accessKeyHandler = null)
	{
		AssetLoader = assetLoader;
		InputManager = inputManager;
		GlobalClock = globalClock;
		AccessKeyHandler = accessKeyHandler;
		KeyboardDevice = keyboardDevice;
		KeyboardNavigation = keyboardNavigation;
		MouseDevice = mouseDevice;
		Platform = platform;
		RenderInterface = renderInterface;
		PlatformSettings = platformSettings;
		FontManagerImpl = fontManagerImpl;
		TextShaperImpl = textShaperImpl;
		StandardCursorFactory = standardCursorFactory;
		Theme = theme;
		WindowImpl = windowImpl;
		WindowingPlatform = windowingPlatform;
	}

	#endregion

	#region Properties

	public IAssetLoader AssetLoader { get; }

	public static TestServices FocusableWindow =>
		new(
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler(),
			inputManager: new InputManager(),
			assetLoader: new StandardAssetLoader(),
			platform: new StandardRuntimePlatform(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			standardCursorFactory: new StubCursorFactory(),
			theme: () => CreateCornerstoneTheme(),
			fontManagerImpl: new TestFontManager(),
			textShaperImpl: new HarfBuzzTextShaper(),
			windowingPlatform: new MockWindowingPlatform());

	public IFontManagerImpl FontManagerImpl { get; }
	public IInputManager InputManager { get; }
	public Func<IKeyboardDevice> KeyboardDevice { get; }

	public static TestServices MockPlatformRenderInterface =>
		new(
			new StandardAssetLoader(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			fontManagerImpl: new TestFontManager(),
			textShaperImpl: new HarfBuzzTextShaper());

	public static TestServices MockPlatformWrapper =>
		new(
			platform: new StubRuntimePlatform());

	public static TestServices MockThreadingInterface =>
		new(
			new StandardAssetLoader());

	public static TestServices MockWindowingPlatform =>
		new(
			windowingPlatform: new MockWindowingPlatform());

	public Func<IMouseDevice> MouseDevice { get; }
	public IRuntimePlatform Platform { get; }
	public IPlatformSettings PlatformSettings { get; }

	public static TestServices RealFocus =>
		new(
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler(),
			inputManager: new InputManager(),
			assetLoader: new StandardAssetLoader(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			fontManagerImpl: new TestFontManager(),
			textShaperImpl: new HarfBuzzTextShaper());

	public IPlatformRenderInterface RenderInterface { get; }
	public ICursorFactory StandardCursorFactory { get; }

	public static TestServices StyledWindow =>
		new(
			new StandardAssetLoader(),
			platform: new StandardRuntimePlatform(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			standardCursorFactory: new StubCursorFactory(),
			theme: () => CreateCornerstoneTheme(),
			fontManagerImpl: new TestFontManager(),
			textShaperImpl: new HarfBuzzTextShaper(),
			windowingPlatform: new MockWindowingPlatform());

	public static TestServices TextServices =>
		new(
			new StandardAssetLoader(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			fontManagerImpl: new TestFontManager(),
			textShaperImpl: new HarfBuzzTextShaper());

	public ITextShaperImpl TextShaperImpl { get; }
	public Func<IStyle> Theme { get; }
	public IWindowImpl WindowImpl { get; }
	public IWindowingPlatform WindowingPlatform { get; }
	internal Func<IAccessKeyHandler> AccessKeyHandler { get; }
	internal IGlobalClock GlobalClock { get; set; }
	internal Func<IKeyboardNavigationHandler> KeyboardNavigation { get; }

	#endregion

	#region Methods

	internal TestServices With(
		IAssetLoader assetLoader = null,
		IInputManager inputManager = null,
		IGlobalClock globalClock = null,
		Func<IAccessKeyHandler> accessKeyHandler = null,
		Func<IKeyboardDevice> keyboardDevice = null,
		Func<IKeyboardNavigationHandler> keyboardNavigation = null,
		Func<IMouseDevice> mouseDevice = null,
		IRuntimePlatform platform = null,
		IPlatformRenderInterface renderInterface = null,
		IPlatformSettings platformSettings = null,
		IRenderTimer renderLoop = null,
		IScheduler scheduler = null,
		ICursorFactory standardCursorFactory = null,
		Func<IStyle> theme = null,
		IFontManagerImpl fontManagerImpl = null,
		ITextShaperImpl textShaperImpl = null,
		IWindowImpl windowImpl = null,
		IWindowingPlatform windowingPlatform = null)
	{
		return new TestServices(
			assetLoader ?? AssetLoader,
			inputManager ?? InputManager,
			globalClock ?? GlobalClock,
			accessKeyHandler: accessKeyHandler ?? AccessKeyHandler,
			keyboardDevice: keyboardDevice ?? KeyboardDevice,
			keyboardNavigation: keyboardNavigation ?? KeyboardNavigation,
			mouseDevice: mouseDevice ?? MouseDevice,
			platform: platform ?? Platform,
			renderInterface: renderInterface ?? RenderInterface,
			platformSettings: platformSettings ?? PlatformSettings,
			fontManagerImpl: fontManagerImpl ?? FontManagerImpl,
			textShaperImpl: textShaperImpl ?? TextShaperImpl,
			standardCursorFactory: standardCursorFactory ?? StandardCursorFactory,
			theme: theme ?? Theme,
			windowingPlatform: windowingPlatform ?? WindowingPlatform,
			windowImpl: windowImpl ?? WindowImpl);
	}

	private static IStyle CreateCornerstoneTheme()
	{
		return new CornerstoneTheme();
	}

	#endregion
}