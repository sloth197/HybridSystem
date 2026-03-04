#include "Camera.h"
#include <array>
#include <cstdlib>

std::string Camera::captureImage() const
{
    static const std::array<const char*, 4> kSamples = {
        "images/ok/sample1.jpg",
        "images/ok/sample2.jpg",
        "images/fail/sample1.jpg",
        "images/fail/sample2.jpg",
    };

    return kSamples[std::rand() % kSamples.size()];
}
