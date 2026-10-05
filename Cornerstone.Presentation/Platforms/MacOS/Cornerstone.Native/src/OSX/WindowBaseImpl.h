//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H

#include "rendertarget.h"
#include "INSWindowHolder.h"
#include "CsnTextInputMethod.h"
#include "TopLevelImpl.h"
#include <list>

@class CsnMenu;
@protocol CsnWindowProtocol;

class WindowBaseImpl : public virtual TopLevelImpl,
                       public virtual ICsnWindowBase,
                       public INSWindowHolder {

public:
    FORWARD_IUNKNOWN()

    BEGIN_INTERFACE_MAP()
        INHERIT_INTERFACE_MAP(TopLevelImpl)
        INTERFACE_MAP_ENTRY(ICsnWindowBase, IID_ICsnWindowBase)
    END_INTERFACE_MAP()

    virtual ~WindowBaseImpl();

    WindowBaseImpl(ICsnWindowBaseEvents *events, bool usePanel = false);

    virtual HRESULT ObtainNSWindowHandle(void **ret) override;

    virtual HRESULT ObtainNSWindowHandleRetained(void **ret) override;

    virtual NSWindow *GetNSWindow() override;

    virtual HRESULT Show(bool activate, bool isDialog) override;

    virtual bool IsShown ();

    virtual bool ShouldTakeFocusOnShow();

    virtual HRESULT Hide() override;

    virtual HRESULT Activate() override;

    virtual HRESULT SetTopMost(bool value) override;

    virtual HRESULT Close() override;

    virtual HRESULT GetFrameSize(CsnSize *ret) override;

    virtual HRESULT SetMinMaxSize(CsnSize minSize, CsnSize maxSize) override;

    virtual HRESULT Resize(double x, double y, CsnPlatformResizeReason reason) override;

    virtual HRESULT SetMainMenu(ICsnMenu *menu) override;

    virtual HRESULT BeginMoveDrag() override;

    virtual HRESULT BeginResizeDrag(__attribute__((unused)) CsnWindowEdge edge) override;

    virtual HRESULT GetPosition(CsnPoint *ret) override;

    virtual HRESULT SetPosition(CsnPoint point) override;

    virtual HRESULT SetFrameThemeVariant(CsnPlatformThemeVariant variant) override;

    virtual HRESULT SetTransparencyMode(CsnWindowTransparencyMode mode) override;
                           
    virtual bool IsModal();

    id<CsnWindowProtocol> GetWindowProtocol ();
                           
    virtual void BringToFront ();

    virtual bool CanZoom() { return false; }
                           
    virtual HRESULT SetParent(ICsnWindowBase* parent) override;

    void UpdateWindowLevel();

protected:
    virtual NSWindowLevel GetBaseWindowLevel();

    virtual NSWindowStyleMask CalculateStyleMask() = 0;
    virtual void UpdateAppearance() override;
    virtual void SetClientSize(NSSize size) override;

private:
    void CreateNSWindow (bool isDialog);
    void CleanNSWindow ();

    bool hasPosition;
    NSSize lastSize;
    NSSize lastMinSize;
    NSSize lastMaxSize;
    CsnMenu* lastMenu;
    bool _inResize;

protected:
    AutoFitContentView *StandardContainer;
    CsnPoint lastPositionSet;
    bool _shown;
    bool _isTopmost;
    std::list<ComObjectWeakPtr<WindowBaseImpl>> _children;

public:
    ComObjectWeakPtr<WindowBaseImpl> Parent = nullptr;
    NSWindow * Window;
    ComPtr<ICsnWindowBaseEvents> BaseEvents;
};

#endif //AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H
