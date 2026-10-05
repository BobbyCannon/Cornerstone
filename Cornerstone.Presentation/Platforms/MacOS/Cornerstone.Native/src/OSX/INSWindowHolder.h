//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_INSWINDOWHOLDER_H
#define AVALONIA_NATIVE_OSX_INSWINDOWHOLDER_H

@class CsnView;

struct INSWindowHolder
{
    virtual NSWindow* _Nonnull GetNSWindow () = 0;
};

struct INSViewHolder
{
    virtual CsnView* _Nonnull GetNSView () = 0;
};

#endif //AVALONIA_NATIVE_OSX_INSWINDOWHOLDER_H
