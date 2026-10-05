#include "common.h"

static int Counter = 0;
CsnInsidePotentialDeadlock::CsnInsidePotentialDeadlock()
{
    Counter++;
}

CsnInsidePotentialDeadlock::~CsnInsidePotentialDeadlock()
{
    Counter--;
}

bool CsnInsidePotentialDeadlock::IsInside()
{
    return Counter!=0;
}
