#import <AppKit/AppKit.h>
#import <Metal/Metal.h>
#import <QuartzCore/QuartzCore.h>
#include "common.h"
#include "rendertarget.h"
#import "crapium.h"


class API_AVAILABLE(macos(12.0)) CsnMTLSharedEvent : public ComSingleObject<ICsnMTLSharedEvent, &IID_ICsnMTLSharedEvent>
{
    id<MTLSharedEvent> _event;
public:
    
    CsnMTLSharedEvent(id<MTLSharedEvent> ev) : _event(ev)
    {
        
    }
    
    FORWARD_IUNKNOWN()
    
    id<MTLSharedEvent> GetEvent()
    {
        return _event;
    }
    
    void *GetNativeHandle() override {
        return (__bridge void*)_event;
    }
    
    bool Wait(uint64_t value, uint64_t timeoutMS) override {
        return MtlSharedEventWaitUntilSignaledValueHack(_event, value, timeoutMS);
    }
    
    void SetSignaledValue(uint64_t value) override {
        _event.signaledValue = value;
    }
    
    uint64_t GetSignaledValue() override {
        return _event.signaledValue;
    }
};


class CsnMetalTexture : public ComSingleObject<ICsnMetalTexture, &IID_ICsnMetalTexture>
{
    id<MTLTexture> _texture;
public:
    FORWARD_IUNKNOWN()
    CsnMetalTexture(id<MTLTexture> texture) : _texture(texture)
    {
        
    }
    void *GetNativeHandle() override
    {
        return (__bridge void*)_texture;
    }
    
    int GetWidth() override
    {
        return (int)_texture.width;
    }
    
    int GetHeight() override
    {
        return (int)_texture.height;
    }
    
    int GetSampleCount() override
    {
        return (int)_texture.sampleCount;
    }
    
};

class CsnMetalDevice : public ComSingleObject<ICsnMetalDevice, &IID_ICsnMetalDevice>
{
public:
    id<MTLDevice> device;
    id<MTLCommandQueue> queue;
    FORWARD_IUNKNOWN()

    void *GetDevice() override {
        return (__bridge void*) device;
    }

    void *GetQueue() override {
        return (__bridge void*) queue;
    }
    
    HRESULT ImportIOSurface(void *handle, CsnPixelFormat pixelFormat, ICsnMetalTexture **ppv) override {
        START_COM_ARP_CALL;
        auto surf = (IOSurfaceRef)handle;
        auto width = IOSurfaceGetWidth(surf);
        auto height = IOSurfaceGetHeight(surf);

        auto desc = [MTLTextureDescriptor new];
        if(pixelFormat == kCsnRgba8888)
            desc.pixelFormat = MTLPixelFormatRGBA8Unorm;
        else if(pixelFormat == kCsnBgra8888)
            desc.pixelFormat = MTLPixelFormatBGRA8Unorm;
        else
            return E_INVALIDARG;
        desc.textureType = MTLTextureType2D;
        desc.width = width;
        desc.height = height;
        desc.depth = 1;
        desc.mipmapLevelCount = 1;
        desc.sampleCount = 1;
        desc.usage = MTLTextureUsageShaderRead | MTLTextureUsageRenderTarget;

        auto texture = [device newTextureWithDescriptor:desc iosurface:surf plane:0];
        if(texture == nullptr)
            return E_FAIL;
        *ppv = new CsnMetalTexture(texture);
        return S_OK;
    }
    
    HRESULT ImportSharedEvent(void *mtlSharedEventInstance, ICsnMTLSharedEvent**ppv) override {
        if (@available(macOS 12.0, *)) {
            auto external = (__bridge id<MTLSharedEvent>)mtlSharedEventInstance;
            auto handle = external.newSharedEventHandle;
            auto imported = [device newSharedEventWithHandle: handle];
            *ppv = new CsnMTLSharedEvent(imported);
            return S_OK;
        } 
        else
        {
            return E_NOTIMPL;
        }
    }
    
    
    HRESULT SignalOrWait(ICsnMTLSharedEvent *ev, uint64_t value, bool wait)
    {
        START_ARP_CALL;
        if (@available(macOS 12.0, *))
        {
            auto e = dynamic_cast<CsnMTLSharedEvent*>(ev);
            if(e == nullptr)
                return E_FAIL;
            auto buf = [queue commandBuffer];
            if(wait)
                [buf encodeWaitForEvent:e->GetEvent() value:value];
            else
                [buf encodeSignalEvent:e->GetEvent() value:value];
            [buf commit];
            return S_OK;
        }
        else
            return E_FAIL;
    }
    
    HRESULT SubmitWait(ICsnMTLSharedEvent *ev, uint64_t value) override {
        return SignalOrWait(ev, value, true);
    }
    
    HRESULT SubmitSignal(ICsnMTLSharedEvent *ev, uint64_t value) override { 
        return SignalOrWait(ev, value, false);
    }
    
    bool GetIOKitRegistryId(uint64_t *value) override { 
        if (@available(macOS 10.13, *)) {
            *value = [device registryID];
            return true;
        } else {
            return false;
        }
    }
    
    CsnMetalDevice(id <MTLDevice> device, id <MTLCommandQueue> queue) : device(device), queue(queue) {
    }

};


