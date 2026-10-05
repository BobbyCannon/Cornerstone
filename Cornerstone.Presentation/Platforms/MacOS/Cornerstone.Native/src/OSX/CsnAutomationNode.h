#pragma once
#include "cornerstone-native.h"
#include "CsnAccessibility.h"

// Defines a means for managed code to raise accessibility events.
class CsnAutomationNode : public ComSingleObject<ICsnAutomationNode, &IID_ICsnAutomationNode>
{
public:
    FORWARD_IUNKNOWN()
    CsnAutomationNode(id <CsnAccessibility> owner) { _owner = owner; }
    CsnAccessibilityElement* GetOwner() { return _owner; }
    virtual void Dispose() override { _owner = nil; }
    virtual void ChildrenChanged () override { [_owner raiseChildrenChanged]; }
    virtual void PropertyChanged (CsnAutomationProperty property) override { [_owner raisePropertyChanged:property]; }
    virtual void FocusChanged () override { [_owner raiseFocusChanged]; }
private:
    __strong id <CsnAccessibility> _owner;
};
