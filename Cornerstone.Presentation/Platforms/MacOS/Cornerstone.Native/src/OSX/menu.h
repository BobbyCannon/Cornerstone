//
//  menu.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 01/08/2019.
//  Copyright © 2019 Avalonia. All rights reserved.
//

#ifndef menu_h
#define menu_h

#include "common.h"

class CsnAppMenuItem;
class CsnAppMenu;

@interface CsnMenu : NSMenu
- (id) initWithDelegate: (NSObject<NSMenuDelegate>*) del;
- (void) setHasGlobalMenuItem: (bool) value;
- (bool) hasGlobalMenuItem;
@end

@interface CsnMenuItem : NSMenuItem
- (id) initWithCsnAppMenuItem: (CsnAppMenuItem*)menuItem;
- (void)didSelectItem:(id)sender;
@end

class CsnAppMenuItem : public ComSingleObject<ICsnMenuItem, &IID_ICsnMenuItem>
{
private:
    NSMenuItem* _native; // here we hold a pointer to an CsnMenuItem
    ComPtr<ICsnActionCallback> _callback;
    ComPtr<ICsnPredicateCallback> _predicate;
    bool _isCheckable;
    
public:
    FORWARD_IUNKNOWN()
    
    CsnAppMenuItem(bool isSeparator);
    
    NSMenuItem* GetNative();
    
    virtual HRESULT SetSubMenu (ICsnMenu* menu) override;
    
    virtual HRESULT SetTitle (char* utf8String) override;

    virtual HRESULT SetToolTip (char* utf8String) override;

    virtual HRESULT SetGesture (CsnKey key, CsnInputModifiers modifiers) override;
    
    virtual HRESULT SetAction (ICsnPredicateCallback* predicate, ICsnActionCallback* callback) override;
    
    virtual HRESULT SetIsChecked (bool isChecked) override;

    virtual HRESULT SetIsVisible (bool isVisible) override;
        
    virtual HRESULT SetToggleType (CsnMenuItemToggleType toggleType) override;
    
    virtual HRESULT SetIcon (void* data, size_t length) override;
    
    bool EvaluateItemEnabled();
    
    void RaiseOnClicked();
};

class CsnAppMenu;

@interface CsnMenuDelegate : NSObject<NSMenuDelegate>
- (id) initWithParent: (CsnAppMenu*) parent;
- (void) parentDestroyed;
@end


class CsnAppMenu : public ComSingleObject<ICsnMenu, &IID_ICsnMenu>
{
private:
    CsnMenu* _native;
    ComPtr<ICsnMenuEvents> _baseEvents;
    CsnMenuDelegate* _delegate;
    
public:
    FORWARD_IUNKNOWN()
    
    CsnAppMenu(ICsnMenuEvents* events);

    CsnMenu* GetNative();
    
    void RaiseNeedsUpdate ();
    void RaiseOpening();
    void RaiseClosed();
    
    virtual HRESULT InsertItem (int index, ICsnMenuItem* item) override;
    
    virtual HRESULT RemoveItem (ICsnMenuItem* item) override;
    
    virtual HRESULT SetTitle (char* utf8String) override;
    
    virtual HRESULT Clear () override;
    virtual ~CsnAppMenu() override;
};



#endif

