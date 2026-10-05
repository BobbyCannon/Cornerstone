//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_WINDOWIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWIMPL_H

#import "WindowBaseImpl.h"
#include "IWindowStateChanged.h"
class WindowImpl : public virtual WindowBaseImpl, public virtual ICsnWindow, public IWindowStateChanged
{
public:
    FORWARD_IUNKNOWN()
BEGIN_INTERFACE_MAP()
        INHERIT_INTERFACE_MAP(WindowBaseImpl)
        INTERFACE_MAP_ENTRY(ICsnWindow, IID_ICsnWindow)
    END_INTERFACE_MAP()
    virtual ~WindowImpl()
    {
    }

    ComPtr<ICsnWindowEvents> WindowEvents;

    WindowImpl(ICsnWindowEvents* events);

    virtual HRESULT Show (bool activate, bool isDialog) override;

    virtual HRESULT SetEnabled (bool enable) override;

    void StartStateTransition () override ;

    void EndStateTransition () override ;

    SystemDecorations Decorations () override ;

    CsnWindowState WindowState () override ;

    void WindowStateChanged () override ;

    bool UndecoratedIsMaximized ();

    bool IsZoomed ();

    void DoZoom();

    virtual HRESULT SetCanResize(bool value) override;
    
    virtual HRESULT SetCanMinimize(bool value) override;
    
    virtual HRESULT SetCanMaximize(bool value) override;

    virtual HRESULT SetDecorations(SystemDecorations value) override;

    virtual HRESULT SetTitle (char* utf8title) override;

    virtual HRESULT SetTitleBarColor(CsnColor color) override;

    virtual HRESULT GetWindowState (CsnWindowState*ret) override;

    virtual HRESULT TakeFocusFromChildren () override;

    virtual HRESULT SetExtendClientArea (bool enable) override;

    virtual HRESULT GetExtendTitleBarHeight (double*ret) override;

    virtual HRESULT SetExtendTitleBarHeight (double value) override;
    
    virtual HRESULT GetWindowZOrder (long* zOrder) override;

    void EnterFullScreenMode ();

    void ExitFullScreenMode ();

    virtual HRESULT SetWindowState (CsnWindowState state) override;
    
    virtual HRESULT SetWindowState (CsnWindowState state, bool shouldResize);

    virtual bool IsModal() override;
    
    bool IsOwned();
    
    virtual void BringToFront () override;
    
    bool CanBecomeKeyWindow ();

    bool CanZoom() override { return _isEnabled && _canMaximize; }

    bool IsTransitioningWindowState() { return _transitioningWindowState; }

protected:
    virtual NSWindowStyleMask CalculateStyleMask() override;
    virtual void UpdateAppearance() override;

private:
    void ZOrderChildWindows();
    void OnInitialiseNSWindow();
    NSString *_lastTitle;
    bool _isEnabled;
    bool _canResize;
    bool _canMinimize;
    bool _canMaximize;
    bool _fullScreenActive;
    SystemDecorations _decorations;
    CsnWindowState _lastWindowState;
    CsnWindowState _actualWindowState;
    bool _inSetWindowState;
    NSRect _preZoomSize;
    bool _transitioningWindowState;
    bool _isClientAreaExtended;
    bool _isModal;
};

#endif //AVALONIA_NATIVE_OSX_WINDOWIMPL_H
