//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_RESIZESCOPE_H
#define AVALONIA_NATIVE_OSX_RESIZESCOPE_H

#include "cornerstone-native.h"

@class CsnView;

class ResizeScope
{
public:
    ResizeScope(CsnView* _Nonnull view, CsnPlatformResizeReason reason);

    ~ResizeScope();
private:
    CsnView* _Nonnull _view;
    CsnPlatformResizeReason _restore;
};

#endif //AVALONIA_NATIVE_OSX_RESIZESCOPE_H
