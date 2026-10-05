#include "common.h"
#include "CsnString.h"
#include "menu.h"
@interface CsnAppDelegate : NSObject<NSApplicationDelegate>
-(CsnAppDelegate* _Nonnull) initWithEvents: (ICsnApplicationEvents* _Nonnull) events;
-(void) releaseEvents;
@end

NSApplicationActivationPolicy CsnDesiredActivationPolicy = NSApplicationActivationPolicyRegular;
static NSMenu* s_dockMenu = nil;

static bool IsOSShutdown()
{
    auto evt = [[NSAppleEventManager sharedAppleEventManager] currentAppleEvent];
    if ([evt eventClass] != kCoreEventClass || [evt eventID] != kAEQuitApplication)
        return false;

    auto reason = [evt paramDescriptorForKeyword:kAEQuitReason];
    if (reason == nil)
        reason = [evt attributeDescriptorForKeyword:kAEQuitReason];

    auto reasonCode = [reason enumCodeValue];
    if (reasonCode == 0)
        reasonCode = [reason typeCodeValue];

    switch (reasonCode)
    {
        case kAELogOut:
        case kAEReallyLogOut:
        case kAEShowRestartDialog:
        case kAERestart:
        case kAEShowShutdownDialog:
        case kAEShutDown:
            return true;
        default:
            return false;
    }
}

@implementation CsnAppDelegate
ComPtr<ICsnApplicationEvents> _events;

- (CsnAppDelegate *)initWithEvents:(ICsnApplicationEvents *)events
{
    _events = events;
    return self;
}

- (void)releaseEvents
{
    _events = nil;
}

- (void)applicationWillFinishLaunching:(NSNotification *)notification
{
    if([[NSApplication sharedApplication] activationPolicy] != CsnDesiredActivationPolicy)
    {
        for (NSRunningApplication * app in [NSRunningApplication runningApplicationsWithBundleIdentifier:@"com.apple.dock"]) {
            [app activateWithOptions:NSApplicationActivateIgnoringOtherApps];
            break;
        }
        
        [[NSUserDefaults standardUserDefaults] setBool:NO forKey:@"NSFullScreenMenuItemEverywhere"];
        
        [[NSApplication sharedApplication] setHelpMenu: [[NSMenu new] initWithTitle:@""]];
    }
}

- (void)applicationDidFinishLaunching:(NSNotification *)notification
{
    [[NSRunningApplication currentApplication] activateWithOptions:NSApplicationActivateIgnoringOtherApps];
}

-(BOOL)applicationShouldHandleReopen:(NSApplication *)sender hasVisibleWindows:(BOOL)flag
{
    _events->OnReopen();
    return YES;
}

- (void)applicationDidHide:(NSNotification *)notification
{
    _events->OnHide();
}

- (void)applicationDidUnhide:(NSNotification *)notification
{
    _events->OnUnhide();
}

- (void) applicationDidBecomeActive:(NSNotification *) notification
{
    _events->OnActivate();
}

- (void) applicationDidResignActive:(NSNotification *) notification
{
    _events->OnDeactivate();
}

- (void)application:(NSApplication *)sender openFiles:(NSArray<NSString *> *)filenames
{
    auto array = CreateCsnStringArray(filenames);
    
    _events->FilesOpened(array);
}

- (void)application:(NSApplication *)application openURLs:(NSArray<NSURL *> *)urls
{
    auto array = CreateCsnStringArray(urls);
    
    _events->UrlsOpened(array);
}

- (NSApplicationTerminateReply)applicationShouldTerminate:(NSApplication *)sender
{
    switch (_events->TryShutdown(IsOSShutdown()))
    {
        case ShutdownReplyCancel:
            return NSTerminateCancel;
            
        // The managed dispatcher loop is exiting: let it handle the termination instead.
        case ShutdownReplyDeferToManagedLoop:
            return NSTerminateCancel;

        case ShutdownReplyTerminateNow:
            return NSTerminateNow;

        // Shouldn't happen
        default:
            return NSTerminateNow;
    }
}

- (void)applicationWillTerminate:(NSNotification *)notification
{
    if (!_events)
        return;
    
    // The process is about to exit() so this is the last point where managed code can still safely be called.
    // Keep the application events object alive for the duration of the call, it's about to be released by the managed side.
    ComPtr<ICsnApplicationEvents> events(_events);
    events->OnTerminating();
}

- (NSMenu *)applicationDockMenu:(NSApplication *)sender
{
    return s_dockMenu;
}

@end

@interface CsnApplication : NSApplication

@end

@implementation CsnApplication
{
    BOOL _isHandlingSendEvent;
}

- (void)sendEvent:(NSEvent *)event
{
    bool oldHandling = _isHandlingSendEvent;
    _isHandlingSendEvent = true;
    @try {
        [super sendEvent: event];
        if ([event type] == NSEventTypeKeyUp && ([event modifierFlags] & NSEventModifierFlagCommand))
        {
            [[self keyWindow] sendEvent:event];
        }
        
    } @finally {
        _isHandlingSendEvent = oldHandling;
    }
}

// This is needed for certain embedded controls DO NOT REMOVE..
- (BOOL) isHandlingSendEvent
{
    return _isHandlingSendEvent;
}

- (void)setHandlingSendEvent:(BOOL)handlingSendEvent
{
    _isHandlingSendEvent = handlingSendEvent;
}
@end

extern void InitializeCsnApp(ICsnApplicationEvents* events, bool disableAppDelegate)
{
    if(!disableAppDelegate)
    {
        NSApplication* app = [CsnApplication sharedApplication];
        id delegate = [[CsnAppDelegate alloc] initWithEvents:events];
        [app setDelegate:delegate];
    }
}

extern void ReleaseCsnAppEvents()
{
    NSApplication* app = [CsnApplication sharedApplication];
    id delegate = [app delegate];
    if ([delegate isMemberOfClass:[CsnAppDelegate class]])
    {
        CsnAppDelegate* csnDelegate = delegate;
        [csnDelegate releaseEvents];
        [app setDelegate:nil];
    }
}

HRESULT CsnApplicationCommands::UnhideApp()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] unhide:[NSApp delegate]];
    return S_OK;
}

HRESULT CsnApplicationCommands::HideApp()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] hide:[NSApp delegate]];
    return S_OK;
}

HRESULT CsnApplicationCommands::ShowAll()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] unhideAllApplications:[NSApp delegate]];
    return S_OK;
}

HRESULT CsnApplicationCommands::HideOthers()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] hideOtherApplications:[NSApp delegate]];
    return S_OK;
}


extern ICsnApplicationCommands* CreateApplicationCommands()
{
    return new CsnApplicationCommands();
}

extern void SetDockMenu(NSMenu* menu)
{
    s_dockMenu = menu;
}
