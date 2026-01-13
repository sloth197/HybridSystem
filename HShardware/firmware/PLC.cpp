#include "PLC.h"
#include <iostream>

//시작 상태 IDLE 고정
PLC::PLC() : state(PLCState::IDLE) {}

//검사 시작
void PLC::trigger()
{
    state = PLCState::INSPECT;
    std::cout << "[PLC] INSPECT START\n";
}

//검사 결과
void PLC::setResult(const std::string& result)
{
    state = PLCState::RESULT;
    std::cout << "[PLC] RESULT = " << result << "\n";
    state = PLCState::IDLE;
}
//대기 상태 복귀
PLCState PLC::getState() const
{
    return state;
}