#pragma once
#import <Cocoa/Cocoa.h>
#import "cornerstone-native.h"

// Defines the interface between CsnAutomationNode and objects which implement
// NSAccessibility such as CsnAccessibilityElement or CsnWindow.
@protocol CsnAccessibility <NSAccessibility>
@required
- (void) raiseChildrenChanged;
- (void) raiseFocusChanged;
- (void) raisePropertyChanged:(CsnAutomationProperty)property;
@end
