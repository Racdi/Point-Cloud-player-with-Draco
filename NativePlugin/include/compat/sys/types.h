#ifndef _SYS_TYPES_H
#define _SYS_TYPES_H

#ifdef _WIN32

#include <stddef.h>

#ifdef _WIN64
typedef __int64 ssize_t;
#else
typedef int ssize_t;
#endif

#endif /* _WIN32 */

#endif /* _SYS_TYPES_H */