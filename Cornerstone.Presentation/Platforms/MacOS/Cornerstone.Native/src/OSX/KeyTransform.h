#ifndef keytransform_h
#define keytransform_h

#import <cstdint>
#include "common.h"

CsnPhysicalKey PhysicalKeyFromScanCode(uint16_t scanCode);

CsnKey VirtualKeyFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags);

NSString* KeySymbolFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags);

uint16_t MenuCharFromVirtualKey(CsnKey key);

#endif
