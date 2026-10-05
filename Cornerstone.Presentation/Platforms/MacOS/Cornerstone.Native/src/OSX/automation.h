#pragma once

#import <Cocoa/Cocoa.h>
#include "CsnAccessibility.h"
NS_ASSUME_NONNULL_BEGIN

class ICsnAutomationPeer;

@interface CsnAccessibilityElement : NSAccessibilityElement <CsnAccessibility>
+ (id _Nullable) acquire:(ICsnAutomationPeer *) peer;
@end

NS_ASSUME_NONNULL_END
