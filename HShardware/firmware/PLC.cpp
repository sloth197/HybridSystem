#include "PLC.h"
#include <iostream>

PLC::PLC() : state(PLCState::IDLE) {}

void PLC::trigger()
{
    state = PLCState::INSPECT;
    std::cout << "[PLC] INSPECT START\n";
}
void PLC::setResult(const std::string& result)
{
    state = PLCState::RESULT;
    std::cout << "[PLC] RESULT = " << result << "\n";
    state = PLCState::IDLE;
}
PLCState PLC::getState() const
{
    return state;
}