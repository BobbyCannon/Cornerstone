//This file will contain actual IID structures
#define COM_GUIDS_MATERIALIZE
#include "common.h"
#include "menu.h"

static NSString* s_appTitle = @"Cornerstone";
static int disableSetProcessName = 0;
static bool disableAppDelegate = false;

// Copyright (c) 2011 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.
void SetProcessName(NSString* appTitle) {
    s_appTitle = appTitle;
    
    CFStringRef process_name = (__bridge CFStringRef)appTitle;
    
    if (!process_name || CFStringGetLength(process_name) == 0) {
        //NOTREACHED() << "SetProcessName given bad name.";
        return;
    }
    
    if (![NSThread isMainThread]) {
        //NOTREACHED() << "Should only set process name from main thread.";
        return;
    }
    
    // Warning: here be dragons! This is SPI reverse-engineered from WebKit's
    // plugin host, and could break at any time (although realistically it's only
    // likely to break in a new major release).
    // When 10.7 is available, check that this still works, and update this
    // comment for 10.8.
    
    // Private CFType used in these LaunchServices calls.
    typedef CFTypeRef PrivateLSASN;
    typedef PrivateLSASN (*LSGetCurrentApplicationASNType)();
    typedef OSStatus (*LSSetApplicationInformationItemType)(int, PrivateLSASN,
                                                            CFStringRef,
                                                            CFStringRef,
                                                            CFDictionaryRef*);
    
    static LSGetCurrentApplicationASNType ls_get_current_application_asn_func =
    NULL;
    static LSSetApplicationInformationItemType
    ls_set_application_information_item_func = NULL;
    static CFStringRef ls_display_name_key = NULL;
    
    static bool did_symbol_lookup = false;
    if (!did_symbol_lookup) {
        did_symbol_lookup = true;
        CFBundleRef launch_services_bundle =
        CFBundleGetBundleWithIdentifier(CFSTR("com.apple.LaunchServices"));
        if (!launch_services_bundle) {
            //LOG(ERROR) << "Failed to look up LaunchServices bundle";
            return;
        }
        
        ls_get_current_application_asn_func =
        reinterpret_cast<LSGetCurrentApplicationASNType>(
                                                         CFBundleGetFunctionPointerForName(
                                                                                           launch_services_bundle, CFSTR("_LSGetCurrentApplicationASN")));
        if (!ls_get_current_application_asn_func){}
        //LOG(ERROR) << "Could not find _LSGetCurrentApplicationASN";
        
        ls_set_application_information_item_func =
        reinterpret_cast<LSSetApplicationInformationItemType>(
                                                              CFBundleGetFunctionPointerForName(
                                                                                                launch_services_bundle,
                                                                                                CFSTR("_LSSetApplicationInformationItem")));
        if (!ls_set_application_information_item_func){}
        //LOG(ERROR) << "Could not find _LSSetApplicationInformationItem";
        
        CFStringRef* key_pointer = reinterpret_cast<CFStringRef*>(
                                                                  CFBundleGetDataPointerForName(launch_services_bundle,
                                                                                                CFSTR("_kLSDisplayNameKey")));
        ls_display_name_key = key_pointer ? *key_pointer : NULL;
        if (!ls_display_name_key){}
        //LOG(ERROR) << "Could not find _kLSDisplayNameKey";
        
        // Internally, this call relies on the Mach ports that are started up by the
        // Carbon Process Manager.  In debug builds this usually happens due to how
        // the logging layers are started up; but in release, it isn't started in as
        // much of a defined order.  So if the symbols had to be loaded, go ahead
        // and force a call to make sure the manager has been initialized and hence
        // the ports are opened.
        ProcessSerialNumber psn;
        GetCurrentProcess(&psn);
    }
    if (!ls_get_current_application_asn_func ||
        !ls_set_application_information_item_func ||
        !ls_display_name_key) {
        return;
    }
    
    PrivateLSASN asn = ls_get_current_application_asn_func();
    // Constant used by WebKit; what exactly it means is unknown.
    const int magic_session_constant = -2;
    
    ls_set_application_information_item_func(magic_session_constant, asn,
                                             ls_display_name_key,
                                             process_name,
                                             NULL /* optional out param */);
}

