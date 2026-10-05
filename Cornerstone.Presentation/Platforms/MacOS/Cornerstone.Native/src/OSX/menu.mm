

#include "common.h"
#include "menu.h"
#include "KeyTransform.h"
#include <CoreFoundation/CoreFoundation.h>
#include <Carbon/Carbon.h> /* For kVK_ constants, and TIS functions. */

@implementation CsnMenu
{
    bool _isReparented;
    NSObject<NSMenuDelegate>* _wtf;
}

- (id) initWithDelegate: (NSObject<NSMenuDelegate>*)del
{
    self = [super init];
    self.delegate = del;
    _wtf = del;
    _isReparented = false;
    return self;
}

- (bool)hasGlobalMenuItem
{
    return _isReparented;
}

- (void)setHasGlobalMenuItem:(bool)value
{
    _isReparented = value;
}

@end

@implementation CsnMenuItem
{
    ComObjectWeakPtr<CsnAppMenuItem> _item;
}

- (id) initWithCsnAppMenuItem: (CsnAppMenuItem*)menuItem
{
    if(self != nil)
    {
        _item = menuItem;
        self = [super initWithTitle:@""
                             action:@selector(didSelectItem:)
                      keyEquivalent:@""];
        
        [self setEnabled:YES];
        
        [self setTarget:self];
    }
    
    return self;
}

- (BOOL)validateMenuItem:(NSMenuItem *)menuItem
{
    if([self submenu] != nil)
    {
        return YES;
    }
    auto item = _item.tryGet();
    if(item == nullptr)
        return NO;
    
    return item->EvaluateItemEnabled();
}

- (void)didSelectItem:(nullable id)sender
{
    auto item = _item.tryGet();
    if(item == nullptr)
        return;
    item->RaiseOnClicked();
}
@end

CsnAppMenuItem::CsnAppMenuItem(bool isSeparator)
{
    _isCheckable = false;

    if(isSeparator)
    {
        _native = [NSMenuItem separatorItem];
    }
    else
    {
        _native = [[CsnMenuItem alloc] initWithCsnAppMenuItem: this];
    }
    
    _callback = nullptr;
}

NSMenuItem* CsnAppMenuItem::GetNative()
{
    return _native;
}

HRESULT CsnAppMenuItem::SetSubMenu (ICsnMenu* menu)
{
    START_COM_CALL;

    @autoreleasepool
    {
        if(menu != nullptr)
        {
            auto nsMenu = dynamic_cast<CsnAppMenu*>(menu)->GetNative();

            [_native setSubmenu: nsMenu];

            // Parity with -[NSMenu setSubmenu:forItem:]: without submenuAction: a click is dispatched to
            // didSelectItem: and dismisses the menu instead of opening the submenu.
            [_native setTarget: nil];
            [_native setAction: @selector(submenuAction:)];
        }
        else
        {
            [_native setSubmenu: nullptr];

            // The item is reused, so put its own action back.
            [_native setTarget: _native];
            [_native setAction: @selector(didSelectItem:)];
        }

        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetTitle (char* utf8String)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (utf8String != nullptr)
        {
            [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        }
        
        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetToolTip (char* utf8String)
{
    START_COM_CALL;

    @autoreleasepool
    {
        if (utf8String != nullptr)
        {
            [_native setToolTip:[NSString stringWithUTF8String:(const char*)utf8String]];
        }

        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetGesture (CsnKey key, CsnInputModifiers modifiers)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if(key != CsnKeyNone)
        {
            NSEventModifierFlags flags = 0;
            
            if (modifiers & Control)
                flags |= NSEventModifierFlagControl;
            if (modifiers & Shift)
                flags |= NSEventModifierFlagShift;
            if (modifiers & Alt)
                flags |= NSEventModifierFlagOption;
            if (modifiers & Windows)
                flags |= NSEventModifierFlagCommand;
            
            auto menuChar = MenuCharFromVirtualKey(key);
            
            if (menuChar != 0)
            {
                auto keyString = [NSString stringWithCharacters:&menuChar length:1];
                
                [_native setKeyEquivalent: keyString];
                [_native setKeyEquivalentModifierMask:flags];
                
                return S_OK;
            }
        }
        
        // Nothing matched... clear.
        [_native setKeyEquivalent: @""];
        [_native setKeyEquivalentModifierMask: 0];
        
        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetAction (ICsnPredicateCallback* predicate, ICsnActionCallback* callback)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        _predicate = predicate;
        _callback = callback;
        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetIsChecked (bool isChecked)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [_native setState:(isChecked && _isCheckable ? NSOnState : NSOffState)];
        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetIsVisible (bool isVisible)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [_native setHidden:!isVisible];
        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetToggleType(CsnMenuItemToggleType toggleType)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        switch(toggleType)
        {
            case CsnMenuItemToggleType::None:
                [_native setOnStateImage: [NSImage imageNamed:@"NSMenuCheckmark"]];
                
                _isCheckable = false;
                break;
                
            case CsnMenuItemToggleType::CheckMark:
                [_native setOnStateImage: [NSImage imageNamed:@"NSMenuCheckmark"]];
                
                _isCheckable = true;
                break;
                
            case CsnMenuItemToggleType::Radio:
                [_native setOnStateImage: [NSImage imageNamed:@"NSMenuItemBullet"]];
                
                _isCheckable = true;
                break;
        }
        
        return S_OK;
    }
}

HRESULT CsnAppMenuItem::SetIcon(void *data, size_t length)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if(data != nullptr)
        {
            NSData *imageData = [NSData dataWithBytes:data length:length];
            NSImage *image = [[NSImage alloc] initWithData:imageData];
            
            NSSize originalSize = [image size];
             
            NSSize size;
            size.height = floor([[NSFont menuFontOfSize:0] pointSize] * 1.333333);
            
            auto scaleFactor = size.height / originalSize.height;
            size.width = floor(originalSize.width * scaleFactor);
            
            [image setSize: size];
            [_native setImage:image];
        }
        else
        {
            [_native setImage:nullptr];
        }
        return S_OK;
    }
}

bool CsnAppMenuItem::EvaluateItemEnabled()
{
    if(_predicate != nullptr)
    {
        auto result = _predicate->Evaluate ();
        
        return result;
    }
    
    return false;
}

void CsnAppMenuItem::RaiseOnClicked()
{
    if(_callback != nullptr)
    {
        _callback->Run();
    }
}

CsnAppMenu::CsnAppMenu(ICsnMenuEvents* events)
{
    _baseEvents = events;
    _delegate = [[CsnMenuDelegate alloc] initWithParent: this];
    _native = [[CsnMenu alloc] initWithDelegate: _delegate];
}

CsnAppMenu::~CsnAppMenu()
{
    [_delegate parentDestroyed];
}


CsnMenu* CsnAppMenu::GetNative()
{
    return _native;
}

void CsnAppMenu::RaiseNeedsUpdate()
{
    if(_baseEvents != nullptr)
    {
        _baseEvents->NeedsUpdate();
    }
}

void CsnAppMenu::RaiseOpening()
{
    if(_baseEvents != nullptr)
    {
        _baseEvents->Opening();
    }
}

void CsnAppMenu::RaiseClosed()
{
    if(_baseEvents != nullptr)
    {
        _baseEvents->Closed();
    }
}


HRESULT CsnAppMenu::InsertItem(int index, ICsnMenuItem *item)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if([_native hasGlobalMenuItem])
        {
            index++;
        }
        
        auto csnMenuItem = dynamic_cast<CsnAppMenuItem*>(item);
        
        if(csnMenuItem != nullptr)
        {
            [_native insertItem: csnMenuItem->GetNative() atIndex:index];
        }
        
        return S_OK;
    }
}

