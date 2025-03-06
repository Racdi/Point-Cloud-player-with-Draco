#pragma once

// First include Windows networking
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <winsock2.h>
#include <ws2tcpip.h>

#ifdef _WIN32
    #define UNITY_EXPORT __declspec(dllexport)
#else
    #define UNITY_EXPORT __attribute__((visibility("default")))
#endif

// Now include lsquic headers
#include "lsquic/lsquic.h"
#include "lsquic/lsquic_types.h"

#ifdef __cplusplus
extern "C" {
#endif

// Simple test function to verify LSQUIC initialization
UNITY_EXPORT int LSQuic_TestInit();

#ifdef __cplusplus
}
#endif