using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Platform.Storage.FileIO;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class MacApplicationPlatform(MacPlatform platform)
        : NativeCallbackBase, ICsnApplicationEvents, IPlatformLifetimeEventsImpl
    {
        public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;

        void ICsnApplicationEvents.FilesOpened(ICsnStringArray urls)
        {
            if (PresentationLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime
                && PresentationLocator.Current.GetService<IStorageProviderFactory>() is StorageProviderApi storageApi)
            {
                var filePaths = urls.ToStringArray();
                var files = new List<IStorageItem>(filePaths.Length);
                foreach (var filePath in filePaths)
                {
                    if (StorageProviderHelpers.TryGetUriFromFilePath(filePath, false) is { } fileUri
                        && storageApi.TryGetStorageItem(fileUri) is { } file)
                    {
                        files.Add(file);
                    }
                }

                if (files.Count > 0)
                {
                    lifetime.OnActivated(new FileActivatedEventArgs(files));
                }
            }
        }

        void ICsnApplicationEvents.UrlsOpened(ICsnStringArray urls)
        {
            if (PresentationLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime
                && PresentationLocator.Current.GetService<IStorageProviderFactory>() is StorageProviderApi storageApi)
            {
                var files = new List<IStorageItem>();
                var uris = new List<Uri>();
                foreach (var url in urls.ToStringArray())
                {
                    if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                    {
                        if (uri.Scheme == Uri.UriSchemeFile)
                        {
                            if (storageApi.TryGetStorageItem(uri) is { } file)
                            {
                                files.Add(file);
                            }
                        }
                        else
                        {
                            uris.Add(uri);
                        }
                    }
                }

                foreach (var uri in uris)
                {
                    lifetime.OnActivated(new ProtocolActivatedEventArgs(uri));
                }
                if (files.Count > 0)
                {
                    lifetime.OnActivated(new FileActivatedEventArgs(files));
                }
            }
        }

        void ICsnApplicationEvents.OnReopen()
        {
            if (PresentationLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnActivated(ActivationKind.Reopen);    
            }
        }

        void ICsnApplicationEvents.OnHide()
        {
        }

        void ICsnApplicationEvents.OnUnhide()
        {
        }

        void ICsnApplicationEvents.OnActivate()
        {
            if (PresentationLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnActivated(ActivationKind.Background);
            }
        }

        void ICsnApplicationEvents.OnDeactivate()
        {
            if (PresentationLocator.Current.GetService<IActivatableLifetime>() is ActivatableLifetimeBase lifetime)
            {
                lifetime.OnDeactivated(ActivationKind.Background);
            }
        }

        void ICsnApplicationEvents.OnTerminating()
        {
            // The OS is terminating us directly: AppDomain.ProcessExit won't run, dispose now.
            platform.Dispose();
        }

        public CsnShutdownReply TryShutdown(int isOSShutdown)
        {
            if (ShutdownRequested is not { } shutdownRequested)
                return CsnShutdownReply.ShutdownReplyTerminateNow;

            var isOSShutdownBool = isOSShutdown.FromComBool();
            var e = new ShutdownRequestedEventArgs { IsOSShutdown = isOSShutdownBool };
            shutdownRequested.Invoke(this, e);

            if (e.Cancel)
                return CsnShutdownReply.ShutdownReplyCancel;

            // If we know the main loop is going to exit (e.g. via a ClassicDesktopApplicationLifetime),
            // tell the native side it doesn't have to exit, allowing the managed side to complete its shutdown.
            if (e.WillExitMainLoop && !isOSShutdownBool)
                return CsnShutdownReply.ShutdownReplyDeferToManagedLoop;

            return CsnShutdownReply.ShutdownReplyTerminateNow;
        }
    }
}
