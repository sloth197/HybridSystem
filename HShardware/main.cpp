#include "hardware/Camera.h"
#include "hardware/Barcode.h"
#include "firmware/PLC.h"
#include "inspection/HybridLogic.h"
#include <iostream>

float runInference(const std::string& imagePath);
int main()
{//PLC 모듈 함수 생성
    PLC plc;
    Camera camera;
    Barcode barcode;
    HybridLogic logic;
    
    //검사 트리거 입력
    plc.trigger();
    //이미지 캡쳐
    std::string image = camera.captureImage();
    //물품의 바코드 읽기
    std::string code  = barcode.read();
    //AI 추론 실행
    float aiScore = runInference(image);
    //AI + 바코드 교차 판단 진행
    std::string result = logic.decide(aiScore, code);
    //PLC 결과 전송
    return 0;
}