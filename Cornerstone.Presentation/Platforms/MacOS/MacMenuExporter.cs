using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Dialogs;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platforms.MacOS.Interop.Impl;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.NativeHosts;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class MacMenuExporter : ITopLevelNativeMenuExporter
    {
        internal enum MenuTarget { Application, Window, TrayIcon, Dock }

        private readonly ICornerstoneNativeFactory _factory;
        private readonly MenuTarget _target;
        private bool _resetQueued = true;
        private bool _exported;
        private readonly ICsnWindow? _nativeWindow;
        private NativeMenu? _menu;
        private __MicroComICsnMenuProxy? _nativeMenu;
        private readonly ICsnTrayIcon? _trayIcon;
        private readonly ICsnApplicationCommands? _applicationCommands;

        public MacMenuExporter(ICsnWindow nativeWindow, ICornerstoneNativeFactory factory)
        {
            _factory = factory;
            _target = MenuTarget.Window;
            _nativeWindow = nativeWindow;
            _applicationCommands = _factory.CreateApplicationCommands();

            DoLayoutReset();
        }

        public MacMenuExporter(ICornerstoneNativeFactory factory)
        {
            _factory = factory;
            _target = MenuTarget.Application;
            _applicationCommands = _factory.CreateApplicationCommands();

            DoLayoutReset();
        }

        public MacMenuExporter(ICsnTrayIcon trayIcon, ICornerstoneNativeFactory factory)
        {
            _factory = factory;
            _target = MenuTarget.TrayIcon;
            _trayIcon = trayIcon;
            _applicationCommands = _factory.CreateApplicationCommands();

            DoLayoutReset();
        }

        internal MacMenuExporter(ICornerstoneNativeFactory factory, MenuTarget target)
        {
            _factory = factory;
            _target = target;
            _resetQueued = false;

            var macOpts = PresentationLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();

            if (macOpts.DisableNativeMenus)
            {
                return;
            }

            NativeDock.MenuProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is Application)
                {
                    SetNativeMenu(args.NewValue.GetValueOrDefault());
                }
            });

            var app = Application.Current;
            if (app is not null)
            {
                var dockMenu = NativeDock.GetMenu(app);
                if (dockMenu is not null)
                {
                    SetNativeMenu(dockMenu);
                }
            }
        }

        public bool IsNativeMenuExported => _exported;

        public event EventHandler OnIsNativeMenuExportedChanged { add { } remove { } }

        public void SetNativeMenu(NativeMenu? menu)
        {
            if (_target == MenuTarget.Dock)
            {
                _menu = menu;

                if (_menu is not null)
                {
                    DoLayoutReset(true);
                }
            }
            else
            {
                _menu = menu ?? new NativeMenu();
                DoLayoutReset(true);
            }
        }

        internal void UpdateIfNeeded()
        {
            if (_resetQueued)
            {
                DoLayoutReset();
            }
        }

        private static NativeMenu CreateDefaultAppMenu()
        {
            var result = new NativeMenu();

            var aboutItem = new NativeMenuItem("About Cornerstone");

            aboutItem.Click += async (_, _) =>
            {
                var dialog = new AboutCornerstoneDialog();

                if (Application.Current is
                    { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime { MainWindow: { IsVisible: true } mainWindow } })
                {
                    await dialog.ShowDialog(mainWindow);
                }
                else
                {
                    dialog.Show();
                }
            };

            result.Add(aboutItem);

            return result;
        }

        private void PopulateStandardOSXMenuItems(NativeMenu appMenu)
        {
            appMenu.Add(new NativeMenuItemSeparator());

            var servicesMenu = new NativeMenuItem("Services");
            servicesMenu.Menu = new NativeMenu { [MacOSNativeMenuCommands.IsServicesSubmenuProperty] = true };

            appMenu.Add(servicesMenu);

            appMenu.Add(new NativeMenuItemSeparator());

            var hideItem = new NativeMenuItem("Hide " + (Application.Current?.Name ?? "Application"))
            {
                Gesture = new KeyGesture(Key.H, KeyModifiers.Meta)
            };

            hideItem.Click += (_, _) =>
            {
                _applicationCommands?.HideApp();
            };

            appMenu.Add(hideItem);

            var hideOthersItem = new NativeMenuItem("Hide Others")
            {
                Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta | KeyModifiers.Alt)
            };
            hideOthersItem.Click += (_, _) =>
            {
                _applicationCommands?.HideOthers();
            };
            appMenu.Add(hideOthersItem);

            var showAllItem = new NativeMenuItem("Show All");
            showAllItem.Click += (_, _) =>
            {
                _applicationCommands?.ShowAll();
            };

            appMenu.Add(showAllItem);

            appMenu.Add(new NativeMenuItemSeparator());

            var quitItem = new NativeMenuItem("Quit") { Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta) };
            quitItem.Click += (_, _) =>
            {
                if (Application.Current is { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime lifetime })
                {
                    lifetime.TryShutdown();
                }
                else if(Application.Current is {ApplicationLifetime: IControlledApplicationLifetime controlledLifetime})
                {
                    controlledLifetime.Shutdown();
                }
            };

            appMenu.Add(quitItem);
        }

        private void DoLayoutReset() => DoLayoutReset(false);

        private void DoLayoutReset(bool forceUpdate)
        {
            var macOpts = PresentationLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();

            if (macOpts.DisableNativeMenus)
            {
                return;
            }

            if (_resetQueued || forceUpdate)
            {
                _resetQueued = false;

                switch (_target)
                {
                    case MenuTarget.Application:
                    {
                        var app = Application.Current;
                        var appMenu = app is null ? null : NativeMenu.GetMenu(app);

                        if (appMenu == null)
                        {
                            appMenu = CreateDefaultAppMenu();

                            if (app is not null)
                                NativeMenu.SetMenu(app, appMenu);
                        }

                        SetMenu(appMenu);
                        break;
                    }

                    case MenuTarget.Window:
                    {
                        if (_menu != null)
                        {
                            SetMenu(_nativeWindow, _menu);
                        }
                        break;
                    }

                    case MenuTarget.TrayIcon:
                    {
                        if (_menu != null)
                        {
                            SetMenu(_trayIcon, _menu);
                        }
                        break;
                    }

                    case MenuTarget.Dock:
                    {
                        if (_menu != null)
                        {
                            SetDockMenu(_menu);
                        }
                        break;
                    }
                }

                _exported = true;
            }
        }

        internal void QueueReset()
        {
            if (_resetQueued)
                return;
            _resetQueued = true;
            Dispatcher.UIThread.Post(DoLayoutReset, DispatcherPriority.Background);
        }

        private void SetMenu(NativeMenu menu)
        {
            var menuItem = menu.Parent;

            var appMenuHolder = menuItem?.Parent;

            if (menuItem is null)
            {
                menuItem = new NativeMenuItem();
            }

            if (appMenuHolder is null)
            {
                appMenuHolder = new NativeMenu();

                appMenuHolder.Add(menuItem);
            }

            menuItem.Menu = menu;

            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComICsnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, appMenuHolder, "");

                var macOpts = PresentationLocator.Current.GetService<MacOSPlatformOptions>();

                if (macOpts == null || !macOpts.DisableDefaultApplicationMenuItems)
                {
                    PopulateStandardOSXMenuItems(menu);
                }

                setMenu = true;
            }

            _nativeMenu.Update(_factory, appMenuHolder);

            if (setMenu)
            {
                _factory.SetAppMenu(_nativeMenu);
            }
        }

        private void SetMenu(ICsnWindow? csnWindow, NativeMenu menu)
        {
            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComICsnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, menu, "");

                setMenu = true;
            }

            _nativeMenu.Update(_factory, menu);

            if(setMenu)
            {
                csnWindow?.SetMainMenu(_nativeMenu);
            }
        }

        private void SetMenu(ICsnTrayIcon? trayIcon, NativeMenu menu)
        {
            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComICsnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, menu, "");

                setMenu = true;
            }

            _nativeMenu.Update(_factory, menu);

            if(setMenu)
            {
                trayIcon?.SetMenu(_nativeMenu);
            }
        }

        private void SetDockMenu(NativeMenu menu)
        {
            var setMenu = false;

            if (_nativeMenu is null)
            {
                _nativeMenu = __MicroComICsnMenuProxy.Create(_factory);

                _nativeMenu.Initialize(this, menu, "");

                setMenu = true;
            }

            _nativeMenu.Update(_factory, menu);

            if (setMenu)
            {
                _factory.SetDockMenu(_nativeMenu);
            }
        }
    }
}
