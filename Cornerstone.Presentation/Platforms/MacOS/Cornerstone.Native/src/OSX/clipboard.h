#pragma once

#include "common.h"

@interface WriteableClipboardItem : NSObject <NSPasteboardWriting>
- (nonnull instancetype) initWithItem:(nonnull ICsnClipboardDataItem*)item source:(nonnull ICsnClipboardDataSource*)source;
@end
