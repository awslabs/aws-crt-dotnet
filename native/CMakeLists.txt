cmake_minimum_required(VERSION 3.1)

project(aws-crt-dotnet C)
option(BUILD_DEPS "Builds aws common runtime dependencies as part of build" ON)

if (POLICY CMP0069)
    cmake_policy(SET CMP0069 NEW) # Enable LTO/IPO if available in the compiler, see AwsCFlags
endif()

if (POLICY CMP0077)
    cmake_policy(SET CMP0077 NEW) # Enable options to get their values from normal variables
endif()

if (DEFINED CMAKE_PREFIX_PATH)
    file(TO_CMAKE_PATH ${CMAKE_PREFIX_PATH} CMAKE_PREFIX_PATH)
endif()

if (DEFINED CMAKE_INSTALL_PREFIX)
    file(TO_CMAKE_PATH ${CMAKE_INSTALL_PREFIX} CMAKE_INSTALL_PREFIX)
endif()


if (UNIX AND NOT APPLE)
    include(GNUInstallDirs)
elseif(NOT DEFINED CMAKE_INSTALL_LIBDIR)
    set(CMAKE_INSTALL_LIBDIR "lib")
endif()

list(APPEND CMAKE_MODULE_PATH "${CMAKE_PREFIX_PATH}/${CMAKE_INSTALL_LIBDIR}/cmake")

if (${CMAKE_INSTALL_LIBDIR} STREQUAL "lib64")
    set(FIND_LIBRARY_USE_LIB64_PATHS true)
endif()

if (NOT WIN32 AND NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE "RelWithDebInfo")
endif()

# This is required in order to append /lib/cmake to each element in CMAKE_PREFIX_PATH
set(AWS_MODULE_DIR "/${CMAKE_INSTALL_LIBDIR}/cmake")
string(REPLACE ";" "${AWS_MODULE_DIR};" AWS_MODULE_PATH "${CMAKE_PREFIX_PATH}${AWS_MODULE_DIR}")
# Append that generated list to the module search path
list(APPEND CMAKE_MODULE_PATH ${AWS_MODULE_PATH})

if (BUILD_DEPS)
    list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_LIST_DIR}/../crt/aws-c-common/cmake")
    message(STATUS "CMAKE_MODULE_PATH = ${CMAKE_MODULE_PATH}")
    include(AwsFindPackage)

    set(IN_SOURCE_BUILD ON)
    set(BUILD_TESTING OFF)
    add_subdirectory(../crt/aws-c-common ${CMAKE_BINARY_DIR}/deps/aws-c-common)
    if (UNIX AND NOT APPLE)
        include(AwsPrebuildDependency)
        set(SEARCH_LIBCRYPTO OFF CACHE BOOL "libcrypto will come from aws-lc, don't look for it")
        set(AWSLC_CMAKE_ARGUMENTS
                -DDISABLE_GO=ON  # Build without using Go, we don't want the extra dependency
                -DDISABLE_PERL=ON  # Build without using Perl, we don't want the extra dependency
                -DBUILD_LIBSSL=OFF  # Don't need libssl, only need libcrypto
                -DBUILD_TESTING=OFF
            )
        if(CMAKE_C_COMPILER_ID MATCHES "GNU" AND CMAKE_C_COMPILER_VERSION VERSION_LESS "5.0")
            # Disable AVX512 on old GCC that not supports it.
            list(APPEND AWSLC_CMAKE_ARGUMENTS -DMY_ASSEMBLER_IS_TOO_OLD_FOR_512AVX=ON)
        endif()

        # s2n-tls uses libcrypto during its configuration, so we need to prebuild aws-lc.
        aws_prebuild_dependency(
                DEPENDENCY_NAME AWSLC
                SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/crt/aws-lc
                CMAKE_ARGUMENTS ${AWSLC_CMAKE_ARGUMENTS}
            )

        # prebuild s2n-tls.
        aws_prebuild_dependency(
            DEPENDENCY_NAME S2N
            SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/crt/s2n
            CMAKE_ARGUMENTS
                -DUNSAFE_TREAT_WARNINGS_AS_ERRORS=OFF
                -DBUILD_TESTING=OFF
        )

    endif()
    add_subdirectory(../crt/aws-c-io ${CMAKE_BINARY_DIR}/deps/aws-c-io)
    add_subdirectory(../crt/aws-c-sdkutils ${CMAKE_BINARY_DIR}/deps/sdkutils)
    add_subdirectory(../crt/aws-c-cal ${CMAKE_BINARY_DIR}/deps/aws-c-cal)
    add_subdirectory(../crt/aws-c-compression ${CMAKE_BINARY_DIR}/deps/aws-c-compression)
    add_subdirectory(../crt/aws-c-http ${CMAKE_BINARY_DIR}/deps/aws-c-http)
    add_subdirectory(../crt/aws-c-auth ${CMAKE_BINARY_DIR}/deps/aws-c-auth)
    add_subdirectory(../crt/aws-checksums ${CMAKE_BINARY_DIR}/deps/aws-checksums)
else()
    include(AwsFindPackage)
    set(IN_SOURCE_BUILD OFF)
endif()

include(AwsCFlags)
include(AwsSharedLibSetup)
include(AwsSanitizers)
include(AwsFeatureTests)

# Native CRT bindings files
file(GLOB AWS_CRT_DOTNET_HEADERS src/*.h)
file(GLOB AWS_CRT_DOTNET_SRC src/*.c)

# shared lib that contains the CRT and API bindings, to be loaded by .NET
add_library(${PROJECT_NAME} SHARED ${AWS_CRT_DOTNET_HEADERS} ${AWS_CRT_DOTNET_SRC})

# AWS Common Runtime dependencies
aws_use_package(aws-c-auth)
aws_use_package(aws-checksums)

target_link_libraries(${PROJECT_NAME} ${DEP_AWS_LIBS})

set(TARGET_LIB_DIR "lib")
if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    if (AWS_ARCH_ARM64)
        set(TARGET_ARCH "ARM64")
    else()
        set(TARGET_ARCH "x64")
    endif()
else()
    set(TARGET_ARCH "x86")
endif()

set_target_properties(${PROJECT_NAME} PROPERTIES LINKER_LANGUAGE C)
set_target_properties(${PROJECT_NAME} PROPERTIES OUTPUT_NAME "${PROJECT_NAME}-${TARGET_ARCH}")
# for non-msvc, library output will control the shared lib output
set_target_properties(${PROJECT_NAME} PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${TARGET_LIB_DIR})
# for msvc, runtime controls where dlls are output
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${TARGET_LIB_DIR})
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY_DEBUG ${TARGET_LIB_DIR})
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY_RELEASE ${TARGET_LIB_DIR})
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY_RELWITHDEBINFO ${TARGET_LIB_DIR})
set_target_properties(${PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY_MINSIZEREL ${TARGET_LIB_DIR})

set(CMAKE_C_FLAGS_DEBUGOPT "")
set_property(TARGET ${PROJECT_NAME} PROPERTY C_STANDARD 99)

aws_set_common_properties(${PROJECT_NAME})
target_include_directories(${PROJECT_NAME} PRIVATE src)

# This is always a shared lib, so always export
target_compile_definitions(${PROJECT_NAME} PUBLIC "-DAWS_DOTNET_USE_IMPORT_EXPORT")
target_compile_definitions(${PROJECT_NAME} PRIVATE "-DAWS_DOTNET_EXPORTS")

target_include_directories(${PROJECT_NAME} PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>)

aws_split_debug_info(${PROJECT_NAME})
