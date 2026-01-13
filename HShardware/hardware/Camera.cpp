#include "Camera.h"
#include <cstdlib>

std::string Camera::captureImage()
{
    //실제로 보이는 카메라 -> 이미지 파일 경로 변경
    if (rand() % 2 == 0)
    {
        return "images/ok/sample1.jpg";
    }
    else
    {
        return "images/fail/sample2.jpg"
    }
}