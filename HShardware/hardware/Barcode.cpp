#include "Barcode.h"
#include <array>
#include <cstdlib>

std::string Barcode::read() const
{
    static const std::array<const char*, 5> kCodes = {
        "PRD-2026-OK",
        "PRD-2026-OK",
        "PRD-2026-OK",
        "PRD-2026-OK",
        "PRD-2026-FAIL",
    };

    return kCodes[std::rand() % kCodes.size()];
}
