#pragma once

#include "State.h"
#include <string>

class PLC
{
public:
    PLC();
    void trigger();
    void setResult(const std::string& result);
    PLCState getState() const;

private:
    PLCState currentState;
};