class MacOptions : public ComSingleObject<ICsnMacOptions, &IID_ICsnMacOptions>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT SetApplicationTitle(char* utf8String) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto appTitle = [NSString stringWithUTF8String: utf8String];
            if (disableSetProcessName == 0)
            {
                [[NSProcessInfo processInfo] setProcessName:appTitle];
                
                SetProcessName(appTitle);
            }
            if (disableSetProcessName == 1)
            {
                auto rootMenu = [NSApp mainMenu];
                [rootMenu setTitle:appTitle];
            }
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetShowInDock(int show) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            NSApplication* app = [NSApplication sharedApplication];
            NSApplicationActivationPolicy requestedPolicy = show
                ? NSApplicationActivationPolicyRegular
                : NSApplicationActivationPolicyAccessory;
            
            if ([app activationPolicy] != requestedPolicy)
            {
                [app setActivationPolicy:requestedPolicy];
            }
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetDisableSetProcessName(int disable)  override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            disableSetProcessName = disable;
            return S_OK;
        }
    }
    
    virtual HRESULT SetDisableAppDelegate(int disable) override
    {
        START_COM_CALL;
        
        @autoreleasepool {
            disableAppDelegate = disable;
            return S_OK;
        }
    }
    
};

/// See "Using POSIX Threads in a Cocoa Application" section here:
/// https://developer.apple.com/library/content/documentation/Cocoa/Conceptual/Multithreading/CreatingThreads/CreatingThreads.html#//apple_ref/doc/uid/20000738-125024
@interface ThreadingInitializer : NSObject
- (void) do;
@end
@implementation ThreadingInitializer
{
    int _fds[2];
}
- (void) runOnce
{
    char buf[]={0};
    write(_fds[1], buf, 1);
}

- (void) do
{
    pipe(_fds);
    [[[NSThread alloc] initWithTarget:self selector:@selector(runOnce) object:nil] start];
    char buf[1];
    read(_fds[0], buf, 1);
    close(_fds[0]);
    close(_fds[1]);
}
@end

static ComPtr<ICsnGCHandleDeallocatorCallback> _deallocator;
static ComPtr<ICsnDispatcher> _dispatcher;
class CornerstoneNative : public ComSingleObject<ICornerstoneNativeFactory, &IID_ICornerstoneNativeFactory>
{
    
public:
    FORWARD_IUNKNOWN()
    
    virtual ~CornerstoneNative() override
    {
        ReleaseCsnAppEvents();
        _deallocator = nullptr;
        _dispatcher = nullptr;
    }
    
    virtual HRESULT Initialize(ICsnGCHandleDeallocatorCallback* deallocator,
            ICsnApplicationEvents* events,
            ICsnDispatcher* dispatcher) override
    {
        START_COM_CALL;
        
        _deallocator = deallocator;
        _dispatcher = dispatcher;
        @autoreleasepool{
            [[ThreadingInitializer new] do];
        }
        InitializeCsnApp(events, disableAppDelegate);
        return S_OK;
    };
    
    virtual ICsnMacOptions* GetMacOptions()  override
    {
        return (ICsnMacOptions*)new MacOptions();
    }
    
