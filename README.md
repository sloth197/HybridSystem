# HybridSystem
 산업/제조 환경에서 사용가능한 이미지 인식과 바코드 인식으로 인한 불량을 구분하는 하이브리드 자동 분류 시스템

 AI 기반의 이미지 분류와 Barcode/QCR 인식을 함께 사용하여 단일 방식보다 정확하고 오류를 줄이는 결과를 도출

 ## Architecture
 
 ### 1. Image Acquisition Layer
 • 산업용 카메라 (Area / Line Scan)
 • 조명 제어기  
 • PLC 신호(트리거, 동작 상태)
 • 이미지 캡처 드라이버 (OpenCV / Vendor SDK)

 ▼ 이미지 RAW 데이터 전송
──────────────────────────────
 ### 2. Edge Preprocessing Layer
 • 이미지 Normalize / Noise 제거  
 • ROI 설정 (작업 라인 별 Zone)  
 • 배경 제거(Threshold / Morphology)  
 • 윤곽(Contour) 감지 → 객체 영역 분리  
 • 전처리 결과 캐시 저장

 ▼ AI 모델 입력용 224×224 / 320×320 변환
──────────────────────────────  
### 3. AI Classification Layer
 • Pretrained Model (ONNX Runtime 기반)  
      - EfficientNet-B0  
      - MobileNetV3 Large  
      - ResNet50 (옵션)
 • 분류 목적:  
      - 정상(OK)  
      - 불량 타입 B1  
      - 불량 타입 B2  
      - 불량 타입 B3  
      - 기타 불량  
 • Confidence Score 출력 (Softmax 기반)

 ▼ Hybrid Logic으로 결과 전송
──────────────────────────────
 ### 4. Hybrid Decision Logic Layer
 • AI 결과 + Rule 기반 검사 조합  
      - 치수 검사(픽셀 기반)  
      - ROI 영역 누락 여부  
      - 특정 패턴 미검출 확인  
 • AI Confidence Threshold 검사  
      - 예: 85% 미만이면 “AI 불확실”  
 • AI + Rule 결과 통합 판단  
      - 최종: OK / NG  
      - NG → 불량 타입 B# 지정  
 • 예외 처리  
      - 이미지 불량  
      - 초점, 조도 문제  
      - 카메라 신호 오류

 ▼ MES/Server 전달용 최종 구조체 생성
──────────────────────────────  
### 5. Output Layer
 • 검사 결과: OK / NG  
 • 불량 타입(B1/B2/B3 등)  
 • Confidence Score  
 • 검사 시간(ms)  
 • 원본/전처리/결과 이미지 스냅샷  
 • 로그 저장(JSON / DB)

 ▼ MES 또는 Dashboard 서버 전송
──────────────────────────────  
### 6. MES / Server Integration
 • REST API / MQTT / TCP Socket  
 • 결과 DB 저장 (MySQL / AWS RDS)  
 • 이미지 파일 저장 (NAS / S3)  
 • 관리자 Dashboard UI  
 • 통계/리포트 생성  
