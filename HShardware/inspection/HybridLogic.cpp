#include "HybridLogic.h"

std::string "HybridLogic::decide(float aiScore, const std::strings barcode)"
{
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