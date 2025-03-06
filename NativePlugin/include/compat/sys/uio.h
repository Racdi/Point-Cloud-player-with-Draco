#ifndef _SYS_UIO_H
#define _SYS_UIO_H

#ifdef _WIN32

#include <stddef.h>

struct iovec {
    void* iov_base;    /* Starting address */
    size_t iov_len;    /* Number of bytes */
};

#endif /* _WIN32 */

#endif /* _SYS_UIO_H */