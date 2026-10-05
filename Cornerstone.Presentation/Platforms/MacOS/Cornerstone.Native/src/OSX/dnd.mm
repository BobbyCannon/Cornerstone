#include "common.h"

extern CsnDragDropEffects ConvertDragDropEffects(NSDragOperation nsop)
{
    int effects = 0;
    if((nsop & NSDragOperationCopy) != 0)
        effects |= (int)CsnDragDropEffects::Copy;
    if((nsop & NSDragOperationMove) != 0)
        effects |= (int)CsnDragDropEffects::Move;
    if((nsop & NSDragOperationLink) != 0)
        effects |= (int)CsnDragDropEffects::Link;
    return (CsnDragDropEffects)effects;
};

extern NSString* GetCsnCustomDataType()
{
    static NSString* result = nil;
    
    if (result == nil)
    {
        const size_t bufferSize = 256;
        char buffer[bufferSize];
        snprintf(buffer, bufferSize, "net.cornerstone.inproc.uti.n%in", getpid());
        result = [NSString stringWithUTF8String:buffer];
    }
    
    return result;
}

@interface CsnDndSource : NSObject<NSDraggingSource>

@end

@implementation CsnDndSource
{
    NSDragOperation _operation;
    ComPtr<ICsnDndResultCallback> _cb;
    void* _sourceHandle;
};

- (NSDragOperation)draggingSession:(nonnull NSDraggingSession *)session sourceOperationMaskForDraggingContext:(NSDraggingContext)context
{
    return _operation;
}

- (CsnDndSource*) initWithOperation: (NSDragOperation)operation
                        andCallback: (ICsnDndResultCallback*) cb
                    andSourceHandle: (void*) handle
{
    self = [super init];
    _operation = operation;
    _cb = cb;
    _sourceHandle = handle;
    return self;
}

- (void)draggingSession:(NSDraggingSession *)session
           endedAtPoint:(NSPoint)screenPoint
              operation:(NSDragOperation)operation
{
    if(_cb != nil)
    {
        auto cb = _cb;
        _cb = nil;
        cb->OnDragAndDropComplete(ConvertDragDropEffects(operation));
    }
    if(_sourceHandle != nil)
    {
        FreeCsnGCHandle(_sourceHandle);
        _sourceHandle = nil;
    }
}

- (void*) gcHandle
{
    return _sourceHandle;
}

@end

extern NSObject<NSDraggingSource>* CreateDraggingSource(NSDragOperation op, ICsnDndResultCallback* cb, void* handle)
{
    return [[CsnDndSource alloc] initWithOperation:op andCallback:cb andSourceHandle:handle];
};

extern void* GetCsnDataObjectHandleFromDraggingInfo(NSObject<NSDraggingInfo>* info)
{
    id obj = [info draggingSource];
    if(obj == nil)
        return nil;
    if([obj isKindOfClass: [CsnDndSource class]])
    {
        auto src = (CsnDndSource*)obj;
        return [src gcHandle];
    }
    return nil;
}
