#include "common.h"
#include "trayicon.h"
#include "menu.h"

extern ICsnTrayIcon* CreateTrayIcon()
{
    @autoreleasepool
    {
        return new CsnTrayIcon();
    }
}

CsnTrayIcon::CsnTrayIcon()
{
    _native = [[NSStatusBar systemStatusBar] statusItemWithLength: NSSquareStatusItemLength];
    
}

CsnTrayIcon::~CsnTrayIcon()
{
    if(_native != nullptr)
    {
        [[_native statusBar] removeStatusItem:_native];
        _native = nullptr;
    }
}

HRESULT CsnTrayIcon::SetIcon (void* data, size_t length)
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
            [image setTemplate: _isTemplateIcon];
            [_native setImage:image];
        }
        else
        {
            [_native setImage:nullptr];
        }
        return S_OK;
    }
}

HRESULT CsnTrayIcon::SetMenu (ICsnMenu* menu)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        auto appMenu = dynamic_cast<CsnAppMenu*>(menu);
        
        if(appMenu != nullptr)
        {
            [_native setMenu:appMenu->GetNative()];	
        }
    }
    
    return  S_OK;
}

HRESULT CsnTrayIcon::SetIsVisible(bool isVisible)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [_native setVisible:isVisible];
    }
    
    return  S_OK;
}

HRESULT CsnTrayIcon::SetToolTipText(char* text)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (text != nullptr)
        {
            [[_native button] setToolTip:[NSString stringWithUTF8String:(const char*)text]];
        }
    }
    
    return  S_OK;
}

HRESULT CsnTrayIcon::SetIsTemplateIcon(bool isTemplateIcon)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (_isTemplateIcon != isTemplateIcon)
        {
            _isTemplateIcon = isTemplateIcon;

            NSImage *image = [_native image];
            if (image)
            {
                [image setTemplate: _isTemplateIcon];
            }
        }
    }
    
    return  S_OK;
}