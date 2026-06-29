cmake_minimum_required(VERSION 3.15)
project(ProjectTestingBackend LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)

FetchContent_Declare(
    nlohmann_json
    GIT_REPOSITORY https://github.com/nlohmann/json
    GIT_TAG v3.12.0
)
FetchContent_MakeAvailable(nlohmann_json)

FetchContent_Declare(
    asio
    GIT_REPOSITORY https://github.com/chriskohlhoff/asio
    GIT_TAG asio-1-38-0
)
FetchContent_MakeAvailable(asio)

set(ASIO_INCLUDE_DIR "${asio_SOURCE_DIR}/asio/include" CACHE PATH "" FORCE)
include_directories(${ASIO_INCLUDE_DIR})

FetchContent_Declare(
    Crow
    GIT_REPOSITORY https://github.com/crowcpp/crow
    GIT_TAG v1.3.2
)
FetchContent_MakeAvailable(Crow)

FetchContent_Declare(
    jwt-cpp
    GIT_REPOSITORY https://github.com/Thalhammer/jwt-cpp
    GIT_TAG v0.7.2
)
FetchContent_MakeAvailable(jwt-cpp)

FetchContent_Declare(
    picosha2
    GIT_REPOSITORY https://github.com/okdshin/PicoSHA2
    GIT_TAG v1.0.1
)
FetchContent_MakeAvailable(picosha2)

add_library(database STATIC 
    database/JSONDatabase.cpp
    database/JSONDatabase.hpp
)
target_include_directories(database PUBLIC database)
target_link_libraries(database PUBLIC nlohmann_json::nlohmann_json)

add_library(config STATIC 
    config/JSONConfig.cpp
    config/JSONConfig.hpp
)
target_include_directories(config PUBLIC config)
target_link_libraries(config PUBLIC nlohmann_json::nlohmann_json)

add_library(utils STATIC 
    utils/utils.cpp
    utils/utils.hpp
)
target_include_directories(utils PUBLIC utils)
target_link_libraries(utils PUBLIC
    config
    picosha2
)

add_library(users_routes STATIC 
    routes/users_routes.cpp
    routes/users_routes.hpp
)
target_include_directories(users_routes PUBLIC routes)
target_link_libraries(users_routes PUBLIC
    utils
    database
    nlohmann_json::nlohmann_json
    jwt-cpp::jwt-cpp
    Crow::Crow
)

add_library(tests_routes STATIC 
    routes/tests_routes.cpp
    routes/tests_routes.hpp
)
target_include_directories(tests_routes PUBLIC routes)
target_link_libraries(tests_routes PUBLIC
    utils
    database
    nlohmann_json::nlohmann_json
    jwt-cpp::jwt-cpp
    Crow::Crow
)

add_executable(project_testing main.cpp)
target_compile_definitions(project_testing PRIVATE CROW_ENABLE_SSL)
target_link_libraries(project_testing PRIVATE
    tests_routes
    users_routes
)

if(MSVC)
    target_compile_options(config PRIVATE /source-charset:.65001)
    target_compile_options(database PRIVATE /source-charset:.65001)
    target_include_directories(project_testing PRIVATE "C:/Program Files/OpenSSL-Win64/include")
    target_link_directories(project_testing PRIVATE "C:/Program Files/OpenSSL-Win64/lib/VC/x64/MT")
    target_link_libraries(project_testing PRIVATE libssl_static libcrypto_static user32 advapi32 crypt32)
else()
    find_package(OpenSSL REQUIRED)
    
    find_package(Threads REQUIRED)
    
    target_link_libraries(project_testing PRIVATE 
        OpenSSL::SSL 
        OpenSSL::Crypto
        Threads::Threads
    )
endif()