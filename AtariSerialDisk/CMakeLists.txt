# CMakeList.txt : CMake project for AtariSerialDisk, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.10)

# Enable Hot Reload for MSVC compilers if supported.
if (POLICY CMP0141)
  cmake_policy(SET CMP0141 NEW)
  set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<IF:$<AND:$<C_COMPILER_ID:MSVC>,$<CXX_COMPILER_ID:MSVC>>,$<$<CONFIG:Debug,RelWithDebInfo>:EditAndContinue>,$<$<CONFIG:Debug,RelWithDebInfo>:ProgramDatabase>>")
endif()

project ("AtariSerialDisk" VERSION 3.1.0)
set(CMAKE_SYSTEM_NAME Generic)

set(toolsprefix m68k-atari-mint)
set(CMAKE_C_COMPILER ${toolsprefix}-gcc)

set(CMAKE_LINKER_FLAGS "-march=68000 -Os -Wall -nostartfiles -nodefaultlibs -nostdlib -s -static -Wl,--traditional-format")
set(SOURCE_FILES ${PROJECT_SOURCE_DIR}/src/main.s)

set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_LINKER_FLAGS} ${SOURCE_FILES}")

add_executable (SERDISK3.TOS ${SOURCE_FILES})

set_target_properties(SERDISK3.TOS PROPERTIES
                    LINKER_LANGUAGE C
                    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/bin)

file(COPY ${PROJECT_SOURCE_DIR}/src DESTINATION ${CMAKE_CURRENT_BINARY_DIR})
file(COPY ${PROJECT_SOURCE_DIR}/macro DESTINATION ${CMAKE_CURRENT_BINARY_DIR})
file(COPY ${PROJECT_SOURCE_DIR}/assets/ DESTINATION ${CMAKE_CURRENT_BINARY_DIR}/bin)
