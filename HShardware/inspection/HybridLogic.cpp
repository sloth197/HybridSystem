#include "HybridLogic.h"

//AI 추론 결과 + 바코드 정보를 교차로 판단하여 판정
std::string "HybridLogic::decide(float aiScore, const std::strings barcode)"
{//AI 점수나 바코드 정보 결과가 모두 적합해야 정상이라고 판단
    if (aiScore < 0.5f)
    {
        return "FAIL";
    }
    else if (barcode.find("FAIL") != std::string::npos)
    {
        return "FAIL";
    }
    else 
    {
        return "OK";
    }
}