#pragma once
#define LZ4F_STATIC_LINKING_ONLY
#if defined(__cplusplus)
#define API_EXTERN_C extern "C"
#else
#define API_EXTERN_C
#endif
#ifdef LZ4WRAP_EXPORTS
#define LZ4API API_EXTERN_C __declspec(dllexport)
#else
#define LZ4API API_EXTERN_C __declspec(dllimport)
#endif

#include <lz4frame.h>
#include <lz4hc.h>

typedef struct
{
    LZ4F_cctx* cctx;
    LZ4F_CDict* dict;
} CContext;

typedef struct
{
    LZ4F_dctx* dctx;
    void* dict;
    size_t dictSize;
} DContext;

LZ4API void __stdcall CompressContextFree(CContext* ctx);

LZ4API size_t __stdcall CompressBufferBound(size_t inBufferSize);

LZ4API size_t __stdcall CompressBegin(CContext** ctx, int compressionLevel, void* outBuff, size_t outCapacity, void* dict, size_t dictSize);

LZ4API size_t __stdcall CompressUpdate(CContext* ctx, void* dstBuffer, size_t dstCapacity, const void* srcBuffer, size_t srcSize);

LZ4API size_t __stdcall CompressEnd(CContext* ctx, void* dstBuffer, size_t dstCapacity);

LZ4API size_t __stdcall DecompressBegin(DContext** pdctx, void* inBuffer, size_t* inBufferSize, size_t* blockSize, void* dict, size_t dictSize);

LZ4API void __stdcall DecompressContextReset(DContext* dctx);

LZ4API size_t __stdcall DecompressUpdate(DContext* dctx, void* outBuffer, size_t* outBufferSize, void* inBuffer, size_t* inBufferSize);

LZ4API size_t __stdcall DecompressEnd(DContext* dctx);
