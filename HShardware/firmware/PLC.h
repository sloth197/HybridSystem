#pragma once
#include "State.h"
#include <string>

class PLC
{
    private:
        PLCState currentState;
    public:
        PLC();
        //외부 트리거 입력
        void trigger();
        //검사 결과 수신(OK, FAIL)
        void setResult(const std::string& result);
        //트리거 상태 조회
        PLCState getState() const;
};