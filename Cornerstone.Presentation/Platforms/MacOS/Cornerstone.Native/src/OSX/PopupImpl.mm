//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#include "WindowInterfaces.h"
#include "CsnView.h"
#include "WindowImpl.h"
#include "automation.h"
#include "menu.h"
#include "common.h"
#import "WindowBaseImpl.h"
#import "WindowProtocol.h"
#import <AppKit/AppKit.h>

class PopupImpl : public virtual WindowBaseImpl, public ICsnPopup
{
private:
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(ICsnPopup, IID_ICsnPopup)
    END_INTERFACE_MAP()
    virtual ~PopupImpl(){}
    ComPtr<ICsnWindowEvents> WindowEvents;
    PopupImpl(ICsnWindowEvents* events) : TopLevelImpl(events), WindowBaseImpl(events)
    {
        WindowEvents = events;
        UpdateWindowLevel();
    }
protected:
    virtual NSWindowLevel GetBaseWindowLevel() override
    {
        return NSPopUpMenuWindowLevel;
    }

    virtual NSWindowStyleMask CalculateStyleMask() override
    {
        return NSWindowStyleMaskBorderless;
    }

public:
    virtual HRESULT Show(bool activate, bool isDialog) override
    {
        auto windowProtocol = GetWindowProtocol();
        
        [windowProtocol setEnabled:true];
        
        return WindowBaseImpl::Show(activate, true);
    }
    
    virtual HRESULT SetHitTestVisible(bool value) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            [Window setIgnoresMouseEvents:!value];
            return S_OK;
        }
    }

    virtual bool ShouldTakeFocusOnShow() override
    {
        auto parent = Parent.tryGet();
        // Don't steal the focus from another windows if our parent is inactive
        if (parent != nullptr && parent->Window != nullptr && ![parent->Window isKeyWindow])
            return false;

        return WindowBaseImpl::ShouldTakeFocusOnShow();
    }
};


extern ICsnPopup* CreateCsnPopup(ICsnWindowEvents*events)
{
    @autoreleasepool
    {
        ICsnPopup* ptr = dynamic_cast<ICsnPopup*>(new PopupImpl(events));
        return ptr;
    }
}
