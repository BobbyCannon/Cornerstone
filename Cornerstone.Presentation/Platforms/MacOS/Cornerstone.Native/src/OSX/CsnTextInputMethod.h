//
//  CsnTextInputMethod.h
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 22.11.22.
//  Copyright © 2022 Avalonia. All rights reserved.
//

#ifndef CsnTextInputMethod_h
#define CsnTextInputMethod_h

#import <Foundation/Foundation.h>

#include "com.h"
#include "comimpl.h"
#include "cornerstone-native.h"
#import "CsnTextInputMethodDelegate.h"

class CsnTextInputMethod: public virtual ComObject, public virtual ICsnTextInputMethod{
private:
    id<CsnTextInputMethodDelegate> _inputMethodDelegate;
public:
    FORWARD_IUNKNOWN()
    
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(ICsnTextInputMethod, IID_ICsnTextInputMethod)
    END_INTERFACE_MAP()
    
    virtual ~CsnTextInputMethod();
    
    CsnTextInputMethod(id<CsnTextInputMethodDelegate> inputMethodDelegate);
    
    bool IsActive ();
    
    HRESULT SetClient (ICsnTextInputMethodClient* client) override;
    
    virtual void Reset () override;
    
    virtual void SetCursorRect (CsnRect rect) override;
    
    virtual void SetSurroundingText (char* text, int start, int end) override;
    
    virtual void SetSelectionInSurroundingText (int start, int end) override;
    
public:
    ComPtr<ICsnTextInputMethodClient> Client;
};
#endif /* CsnTextInputMethod_h */
