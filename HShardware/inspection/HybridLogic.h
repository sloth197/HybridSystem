#pragma once
#include <string>
//AI 추론 함수
class HybridLogic
{
    public:
        std::string decide(float aiScore, const std::string& barcode);
};