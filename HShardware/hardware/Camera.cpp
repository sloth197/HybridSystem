#include "Camera.h"
#include <cstdlib>

std::string Camera::captureImage()
{
    //captureImage: 실제로 보이는 카메라 -> 이미지 파일 경로 변경
    if (rand() % 2 == 0)
    {//정상 이미지
        return "images/ok/sample1.jpg";
    }
    else
    {//불량 이미지
        return "images/fail/sample2.jpg"
    }
}