#include "common.h"

namespace
{
    id<NSObject> s_inhibitAppSleepHandle{};
}

class PlatformBehaviorInhibition : public ComSingleObject<ICsnPlatformBehaviorInhibition, &IID_ICsnCursorFactory>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual void SetInhibitAppSleep(bool inhibitAppSleep, char* reason) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (inhibitAppSleep && s_inhibitAppSleepHandle == nullptr)
            {
                NSActivityOptions options = NSActivityUserInitiatedAllowingIdleSystemSleep;
                s_inhibitAppSleepHandle = [[NSProcessInfo processInfo] beginActivityWithOptions:options reason:[NSString stringWithUTF8String: reason]];
            }
            
            if (!inhibitAppSleep)
            {
                s_inhibitAppSleepHandle = nullptr;
            }
        }
    }
};

extern ICsnPlatformBehaviorInhibition* CreatePlatformBehaviorInhibition()
{
    @autoreleasepool
    {
        return new PlatformBehaviorInhibition();
    }
}
