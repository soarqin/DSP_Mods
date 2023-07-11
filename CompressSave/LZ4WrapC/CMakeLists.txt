cmake_minimum_required(VERSION 3.2)

project(lz4wrap)

add_library(lz4wrap SHARED
  lz4/lz4.c lz4/lz4.h
  lz4/lz4frame.c lz4/lz4frame.h lz4/lz4frame_static.h
  lz4/lz4hc.c lz4/lz4hc.h
  lz4/xxhash.c lz4/xxhash.h
  dllmain.c LZ4Wrap.c LZ4Wrap.h)

target_compile_definitions(lz4wrap PRIVATE LZ4WRAP_EXPORTS)
target_include_directories(lz4wrap PRIVATE lz4)
if(WIN32)
  set_target_properties(lz4wrap PROPERTIES PREFIX "")
endif()
