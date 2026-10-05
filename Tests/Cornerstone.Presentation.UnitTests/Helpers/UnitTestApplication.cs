#region References

using System;
using System.Reactive.Disposables;
using System.Threading;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class UnitTestApplication : Application
{
	#region Constructors

	public UnitTestApplication() : this(null)
	{
	}

	public UnitTestApplication(TestServices services)
	{
		Services = services ?? new TestServices();
		PresentationLocator.CurrentMutable.BindToSelf<Application>(this);
		RegisterServices();
	}

	static UnitTestApplication()
	{
		AssetLoader.RegisterResUriParsers();
	}

	#endregion

	#region Properties

	public new static UnitTestApplication Current => (UnitTestApplication) Application.Current!;

	public TestServices Services { get; }

	#endregion

	#region Methods

	public override void RegisterServices()
	{
		// Arrange (as part of layouting) calls TaskScheduler.FromCurrentSynchronizationContext, which needs a non-null context.
		// If it's null, it will fail. So we need to ensure it's not null.
		// Testhost does not always install a SynchronizationContext.
		if (SynchronizationContext.Current is null)
		{
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
		}

		PresentationLocator.CurrentMutable
			.Bind<IAssetLoader>().ToConstant(Services.AssetLoader)
			.Bind<IGlobalClock>().ToConstant(Services.GlobalClock ?? new MockGlobalClock())
			.BindToSelf<IGlobalStyles>(this)
			.Bind<IInputManager>().ToConstant(Services.InputManager)
			.Bind<IToolTipService>().ToConstant(Services.InputManager == null ? null : new ToolTipService(Services.InputManager))
			.Bind<IKeyboardDevice>().ToConstant(Services.KeyboardDevice?.Invoke())
			.Bind<IMouseDevice>().ToConstant(Services.MouseDevice?.Invoke())
			.Bind<IKeyboardNavigationHandler>().ToFunc(Services.KeyboardNavigation ?? (() => null))
			.Bind<IRuntimePlatform>().ToConstant(Services.Platform)
			.Bind<IPlatformRenderInterface>().ToConstant(Services.RenderInterface ?? new HeadlessPlatformRenderInterface())
			.Bind<IFontManagerImpl>().ToConstant(Services.FontManagerImpl ?? new TestFontManager())
			.Bind<ITextShaperImpl>().ToConstant(Services.TextShaperImpl)
			.Bind<ICursorFactory>().ToConstant(Services.StandardCursorFactory)
			.Bind<IWindowingPlatform>().ToConstant(Services.WindowingPlatform ?? new MockWindowingPlatform())
			.Bind<IPlatformIconLoader>().ToConstant(new HeadlessIconLoaderStub())
			.Bind<CornerstoneXamlLoader.IRuntimeXamlLoader>().ToConstant(new TestRuntimeXamlLoader())
			.Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
			.Bind<IPlatformSettings>().ToConstant(Services.PlatformSettings ?? new DefaultPlatformSettings())
			.Bind<IAccessKeyHandler>().ToFunc(Services.AccessKeyHandler ?? (() => null));

		InitializeThemeVariant();

		// This is a hack to make tests work, we need to refactor the way font manager is registered
		// See https://github.com/AvaloniaUI/Avalonia/issues/10081
		PresentationLocator.CurrentMutable.Bind<FontManager>().ToConstant((FontManager) null!);
		var theme = Services.Theme?.Invoke();

		if (theme is Style styles)
		{
			Styles.AddRange(styles.Children);
		}
		else if (theme is not null)
		{
			Styles.Add(theme);
		}
	}

	public static IDisposable Start(TestServices services = null)
	{
		var scope = PresentationLocator.EnterScope();
		var oldContext = SynchronizationContext.Current;
		_ = new UnitTestApplication(services);
		Dispatcher.ResetBeforeUnitTests();
		return Disposable.Create(() =>
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				Dispatcher.UIThread.RunJobs();
			}

			(PresentationLocator.Current.GetService<IToolTipService>() as ToolTipService)?.Dispose();
			(PresentationLocator.Current.GetService<FontManager>() as IDisposable)?.Dispose();
			(PresentationLocator.Current.GetService<IInputManager>() as IDisposable)?.Dispose();

			Dispatcher.ResetForUnitTests();
			scope.Dispose();
			Dispatcher.ResetBeforeUnitTests();
			SynchronizationContext.SetSynchronizationContext(oldContext);
		});
	}

	#endregion
}