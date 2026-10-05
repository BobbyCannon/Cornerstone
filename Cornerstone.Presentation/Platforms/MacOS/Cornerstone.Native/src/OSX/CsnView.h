//
// Created by Dan Walmsley on 05/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//
#pragma once
#import <Foundation/Foundation.h>

#import <AppKit/AppKit.h>
#include "common.h"
#include "TopLevelImpl.h"
#include "KeyTransform.h"

@class CsnAccessibilityElement;
@protocol IRenderTarget;

@interface CsnView : NSView<NSTextInputClient, NSDraggingDestination, CsnTextInputMethodDelegate, CALayerDelegate>
-(CsnView* _Nonnull) initWithParent: (TopLevelImpl* _Nonnull) parent;
-(NSEvent* _Nonnull) lastMouseDownEvent;
-(CsnPoint) translateLocalPoint:(CsnPoint)pt;
-(void) onClosed;
-(void) setModifiers:(NSEventModifierFlags)modifierFlags;

-(CsnPlatformResizeReason) getResizeReason;
-(void) setResizeReason:(CsnPlatformResizeReason)reason;
-(void) setRenderTarget:(NSObject<IRenderTarget>* _Nonnull)target;
-(void) raiseAccessibilityChildrenChanged;
@end
