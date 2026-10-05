//
//  CsnTextInputMethod.mm
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 23.11.22.
//  Copyright © 2022 Avalonia. All rights reserved.
//

#include "CsnTextInputMethod.h"

CsnTextInputMethod::~CsnTextInputMethod() {
    Client = nullptr;
}

CsnTextInputMethod::CsnTextInputMethod(id<CsnTextInputMethodDelegate> inputMethodDelegate) {
    _inputMethodDelegate = inputMethodDelegate;
}

bool CsnTextInputMethod::IsActive() {
    return Client != nullptr;
}

HRESULT CsnTextInputMethod::SetClient(ICsnTextInputMethodClient *client) {
    START_COM_CALL;
    
    Client = client;
    
    return S_OK;
}

void CsnTextInputMethod::Reset() {
    [_inputMethodDelegate resetInputMethod];
}

void CsnTextInputMethod::SetSurroundingText(char* text, int start, int end) {
    // stringWithUTF8String: throws on a null pointer and returns nil for invalid UTF-8.
    NSString* surroundingText = text != nullptr ? [NSString stringWithUTF8String:text] : nil;

    [_inputMethodDelegate setText:surroundingText != nil ? surroundingText : @""];
    [_inputMethodDelegate setSelection: start:end];
}

void CsnTextInputMethod::SetCursorRect(CsnRect rect) {
    [_inputMethodDelegate setCursorRect: rect];
}

void CsnTextInputMethod::SetSelectionInSurroundingText(int start, int end) {
    [_inputMethodDelegate setSelection: start:end];
}
