#include "nonewrap.h"

#include <windows.h>
#include <limits.h>
#include <assert.h>
#include <stdio.h>

size_t __stdcall CompressBufferBound(size_t inBufferSize)
{
    return inBufferSize * 4;
}

size_t __stdcall CompressBegin(Context** pctx, int compressionLevel, void* outBuff, size_t outCapacity, void* dict, size_t dictSize)
{
    Context *ctx = (Context *)malloc(sizeof(Context));
    if (ctx == NULL) return -1;
    *pctx = ctx;
    return 0;
}

size_t __stdcall CompressUpdate(Context* ctx,void* dstBuffer, size_t dstCapacity,const void* srcBuffer, size_t srcSize)
{
    memcpy(dstBuffer, srcBuffer, srcSize);
    return srcSize;
}

size_t __stdcall CompressEnd(Context* ctx, void* dstBuffer, size_t dstCapacity)
{
    return 0;
}

void __stdcall CompressContextFree(Context* ctx)
{
    free(ctx);
}

size_t __stdcall DecompressBegin(Context **pdctx,void *inBuffer,size_t *inBufferSize, size_t *blockSize, void* dict, size_t dictSize)
{
    Context *ctx = (Context *)malloc(sizeof(Context));
    if (ctx == NULL) return -1;
    *pdctx = ctx;
    *inBufferSize = 0;
    *blockSize = 4 * 512 * 1024;
    return 0;
}

void __stdcall DecompressContextReset(Context* dctx)
{
}

size_t __stdcall DecompressUpdate(Context* dctx, void* outBuffer, size_t * outBufferSize, void* inBuffer, size_t * inBufferSize)
{
    memcpy(outBuffer, inBuffer, *inBufferSize);
    *outBufferSize = *inBufferSize;
    return 1;
}

size_t __stdcall DecompressEnd(Context* ctx)
{
    free(ctx);
    return 0;
}