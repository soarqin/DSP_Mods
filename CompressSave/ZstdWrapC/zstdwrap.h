#pragma once

#include <zstd.h>

#include <stddef.h>

#if defined(__cplusplus)
#define API_EXTERN_C extern "C"
#else
#define API_EXTERN_C
#endif
#ifdef ZSTDWRAP_EXPORTS
#define ZSTDAPI API_EXTERN_C __declspec(dllexport)
#else
#define ZSTDAPI API_EXTERN_C __declspec(dllimport)
#endif

ZSTDAPI void __stdcall CompressContextFree(ZSTD_CStream* ctx);

ZSTDAPI size_t __stdcall CompressBufferBound(size_t inBufferSize);

ZSTDAPI size_t __stdcall CompressBegin(ZSTD_CStream** ctx, int compressionLevel, void* outBuff, size_t outCapacity, void* dict, size_t dictSize);

ZSTDAPI size_t __stdcall CompressUpdate(ZSTD_CStream* ctx, void* dstBuffer, size_t dstCapacity, const void* srcBuffer, size_t srcSize);

ZSTDAPI size_t __stdcall CompressEnd(ZSTD_CStream* ctx, void* dstBuffer, size_t dstCapacity);

ZSTDAPI size_t __stdcall DecompressBegin(ZSTD_DStream** pdctx, void* inBuffer, size_t* inBufferSize, size_t* blockSize, void* dict, size_t dictSize);

ZSTDAPI void __stdcall DecompressContextReset(ZSTD_DStream* dctx);

ZSTDAPI size_t __stdcall DecompressUpdate(ZSTD_DStream* dctx, void* outBuffer, size_t* outBufferSize, void* inBuffer, size_t* inBufferSize);

ZSTDAPI size_t __stdcall DecompressEnd(ZSTD_DStream* dctx);
