cmake_minimum_required(VERSION 3.10)
project(LSQuicUnityPlugin)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Set vcpkg toolchain
set(CMAKE_TOOLCHAIN_FILE "C:/Users/a2655/Documents/vcpkg/scripts/buildsystems/vcpkg.cmake")
set(VCPKG_TARGET_TRIPLET "x64-windows-static")

# Set output directories
if(WIN32)
    set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
    set(OUTPUT_DIR "${CMAKE_SOURCE_DIR}/../Assets/Plugins/x86_64")
else()
    set(OUTPUT_DIR "${CMAKE_SOURCE_DIR}/../Assets/Plugins/Linux/x86_64")
endif()

file(MAKE_DIRECTORY ${OUTPUT_DIR})

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${OUTPUT_DIR})
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${OUTPUT_DIR})

# Add the library
add_library(LSQuicUnityPlugin SHARED
    src/LSQuicWrapper.cpp
)

# Include directories
target_include_directories(LSQuicUnityPlugin PRIVATE
    ${CMAKE_SOURCE_DIR}/include
    ${CMAKE_SOURCE_DIR}/include/lsquic
    ${CMAKE_SOURCE_DIR}/include/compat
    ${CMAKE_SOURCE_DIR}/include/wincompat
    "C:/Users/a2655/Documents/boringssl/include"
    "C:/Users/a2655/Documents/vcpkg/installed/x64-windows-static/include"
)

# Add compile definitions
target_compile_definitions(LSQuicUnityPlugin PRIVATE
    WIN32_LEAN_AND_MEAN
    _CRT_SECURE_NO_WARNINGS
    ZLIB_WINAPI
)

# Link with libraries
if(WIN32)
    target_link_libraries(LSQuicUnityPlugin PRIVATE
        # Use Debug configuration for lsquic
        "C:/Users/a2655/Documents/lsquic/build/src/liblsquic/Debug/lsquic.lib"
        
        # Use the correct paths for BoringSSL
        "C:/Users/a2655/Documents/boringssl/build/ssl/ssl.lib"
        "C:/Users/a2655/Documents/boringssl/build/crypto/crypto.lib"
        
        # Use Debug configuration for zlib
        "C:/Users/a2655/Documents/vcpkg/installed/x64-windows-static/debug/lib/zlibd.lib"
        
        # Windows system libraries
        ws2_32.lib
    )
endif()