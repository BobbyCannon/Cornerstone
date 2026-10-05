#ifndef common_h
#define common_h
#include "comimpl.h"
#include "cornerstone-native.h"
#include <stdio.h>
#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include <pthread.h>
#include "noarc.h"

extern ICsnPlatformThreadingInterface* CreatePlatformThreading();
extern void FreeCsnGCHandle(void* handle);
extern void PostDispatcherCallback(ICsnActionCallback* cb);
extern ICsnTopLevel* CreateCsnTopLevel(ICsnTopLevelEvents* events);
extern ICsnWindow* CreateCsnWindow(ICsnWindowEvents*events);
extern ICsnPopup* CreateCsnPopup(ICsnWindowEvents*events);
extern ICsnStorageProvider* CreateStorageProvider();
extern ICsnScreens* CreateScreens(ICsnScreenEvents* cb);
extern ICsnClipboard* CreateClipboard(NSPasteboard* pb);
extern NSObject<NSDraggingSource>* CreateDraggingSource(NSDragOperation op, ICsnDndResultCallback* cb, void* handle);
extern void* GetCsnDataObjectHandleFromDraggingInfo(NSObject<NSDraggingInfo>* info);
extern NSString* GetCsnCustomDataType();
extern CsnDragDropEffects ConvertDragDropEffects(NSDragOperation nsop);
extern ICsnCursorFactory* CreateCursorFactory();
extern ICsnGlDisplay* GetGlDisplay();
extern ICsnMetalDisplay* GetMetalDisplay();
extern ICsnMenu* CreateAppMenu(ICsnMenuEvents* events);
extern ICsnTrayIcon* CreateTrayIcon();
extern ICsnMenuItem* CreateAppMenuItem();
extern ICsnMenuItem* CreateAppMenuItemSeparator();
extern ICsnApplicationCommands* CreateApplicationCommands();
extern ICsnPlatformBehaviorInhibition* CreatePlatformBehaviorInhibition();
extern ICsnNativeControlHost* CreateNativeControlHost(NSView* parent);
extern ICsnPlatformSettings* CreatePlatformSettings();
extern ICsnPlatformRenderTimer* CreatePlatformRenderTimer();
extern ICsnNativeObjectsMemoryManagement* CreateMemoryManagementHelper();
extern void SetAppMenu(ICsnMenu *menu);
extern void SetServicesMenu (ICsnMenu* menu);
class CsnAppMenu;
extern CsnAppMenu* GetAppMenu ();
extern NSMenuItem* GetAppMenuItem ();
extern void SetDockMenu(NSMenu* menu);

extern void InitializeCsnApp(ICsnApplicationEvents* events, bool disableAppDelegate);
extern void ReleaseCsnAppEvents();
extern bool CsnNativeBehindComposition;
extern "C" void CsnSetNativeBehindComposition(int enabled);

extern NSApplicationActivationPolicy CsnDesiredActivationPolicy;
extern NSPoint ToNSPoint (CsnPoint p);
extern NSRect ToNSRect (CsnRect r);
extern CsnPoint ToCsnPoint (NSPoint p);
extern CsnPoint ConvertPointY (CsnPoint p);
extern NSSize ToNSSize (CsnSize s);
extern CsnSize FromNSSize (NSSize s);
extern ICsnMTLSharedEvent* ImportMTLSharedEvent(void* object);
#ifdef DEBUG
#define NSDebugLog(...) NSLog(__VA_ARGS__)
#else
#define NSDebugLog(...) (void)0
#endif

template<typename T> inline T* objc_cast(id from) {
    if(from == nil)
        return nil;
    if ([from isKindOfClass:[T class]]) {
        return static_cast<T*>(from);
    }
    return nil;
}

template<typename T> class ObjCWrapper {
public:
    T* Value;
    ObjCWrapper(T* value)
    {
        Value = value;
    }
    operator T*() const
    {
        return Value;
    }
    T* operator->() const
    {
        return Value;
    }
    ~ObjCWrapper()
    {
        Value = nil;
    }
};

@interface ActionCallback : NSObject
- (ActionCallback*) initWithCallback: (ICsnActionCallback*) callback;
- (void) action;
@end

@implementation NSScreen (AvNSScreen)
- (CGDirectDisplayID)av_displayId
{
    return [self.deviceDescription[@"NSScreenNumber"] unsignedIntValue];
}
@end

class CsnInsidePotentialDeadlock
{
public:
    static bool IsInside();
    CsnInsidePotentialDeadlock();
    ~CsnInsidePotentialDeadlock();
};


class CsnApplicationCommands : public ComSingleObject<ICsnApplicationCommands, &IID_ICsnApplicationCommands>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT UnhideApp() override;
    virtual HRESULT HideApp() override;
    virtual HRESULT ShowAll() override;
    virtual HRESULT HideOthers() override;
};
#define NSApp [NSApplication sharedApplication]

#define START_COM_ARP_CALL START_ARP_CALL; START_COM_CALL

#endif
