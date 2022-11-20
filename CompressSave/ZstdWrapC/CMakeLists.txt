cmake_minimum_required(VERSION 3.2)

project(zstdwrap)

add_library(LZ4 SHARED
  dllmain.c zstdwrap.c zstdwrap.h)

target_compile_definitions(zstdwrap PRIVATE ZSTDWRAP_EXPORTS ZSTDLIB_STATIC_API)
target_link_libraries(zstdwrap PRIVATE zstd)
if(WIN32)
  set_target_properties(zstdwrap PROPERTIES PREFIX "")
endif()
