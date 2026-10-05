//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#include "ResizeScope.h"
#include "CsnView.h"

ResizeScope::ResizeScope(CsnView *view, CsnPlatformResizeReason reason) {
    _view = view;
    _restore = [view getResizeReason];
    [view setResizeReason:reason];
}

ResizeScope::~ResizeScope() {
    [_view setResizeReason:_restore];
}