    virtual HRESULT CreateTopLevel(ICsnTopLevelEvents* cb,
                                           ICsnTopLevel** ppv) override {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(cb == nullptr || ppv == nullptr)
                return E_POINTER;
            *ppv = CreateCsnTopLevel(cb);
            return S_OK;
        }
    }
    
    virtual HRESULT CreateWindow(ICsnWindowEvents* cb, ICsnWindow** ppv)  override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(cb == nullptr || ppv == nullptr)
                return E_POINTER;
            *ppv = CreateCsnWindow(cb);
            return S_OK;
        }
    };
    
    virtual HRESULT CreatePopup(ICsnWindowEvents* cb, ICsnPopup** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(cb == nullptr || ppv == nullptr)
                return E_POINTER;
            
            *ppv = CreateCsnPopup(cb);
            return S_OK;
        }
    }
    
    virtual HRESULT CreatePlatformThreadingInterface(ICsnPlatformThreadingInterface** ppv)  override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = CreatePlatformThreading();
            return S_OK;
        }
    }
    
    virtual HRESULT CreateStorageProvider(ICsnStorageProvider** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateStorageProvider();
            return  S_OK;
        }
    }
    
    virtual HRESULT CreateScreens (ICsnScreenEvents* cb, ICsnScreens** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateScreens (cb);
            return S_OK;
        }
    }

    virtual HRESULT CreateClipboard(ICsnClipboard** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateClipboard(nil);
            return S_OK;
        }
    }

    virtual HRESULT CreateCursorFactory(ICsnCursorFactory** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateCursorFactory();
            return S_OK;
        }
    }
    
    virtual HRESULT ObtainGlDisplay(ICsnGlDisplay** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto rv = ::GetGlDisplay();
            if(rv == NULL)
                return E_FAIL;
            rv->AddRef();
            *ppv = rv;
            return S_OK;
        }
    }

    virtual HRESULT ObtainMetalDisplay(ICsnMetalDisplay** ppv) override
    {
        START_COM_CALL;
        @autoreleasepool
        {
            auto rv = ::GetMetalDisplay();
            if(rv == NULL)
                return E_FAIL;
            rv->AddRef();
            *ppv = rv;
            return S_OK;
        }
    }


    virtual HRESULT CreateTrayIcon (ICsnTrayIcon** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateTrayIcon();
            return S_OK;
        }
    }
    
    virtual HRESULT CreateMenu (ICsnMenuEvents* cb, ICsnMenu** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateAppMenu(cb);
            return S_OK;
        }
    }
    
    virtual HRESULT CreateMenuItem (ICsnMenuItem** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateAppMenuItem();
            return S_OK;
        }
    }
    
    virtual HRESULT CreateMenuItemSeparator (ICsnMenuItem** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateAppMenuItemSeparator();
            return S_OK;
        }
    }
        
    virtual HRESULT SetAppMenu (ICsnMenu* appMenu) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            ::SetAppMenu(appMenu);
            return S_OK;
        }
    }
    
    virtual HRESULT SetServicesMenu (ICsnMenu* servicesMenu) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            ::SetServicesMenu(servicesMenu);
            return S_OK;
        }
    }
    
    virtual HRESULT CreateApplicationCommands (ICsnApplicationCommands** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreateApplicationCommands();
            return S_OK;
        }
    }
    
    virtual HRESULT CreatePlatformSettings (ICsnPlatformSettings** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreatePlatformSettings();
            return S_OK;
        }
    }

    virtual HRESULT CreatePlatformBehaviorInhibition(ICsnPlatformBehaviorInhibition** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreatePlatformBehaviorInhibition();
            return S_OK;
        }
    }
    
    virtual HRESULT CreatePlatformRenderTimer(ICsnPlatformRenderTimer** ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *ppv = ::CreatePlatformRenderTimer();
            return S_OK;
        }
    }
    
    virtual HRESULT ImportMTLSharedEvent(void* event, ICsnMTLSharedEvent** ppv) override
    {
        START_COM_CALL;
        *ppv = ::ImportMTLSharedEvent(event);
        return *ppv != nullptr ? S_OK : E_FAIL;
    }
    
    HRESULT CreateMemoryManagementHelper(ICsnNativeObjectsMemoryManagement **ppv) override {
        START_COM_CALL;
        *ppv = ::CreateMemoryManagementHelper();
        return S_OK;
    }

    virtual HRESULT SetDockMenu(ICsnMenu* dockMenu) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            auto nativeMenu = dynamic_cast<CsnAppMenu*>(dockMenu);
            ::SetDockMenu(nativeMenu != nullptr ? nativeMenu->GetNative() : nil);
            return S_OK;
        }
    }

};

extern "C" ICornerstoneNativeFactory* CreateCornerstoneNative()
{
    return new CornerstoneNative();
};

extern void FreeCsnGCHandle(void* handle)
{
    if(_deallocator != nil)
        _deallocator->FreeGCHandle(handle);
}

extern void PostDispatcherCallback(ICsnActionCallback* cb)
{
    _dispatcher->Post(cb);
}

NSSize ToNSSize (CsnSize s)
{
    NSSize result;
    result.width = s.Width;
    result.height = s.Height;
    
    return result;
}

CsnSize FromNSSize (NSSize s)
{
    CsnSize result;
    result.Width = s.width;
    result.Height = s.height;
    
    return result;
}

NSPoint ToNSPoint (CsnPoint p)
{
    NSPoint result;
    result.x = p.X;
    result.y = p.Y;
    
    return result;
}

NSRect ToNSRect (CsnRect r)
{
    return NSRect
    {
        NSPoint { r.X, r.Y },
        NSSize { r.Width, r.Height }
    };
}

CsnPoint ToCsnPoint (NSPoint p)
{
    CsnPoint result;
    result.X = p.x;
    result.Y = p.y;
    
    return result;
}

CsnPoint ConvertPointY (CsnPoint p)
{
    auto primaryDisplayHeight = NSMaxY([[[NSScreen screens] firstObject] frame]);
    
    p.Y = primaryDisplayHeight - p.Y;
    
    return p;
}

