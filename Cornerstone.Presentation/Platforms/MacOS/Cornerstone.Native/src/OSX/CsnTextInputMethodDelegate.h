//
//  CsnTextInputMethodHost.h
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 24.11.22.
//  Copyright © 2022 Avalonia. All rights reserved.
//

#ifndef CsnTextInputMethodHost_h
#define CsnTextInputMethodHost_h

@protocol CsnTextInputMethodDelegate
@required
-(void) setText:(NSString* _Nonnull) text;
-(void) setCursorRect:(CsnRect) cursorRect;
-(void) setSelection: (int) start : (int) end;
-(void) resetInputMethod;

@end

#endif /* CsnTextInputMethodHost_h */
