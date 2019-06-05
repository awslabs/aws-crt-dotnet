cmake_minimum_required(VERSION 3.1)

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

option(BUILD_DEPS "Builds aws common runtime dependencies as part of build" ON)

if (BUILD_DEPS)
    set(AWS_DEPS_BUILD_DIR ${CMAKE_CURRENT_BINARY_DIR}/deps)
    set(AWS_DEPS_INSTALL_DIR ${CMAKE_CURRENT_BINARY_DIR}/deps/install)

    list(APPEND CMAKE_MODULE_PATH "${AWS_DEPS_INSTALL_DIR}/lib/cmake")
    list(APPEND CMAKE_MODULE_PATH "${AWS_DEPS_INSTALL_DIR}/lib64/cmake")

    if (NOT DEFINED CMAKE_PREFIX_PATH)
        set(CMAKE_PREFIX_PATH ${AWS_DEPS_INSTALL_DIR})
    endif()

    file(MAKE_DIRECTORY ${AWS_DEPS_BUILD_DIR})

    # run another cmake to do the deps build
    if(TARGET_ARCH STREQUAL "ANDROID")
        execute_process(
                COMMAND ${CMAKE_COMMAND} -G ${CMAKE_GENERATOR}
                -DTARGET_ARCH=${TARGET_ARCH}
                -DCMAKE_TOOLCHAIN_FILE=${CMAKE_TOOLCHAIN_FILE}
                -DANDROID_NATIVE_API_LEVEL=${ANDROID_NATIVE_API_LEVEL}
                -DANDROID_ABI=${ANDROID_ABI}
                -DANDROID_TOOLCHAIN_NAME=${ANDROID_TOOLCHAIN_NAME}
                -DANDROID_STANDALONE_TOOLCHAIN=${ANDROID_STANDALONE_TOOLCHAIN}
                -DANDROID_STL=${ANDROID_STL}
                -DCMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE}
                -DCMAKE_PREFIX_PATH=${AWS_DEPS_INSTALL_DIR}
                -DCMAKE_INSTALL_PREFIX=${AWS_DEPS_INSTALL_DIR}
                -DCMAKE_PREFIX_PATH=${CMAKE_PREFIX_PATH}
                -DGIT_EXECUTABLE=${GIT_EXECUTABLE}
                -DCMAKE_C_FLAGS=${CMAKE_C_FLAGS}
                ${CMAKE_CURRENT_SOURCE_DIR}/../aws-common-runtime
                WORKING_DIRECTORY ${AWS_DEPS_BUILD_DIR}
                RESULT_VARIABLE BUILD_AWSCRT_EXIT_CODE
        )

    else()
        execute_process(
                COMMAND ${CMAKE_COMMAND} -G ${CMAKE_GENERATOR}
                -DTARGET_ARCH=${TARGET_ARCH}
                -DCMAKE_TOOLCHAIN_FILE=${CMAKE_TOOLCHAIN_FILE}
                -DCMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE}
                -DCMAKE_PREFIX_PATH=${AWS_DEPS_INSTALL_DIR}
                -DCMAKE_INSTALL_PREFIX=${AWS_DEPS_INSTALL_DIR}
                -DCMAKE_PREFIX_PATH=${CMAKE_PREFIX_PATH}
                -DCMAKE_RUNTIME_OUTPUT_DIRECTORY=${CMAKE_CURRENT_BINARY_DIR}/bin
                -DCMAKE_C_FLAGS=${CMAKE_C_FLAGS}
                ${CMAKE_CURRENT_SOURCE_DIR}/../aws-common-runtime
                WORKING_DIRECTORY ${AWS_DEPS_BUILD_DIR}
                RESULT_VARIABLE BUILD_AWSCRT_EXIT_CODE
        )
    endif()

    if (NOT ${BUILD_AWSCRT_EXIT_CODE} EQUAL 0)
        message(FATAL_ERROR "Failed to configure aws crt libraries.")
    endif()
    execute_process(COMMAND ${CMAKE_COMMAND} --build ${AWS_DEPS_BUILD_DIR} --config ${CMAKE_BUILD_TYPE}
            RESULT_VARIABLE BUILD_AWSCRT_EXIT_CODE)

    if (NOT ${BUILD_AWSCRT_EXIT_CODE} EQUAL 0)
        message(FATAL_ERROR "Failed to build aws crt libraries.")
    endif()
    message(STATUS "AWS Common Runtime is installed at: ${AWS_DEPS_INSTALL_DIR}")

    #the following two lines are done in this branch intentionally, don't move it. project() does some magic that
    #we don't want happening until we're done with the above code.
    list(APPEND CMAKE_PREFIX_PATH "${AWS_DEPS_INSTALL_DIR}")
    project(aws-crt-dotnet C)
else()
    #the following two lines are done in this branch intentionally, don't move it. project() does some magic that
    #we want happening exactly right now.
    project(aws-crt-dotnet C)

    list(APPEND CMAKE_MODULE_PATH "${CMAKE_PREFIX_PATH}/${CMAKE_INSTALL_LIBDIR}/cmake")
endif()

include(AwsCFlags)
include(AwsSharedLibSetup)
include(AwsSanitizers)

# Native CRT bindings files
file(GLOB AWS_CRT_DOTNET_HEADERS src/*.h)
file(GLOB AWS_CRT_DOTNET_SRC src/*.c)

set(TARGET_LIB_DIR "lib")
if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    set(TARGET_ARCH "x64")
else()
    set(TARGET_ARCH "x86")
endif()

# shared lib that contains the CRT and API bindings, to be loaded by .NET
add_library(${CMAKE_PROJECT_NAME} SHARED ${AWS_CRT_DOTNET_HEADERS} ${AWS_CRT_DOTNET_SRC})
set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES LINKER_LANGUAGE C)
set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES OUTPUT_NAME "${CMAKE_PROJECT_NAME}-${TARGET_ARCH}")
# for non-msvc, library output will control the shared lib output
set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${TARGET_LIB_DIR})
# for msvc, runtime controls where dlls are output
set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${TARGET_LIB_DIR})
set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY_DEBUG ${TARGET_LIB_DIR})
set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY_RELEASE ${TARGET_LIB_DIR})
set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES RUNTIME_OUTPUT_DIRECTORY_RELWITHDEBINFO ${TARGET_LIB_DIR})

aws_set_common_properties(${CMAKE_PROJECT_NAME})
include_directories(src)

# This is always a shared lib, so always export
target_compile_definitions(${CMAKE_PROJECT_NAME} PUBLIC "-DAWS_DOTNET_USE_IMPORT_EXPORT")
target_compile_definitions(${CMAKE_PROJECT_NAME} PRIVATE "-DAWS_DOTNET_EXPORTS")

target_include_directories(${CMAKE_PROJECT_NAME} PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>)

find_package(aws-c-mqtt REQUIRED)
find_package(aws-c-http REQUIRED)
target_link_libraries(${CMAKE_PROJECT_NAME} AWS::aws-c-mqtt)
target_link_libraries(${CMAKE_PROJECT_NAME} AWS::aws-c-http)
