#pragma once

#include "com.h"
#include "comimpl.h"
#include "cornerstone-native.h"

@protocol IRenderTarget

-(void) resize: (CsnPixelSize) size withScale: (float) scale;
-(CALayer*) layer;

@end

@interface IOSurfaceRenderTarget : NSObject<IRenderTarget>
-(IOSurfaceRenderTarget*) initWithOpenGlContext: (ICsnGlContext*) context;
-(ICsnGlSurfaceRenderTarget*) createSurfaceRenderTarget;
-(ICsnSoftwareRenderTarget*) createSoftwareRenderTarget;
-(HRESULT) setSwFrame: (CsnFramebuffer*) fb;
-(void)consumeSurfaces;
@end

@interface MetalRenderTarget : NSObject<IRenderTarget>
-(MetalRenderTarget*) initWithDevice: (ICsnMetalDevice*) device;
-(void) getRenderTarget: (ICsnMetalRenderTarget**) ppv;
@end