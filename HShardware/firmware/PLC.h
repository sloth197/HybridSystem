#pragma once
#include "State.h"
#include <string>

class PLC
{
    private:
        PLCState currentState;
    public:
        PLC();
        void trigger();
        void setResult(const std::string& result);
        PLCState getState() const;
};