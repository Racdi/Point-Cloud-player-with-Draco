#include "LSQuicWrapper.h"

UNITY_EXPORT int LSQuic_TestInit() {
    // Try to initialize LSQUIC
    int result = lsquic_global_init(LSQUIC_GLOBAL_CLIENT);
    return result; // Returns 0 on success
}