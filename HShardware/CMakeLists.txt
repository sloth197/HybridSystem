cmake_minimum_required(VERSION 3.16)
project(HybridHardwareSim LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_executable(hybrid_hw_sim
    main.cpp
    hardware/Camera.cpp
    hardware/Barcode.cpp
    firmware/PLC.cpp
    inspection/HybridLogic.cpp
)

target_include_directories(hybrid_hw_sim PRIVATE .)
