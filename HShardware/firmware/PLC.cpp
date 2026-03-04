#include "PLC.h"
#include <iostream>

PLC::PLC() : currentState(PLCState::IDLE) {}

void PLC::trigger()
{
    currentState = PLCState::INSPECT;
    std::cout << "[PLC] INSPECT START\n";
}

void PLC::setResult(const std::string& result)
{
    currentState = PLCState::RESULT;
    std::cout << "[PLC] RESULT = " << result << '\n';
    currentState = PLCState::IDLE;
}

PLCState PLC::getState() const
{
    return currentState;
}
