//
//  TopLevelImpl.h
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 16.05.24.
//  Copyright © 2024 Avalonia. All rights reserved.
//

#ifndef TopLevelImpl_h
#define TopLevelImpl_h

#include "rendertarget.h"
#include "INSWindowHolder.h"
#include "CsnTextInputMethod.h"
#include "AutoFitContentView.h"
#include <list>

class TopLevelImpl : public virtual ComObject,
                     public virtual ICsnTopLevel,
                     public INSViewHolder{
    
public:
    FORWARD_IUNKNOWN()
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(ICsnTopLevel, IID_ICsnTopLevel)
    END_INTERFACE_MAP()
    
    virtual ~TopLevelImpl();
    
    TopLevelImpl(ICsnTopLevelEvents* events);
                         
    virtual CsnView *GetNSView() override;
                         
    virtual HRESULT SetCursor(ICsnCursor* cursor) override;
                         
    virtual HRESULT GetScaling(double*ret) override;
                         
    virtual HRESULT GetClientSize(CsnSize *ret) override;
                           
    virtual HRESULT GetInputMethod(ICsnTextInputMethod **ppv) override;
                           
    virtual HRESULT ObtainNSViewHandle(void** retOut) override;
                                                  
    virtual HRESULT ObtainNSViewHandleRetained(void** retOut) override;
                           
    virtual HRESULT CreateSoftwareRenderTarget(ICsnSoftwareRenderTarget** ret) override;
                                                  
    virtual HRESULT CreateMetalRenderTarget(ICsnMetalDevice* device, ICsnMetalRenderTarget** ret) override;
                           
    virtual HRESULT CreateGlRenderTarget(ICsnGlContext* context, ICsnGlSurfaceRenderTarget** ret) override;

    virtual HRESULT CreateNativeControlHost(ICsnNativeControlHost **retOut) override;
                         
    virtual HRESULT Invalidate() override;
                         
    virtual HRESULT PointToClient(CsnPoint point, CsnPoint *ret) override;

    virtual HRESULT PointToScreen(CsnPoint point, CsnPoint *ret) override;
     
    virtual HRESULT SetTransparencyMode(CsnWindowTransparencyMode mode) override;

    virtual HRESULT GetCurrentDisplayId (CGDirectDisplayID* ret) override;

    virtual HRESULT BeginDragAndDropOperation(
        CsnDragDropEffects effects,
        CsnPoint point,
        ICsnClipboardDataSource* source,
        ICsnDndResultCallback* callback,
        void* sourceHandle) override;

protected:
    NSCursor *cursor;
    virtual void UpdateAppearance();
                           
public:
    NSObject<IRenderTarget> *currentRenderTarget;
    ComPtr<CsnTextInputMethod> InputMethod;
    ComPtr<ICsnTopLevelEvents> TopLevelEvents;
    CsnView *View;
                         
    void UpdateCursor();
    virtual void SetClientSize(NSSize size);
};

#endif /* TopLevelImpl_h */
