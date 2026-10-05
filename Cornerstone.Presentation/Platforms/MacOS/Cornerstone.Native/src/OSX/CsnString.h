//
//  CsnString.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 07/11/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#ifndef CsnString_h
#define CsnString_h

extern ICsnString* CreateCsnString(NSString* string);
extern ICsnStringArray* CreateCsnStringArray(NSArray<NSString*>* array);
extern ICsnStringArray* CreateCsnStringArray(NSArray<NSURL*>* array);
extern ICsnStringArray* CreateCsnStringArray(NSString* string);
extern ICsnString* CreateByteArray(void* data, int len);
extern NSString* GetNSStringAndRelease(ICsnString* s);
extern NSString* GetNSStringWithoutRelease(ICsnString* s);
extern NSArray<NSString*>* GetNSArrayOfStringsAndRelease(ICsnStringArray* array);
#endif /* CsnString_h */
