cmake_minimum_required(VERSION 3.2)

project(zstdwrap)

add_library(zstdwrap SHARED
  dllmain.c zstdwrap.c zstdwrap.h)

target_compile_definitions(zstdwrap PRIVATE ZSTDWRAP_EXPORTS ZSTDLIB_STATIC_API)
if(MSVC)
  target_compile_options(zstdwrap PRIVATE /MT)
else()
  target_link_options(zstdwrap PRIVATE -static)
endif()
target_link_libraries(zstdwrap PRIVATE zstd)
if(WIN32)
  set_target_properties(zstdwrap PROPERTIES PREFIX "")
endif()
