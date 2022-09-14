cmake_minimum_required(VERSION 3.2)

project(LZ4Wrap)

add_library(LZ4 SHARED
  lz4/lz4.c lz4/lz4.h
  lz4/lz4frame.c lz4/lz4frame.h lz4/lz4frame_static.h
  lz4/lz4hc.c lz4/lz4hc.h
  lz4/xxhash.c lz4/xxhash.h
  dllmain.c LZ4Wrap.c LZ4Wrap.h)

target_compile_definitions(LZ4 PRIVATE LZ4WRAP_EXPORTS)
target_include_directories(LZ4 PRIVATE lz4)
if(WIN32)
  set_target_properties(LZ4 PROPERTIES PREFIX "")
endif()
