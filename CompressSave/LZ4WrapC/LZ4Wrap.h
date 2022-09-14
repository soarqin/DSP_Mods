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

LZ4API void FreeCompressContext(CContext* ctx);

LZ4API size_t CalCompressOutBufferSize(size_t inBufferSize);

LZ4API size_t CompressBegin(CContext** ctx, void* outBuff, size_t outCapacity, void* dictBuffer, size_t dictSize);

LZ4API size_t CompressUpdate(CContext* ctx, void* dstBuffer, size_t dstCapacity, const void* srcBuffer, size_t srcSize);

LZ4API size_t CompressEnd(CContext* ctx, void* dstBuffer, size_t dstCapacity);

LZ4API size_t DecompressBegin(LZ4F_dctx** pdctx, void* inBuffer, size_t* inBufferSize, size_t* blockSize);

LZ4API void ResetDecompresssCTX(LZ4F_dctx* dctx);

LZ4API size_t DecompressUpdate(LZ4F_dctx* dctx, void* outBuffer, size_t* outBufferSize, void* inBuffer, size_t* inBufferSize, void* dict, size_t dictSize);

LZ4API size_t DecompressEnd(LZ4F_dctx* dctx);
