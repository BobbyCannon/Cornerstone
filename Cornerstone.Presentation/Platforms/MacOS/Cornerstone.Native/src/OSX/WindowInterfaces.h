//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include "WindowProtocol.h"
#include "WindowBaseImpl.h"
#include "CsnAccessibility.h"

@interface CsnWindow : NSWindow <CsnWindowProtocol, NSWindowDelegate, CsnAccessibility>
-(CsnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
-(CsnView* _Nullable) view;
@end

@interface CsnPanel : NSPanel <CsnWindowProtocol, NSWindowDelegate, CsnAccessibility>
-(CsnPanel* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
@end