class CsnMetalRenderSession : public ComSingleObject<ICsnMetalRenderingSession, &IID_ICsnMetalRenderingSession>
{
    id<CAMetalDrawable> _drawable;
    id<MTLCommandQueue> _queue;
    id<MTLTexture> _texture;
    CAMetalLayer* _layer;
    CsnPixelSize _size;
    double _scaling;
    bool _presentWithTransaction;
public:
    FORWARD_IUNKNOWN()

    CsnMetalRenderSession(CsnMetalDevice* device, CAMetalLayer* layer, id <CAMetalDrawable> drawable, const CsnPixelSize &size, double scaling, bool presentWithTransaction)
            : _drawable(drawable), _size(size), _scaling(scaling), _queue(device->queue),
            _texture([drawable texture]), _presentWithTransaction(presentWithTransaction) {
        _layer = layer;
    }

    HRESULT GetPixelSize(CsnPixelSize *ret) override {
        *ret = _size;
        return 0;
    }

    double GetScaling() override {
        return _scaling;
    }

    void *GetTexture() override {
        return (__bridge void*) _texture;
    }

    ~CsnMetalRenderSession()
    {
        START_ARP_CALL;
        auto buffer = [_queue commandBuffer];
        if(_presentWithTransaction)
        {
            [buffer commit];
            [buffer waitUntilScheduled];
            [_drawable present];
            // Restore the default asynchronous presentation for the off-thread render loop.
            _layer.presentsWithTransaction = NO;
        }
        else
        {
            [buffer presentDrawable: _drawable];
            [buffer commit];
        }
    }
};

class CsnMetalRenderTarget : public ComSingleObject<ICsnMetalRenderTarget, &IID_ICsnMetalRenderTarget>
{
    CAMetalLayer* _layer;
    double _scaling = 1;
    CsnPixelSize _size = {1,1};
    ComPtr<CsnMetalDevice> _device;
public:
    double PendingScaling = 1;
    CsnPixelSize PendingSize = {1,1};
    FORWARD_IUNKNOWN()
    CsnMetalRenderTarget(CAMetalLayer* layer, ComPtr<CsnMetalDevice> device)
    {
        _layer = layer;
        _device = device;
    }

    HRESULT BeginDrawing(ICsnMetalRenderingSession **ret) override {
        START_COM_ARP_CALL;
        bool onMainThread = [NSThread isMainThread];
        if(onMainThread)
        {
            // Flush all existing rendering
            auto buffer = [_device->queue commandBuffer];
            [buffer commit];
            [buffer waitUntilCompleted];
            _size = PendingSize;
            _scaling= PendingScaling;
            CGSize layerSize = {(CGFloat)_size.Width, (CGFloat)_size.Height};

            [CATransaction begin];
            [CATransaction setDisableActions:YES];
            [_layer setDrawableSize: layerSize];
            _layer.presentsWithTransaction = YES;
            [CATransaction commit];
        }
        auto drawable = [_layer nextDrawable];
        if(drawable == nil)
        {
            if(onMainThread)
                _layer.presentsWithTransaction = NO;
            *ret = nullptr;
            return E_FAIL;
        }
        *ret = new CsnMetalRenderSession(_device, _layer, drawable, _size, _scaling, onMainThread);
        return 0;
    }
};

@implementation MetalRenderTarget
{
    ComPtr<CsnMetalDevice> _device;
    CAMetalLayer* _layer;
    ComPtr<CsnMetalRenderTarget> _target;
}
- (MetalRenderTarget *)initWithDevice:(ICsnMetalDevice *)device {
    _device = dynamic_cast<CsnMetalDevice*>(device);
    _layer = [CAMetalLayer new];
    _layer.opaque = false;
    _layer.device = _device->device;
    _target.setNoAddRef(new CsnMetalRenderTarget(_layer, _device));
    return self;
}


-(void) getRenderTarget: (ICsnMetalRenderTarget**) ppv
{
    *ppv = static_cast<ICsnMetalRenderTarget*>(_target.getRetainedReference());
}

- (void)resize:(CsnPixelSize)size withScale:(float)scale {
    CGSize layerSize = {(CGFloat)size.Width, (CGFloat)size.Height};
    _target->PendingScaling = scale;
    _target->PendingSize = size;
    [_layer setNeedsDisplay];
}

- (CALayer *)layer {
    return _layer;
}
@end


class CsnMetalDisplay : public ComSingleObject<ICsnMetalDisplay, &IID_ICsnMetalDisplay>
{
public:
    FORWARD_IUNKNOWN()
    HRESULT CreateDevice(ICsnMetalDevice **ret) override {
        START_COM_ARP_CALL;
        auto device = MTLCreateSystemDefaultDevice();
        if(device == nil) {
            ret = nil;
            return E_FAIL;
        }
        auto queue = [device newCommandQueue];
        *ret = new CsnMetalDevice(device, queue);
        return S_OK;
    }
};

static ComStaticPtr<CsnMetalDisplay> _display(comnew<CsnMetalDisplay>());

extern ICsnMetalDisplay* GetMetalDisplay()
{
    return _display;
}


extern ICsnMTLSharedEvent* ImportMTLSharedEvent(void* object)
{
    if (@available(macOS 12.0, *)) {
    if(object == nullptr)
        return nil;
    auto evId = (__bridge id<MTLSharedEvent>)object;
    
    if(evId == nil)
        return nil;
    
    
    return new CsnMTLSharedEvent(evId);
    } 
    else
    {
        return nil;
    }
}
