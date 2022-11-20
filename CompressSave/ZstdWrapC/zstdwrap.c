#include "zstdwrap.h"

#include <windows.h>
#include <limits.h>
#include <assert.h>
#include <stdio.h>

size_t __stdcall CompressBufferBound(size_t inBufferSize)
{
    return ZSTD_COMPRESSBOUND(inBufferSize);
}

size_t __stdcall CompressBegin(ZSTD_CStream** pctx, int compressionLevel, void* outBuff, size_t outCapacity, void* dict, size_t dictSize)
{
    ZSTD_CStream *ctx = ZSTD_createCStream();
    if (ctx == NULL) return -1;
    ZSTD_CCtx_setParameter(ctx, ZSTD_c_compressionLevel, compressionLevel);
    if (dict)
    {
        ZSTD_CCtx_loadDictionary(ctx, dict, dictSize);
    }
    *pctx = ctx;
    return 0;
}


size_t __stdcall CompressUpdate(ZSTD_CStream* ctx,void* dstBuffer, size_t dstCapacity,const void* srcBuffer, size_t srcSize)
{
    ZSTD_outBuffer obuf = {dstBuffer, dstCapacity, 0};
    ZSTD_inBuffer ibuf = {srcBuffer, srcSize, 0};
    do
    {
        ZSTD_compressStream2(ctx, &obuf, &ibuf, ZSTD_e_continue);
    }
    while (ibuf.pos < ibuf.size);
    return obuf.pos;
}

size_t __stdcall CompressEnd(ZSTD_CStream* ctx, void* dstBuffer, size_t dstCapacity)
{
    ZSTD_outBuffer obuf = {dstBuffer, dstCapacity, 0};
    ZSTD_inBuffer ibuf = {NULL, 0, 0};
    while (ZSTD_compressStream2(ctx, &obuf, &ibuf, ZSTD_e_end) > 0) {}
    return obuf.pos;
}

void __stdcall CompressContextFree(ZSTD_CStream* ctx)
{
    ZSTD_freeCStream(ctx);
}

size_t __stdcall DecompressBegin(ZSTD_DStream **pdctx,void *inBuffer,size_t *inBufferSize, size_t *blockSize, void* dict, size_t dictSize)
{
    ZSTD_DStream *ctx = ZSTD_createDStream();
    if (ctx == NULL) return -1;
    if (dict)
    {
        ZSTD_DCtx_loadDictionary(ctx, dict, dictSize);
    }
    *pdctx = ctx;
    *inBufferSize = 0;
    *blockSize = ZSTD_DStreamOutSize() << 2;
    return 0;
}

void __stdcall DecompressContextReset(ZSTD_DStream* dctx)
{
    ZSTD_DCtx_reset(dctx, ZSTD_reset_session_only);
}

size_t __stdcall DecompressUpdate(ZSTD_DStream* dctx, void* outBuffer, size_t * outBufferSize, void* inBuffer, size_t * inBufferSize)
{
    ZSTD_outBuffer obuf = {outBuffer, *outBufferSize, 0};
    ZSTD_inBuffer ibuf = {inBuffer, *inBufferSize, 0};
    size_t r = ZSTD_decompressStream(dctx, &obuf, &ibuf);
    *outBufferSize = obuf.pos;
    *inBufferSize = ibuf.pos;
    return r;
}

size_t __stdcall DecompressEnd(ZSTD_DStream* ctx)
{
    return ZSTD_freeDStream(ctx);
}