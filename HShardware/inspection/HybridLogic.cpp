#include "HybridLogic.h"

std::string HybridLogic::decide(float aiScore, const std::string& barcode) const
{
    if (aiScore < 0.50f)
    {
        return "FAIL";
    }

    if (barcode.find("FAIL") != std::string::npos)
    {
        return "FAIL";
    }

    return "OK";
}
