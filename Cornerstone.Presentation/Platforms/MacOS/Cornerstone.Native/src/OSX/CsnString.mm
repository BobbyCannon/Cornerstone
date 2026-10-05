//
//  CsnString.m
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 07/11/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#include "common.h"
#include <vector>

class CsnStringImpl : public virtual ComSingleObject<ICsnString, &IID_ICsnString>
{
private:
    int _length;
    const char* _cstring;
    
public:
    FORWARD_IUNKNOWN()
    
    CsnStringImpl(NSString* string)
    { 
        auto cstring = [string cStringUsingEncoding:NSUTF8StringEncoding];
        _length = (int)[string lengthOfBytesUsingEncoding:NSUTF8StringEncoding];
        
        _cstring = (const char*)malloc(_length + 5);
        
        memset((void*)_cstring, 0, _length + 5);
        memcpy((void*)_cstring, (void*)cstring, _length);
    }
    
    CsnStringImpl(void*ptr, int len)
    {
        _length = len;
        _cstring = (const char*)malloc(_length);
        memcpy((void*)_cstring, ptr, len);
    }
    
    virtual ~CsnStringImpl()
    {
        free((void*)_cstring);
    }
    
    virtual HRESULT Pointer(void**retOut) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(retOut == nullptr)
            {
                return E_POINTER;
            }
            
            *retOut = (void*)_cstring;
            
            return S_OK;
        }
    }
    
    virtual HRESULT Length(int*retOut) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(retOut == nullptr)
            {
                return E_POINTER;
            }
            
            *retOut = _length;
            
            return S_OK;
        }
    }
};

class CsnStringArrayImpl : public virtual ComSingleObject<ICsnStringArray, &IID_ICsnStringArray>
{
private:
    std::vector<ComPtr<CsnStringImpl>> _list;
public:
    FORWARD_IUNKNOWN()
    CsnStringArrayImpl(NSArray<NSString*>* array)
    {
        for(int c = 0; c < [array count]; c++)
        {
            _list.push_back(comnew<CsnStringImpl>([array objectAtIndex:c]));
        }
    }
    
    CsnStringArrayImpl(NSArray<NSURL*>* array)
    {
        for(int c = 0; c < [array count]; c++)
        {
            auto s = comnew<CsnStringImpl>([array objectAtIndex:c].absoluteString);
            _list.push_back(s);
        }
    }
    
    CsnStringArrayImpl(NSString* string)
    {
        _list.push_back(comnew<CsnStringImpl>(string));
    }
    
    virtual unsigned int GetCount() override
    {
        return (unsigned int)_list.size();
    }
    
    virtual HRESULT Get(unsigned int index, ICsnString**ppv) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(_list.size() <= index)
                return E_INVALIDARG;
            *ppv = _list[index].getRetainedReference();
            return S_OK;
        }
    }
};

ICsnString* CreateCsnString(NSString* string)
{
    return new CsnStringImpl(string);
}


ICsnStringArray* CreateCsnStringArray(NSArray<NSString*> * array)
{
    return new CsnStringArrayImpl(array);
}

ICsnStringArray* CreateCsnStringArray(NSArray<NSURL*> * array)
{
    return new CsnStringArrayImpl(array);
}

ICsnStringArray* CreateCsnStringArray(NSString* string)
{
    return new CsnStringArrayImpl(string);
}

ICsnString* CreateByteArray(void* data, int len)
{
    return new CsnStringImpl(data, len);
}

NSString* GetNSStringAndRelease(ICsnString* s)
{
    NSString* result = nil;
    
    if (s != nullptr)
    {
        char* p;
        if (s->Pointer((void**)&p) == S_OK && p != nullptr)
            result = [NSString stringWithUTF8String:p];
        
        s->Release();
    }
    
    return result;
}

NSString* GetNSStringWithoutRelease(ICsnString* s)
{
    NSString* result = nil;
    
    if (s != nullptr)
    {
        char* p;
        if (s->Pointer((void**)&p) == S_OK && p != nullptr)
            result = [NSString stringWithUTF8String:p];
    }
    
    return result;
}

NSArray<NSString*>* GetNSArrayOfStringsAndRelease(ICsnStringArray* array)
{
    auto output = [NSMutableArray array];
    if (array)
    {
        ICsnString* arrayItem;
        for (int i = 0; i < array->GetCount(); i++)
        {
            if (array->Get(i, &arrayItem) == 0) {
                NSString* ext = GetNSStringAndRelease(arrayItem);
                [output addObject:ext];
            }
        }
        array->Release();
    }
    return output;
}
