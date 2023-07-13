#pragma once

#include <stddef.h>

#if defined(__cplusplus)
#define API_EXTERN_C extern "C"
#else
#define API_EXTERN_C
#endif
#ifdef NONEWRAP_EXPORTS
#define NONEAPI API_EXTERN_C __declspec(dllexport)
#else
#define NONEAPI API_EXTERN_C __declspec(dllimport)
#endif

typedef struct {
    int n;
} Context;

NONEAPI void __stdcall CompressContextFree(Context* ctx);

NONEAPI size_t __stdcall CompressBufferBound(size_t inBufferSize);

NONEAPI size_t __stdcall CompressBegin(Context** ctx, int compressionLevel, void* outBuff, size_t outCapacity, void* dict, size_t dictSize);

NONEAPI size_t __stdcall CompressUpdate(Context* ctx, void* dstBuffer, size_t dstCapacity, const void* srcBuffer, size_t srcSize);

NONEAPI size_t __stdcall CompressEnd(Context* ctx, void* dstBuffer, size_t dstCapacity);

NONEAPI size_t __stdcall DecompressBegin(Context** pdctx, void* inBuffer, size_t* inBufferSize, size_t* blockSize, void* dict, size_t dictSize);

NONEAPI void __stdcall DecompressContextReset(Context* dctx);

NONEAPI size_t __stdcall DecompressUpdate(Context* dctx, void* outBuffer, size_t* outBufferSize, void* inBuffer, size_t* inBufferSize);

NONEAPI size_t __stdcall DecompressEnd(Context* dctx);