HRESULT CsnAppMenu::RemoveItem (ICsnMenuItem* item)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        auto csnMenuItem = dynamic_cast<CsnAppMenuItem*>(item);
        
        if(csnMenuItem != nullptr)
        {
            [_native removeItem:csnMenuItem->GetNative()];
        }
        
        return S_OK;
    }
}

HRESULT CsnAppMenu::SetTitle (char* utf8String)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (utf8String != nullptr)
        {
            [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        }
        
        return S_OK;
    }
}

HRESULT CsnAppMenu::Clear()
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [_native removeAllItems];
        return S_OK;
    }
}

@implementation CsnMenuDelegate
{
    CsnAppMenu* _parent;
}
- (id) initWithParent:(CsnAppMenu *)parent
{
    self = [super init];
    _parent = parent;
    return self;
}

- (void) parentDestroyed
{
    _parent = nullptr;
}

- (BOOL)menu:(NSMenu *)menu updateItem:(NSMenuItem *)item atIndex:(NSInteger)index shouldCancel:(BOOL)shouldCancel
{
    if(shouldCancel)
        return NO;
    return YES;
}

- (NSInteger)numberOfItemsInMenu:(NSMenu *)menu
{
    return [menu numberOfItems];
}

- (void)menuNeedsUpdate:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseNeedsUpdate();
}

- (void)menuWillOpen:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseOpening();
}

- (void)menuDidClose:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseClosed();
}

@end

extern ICsnMenu* CreateAppMenu(ICsnMenuEvents* cb)
{
    @autoreleasepool
    {
        return new CsnAppMenu(cb);
    }
}

extern ICsnMenuItem* CreateAppMenuItem()
{
    @autoreleasepool
    {
        return new CsnAppMenuItem(false);
    }
}

extern ICsnMenuItem* CreateAppMenuItemSeparator()
{
    @autoreleasepool
    {
        return new CsnAppMenuItem(true);
    }
}

static ComStaticPtr<CsnAppMenu> s_appMenu;
static NSMenuItem* s_appMenuItem = nullptr;

extern void SetAppMenu(ICsnMenu *menu)
{
    s_appMenu.set(dynamic_cast<CsnAppMenu*>(menu));
    
    if(s_appMenu != nullptr)
    {
        auto currentMenu = [s_appMenuItem menu];
        
        if (currentMenu != nullptr)
        {
            [currentMenu removeItem:s_appMenuItem];
        }
        
        s_appMenuItem = [s_appMenu->GetNative() itemAtIndex:0];
        
        if (currentMenu == nullptr)
        {
            currentMenu = [s_appMenuItem menu];
        }
        
        [[s_appMenuItem menu] removeItem:s_appMenuItem];
        
        [currentMenu insertItem:s_appMenuItem atIndex:0];
        
        if([s_appMenuItem submenu] == nullptr)
        {
            [s_appMenuItem setSubmenu:[NSMenu new]];
        }
    }
    else
    {
        s_appMenuItem = nullptr;
    }
}

extern void SetServicesMenu (ICsnMenu* menu)
{
    auto nativeMenu = dynamic_cast<CsnAppMenu*>(menu);
    [NSApplication sharedApplication].servicesMenu = nativeMenu->GetNative();
}

extern CsnAppMenu* GetAppMenu ()
{
    return s_appMenu.getRaw();
}

extern NSMenuItem* GetAppMenuItem ()
{
    return s_appMenuItem;
}


