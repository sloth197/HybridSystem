#include "hardware/Barcode.h"
#include "hardware/Camera.h"
#include "firmware/PLC.h"
#include "inspection/HybridLogic.h"
#include <cstdlib>
#include <ctime>
#include <iomanip>
#include <iostream>
#include <string>

float runInference(const std::string& imagePath)
{
    const bool imageLooksDefective =
        imagePath.find("/fail/") != std::string::npos ||
        imagePath.find("\\fail\\") != std::string::npos ||
        imagePath.find("fail") != std::string::npos;

    const float random01 = static_cast<float>(std::rand()) / static_cast<float>(RAND_MAX);
    if (imageLooksDefective)
    {
        return 0.15f + (0.35f * random01);
    }

    return 0.60f + (0.39f * random01);
}

int main()
{
    std::srand(static_cast<unsigned int>(std::time(nullptr)));

    PLC plc;
    Camera camera;
    Barcode barcode;
    HybridLogic logic;

    constexpr int kCycleCount = 10;
    int okCount = 0;
    int failCount = 0;

    std::cout << "[SIM] Hybrid virtual inspection start\n";
    for (int cycle = 1; cycle <= kCycleCount; ++cycle)
    {
        std::cout << "\n[SIM] Cycle " << cycle << '\n';
        plc.trigger();

        const std::string image = camera.captureImage();
        const std::string code = barcode.read();
        const float aiScore = runInference(image);
        const std::string result = logic.decide(aiScore, code);
        plc.setResult(result);

        std::cout << "  image   : " << image << '\n';
        std::cout << "  barcode : " << code << '\n';
        std::cout << "  aiScore : " << std::fixed << std::setprecision(2) << aiScore << '\n';
        std::cout << "  final   : " << result << '\n';

        if (result == "OK")
        {
            ++okCount;
        }
        else
        {
            ++failCount;
        }
    }

    std::cout << "\n[SIM] Summary - OK: " << okCount << ", FAIL: " << failCount << '\n';
    return 0;
}
