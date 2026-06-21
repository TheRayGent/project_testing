cmake_minimum_required(VERSION 3.14)
project(MyCppWebsite LANGUAGES CXX)

# Устанавливаем стандарт C++17
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# --- АВТОМАТИЧЕСКАЯ ЗАГРУЗКА БИБЛИОТЕК ИЗ GITHUB ---
include(FetchContent)

# 1. Скачиваем nlohmann/json
FetchContent_Declare(
    nlohmann_json
    GIT_REPOSITORY https://github.com/nlohmann/json
    GIT_TAG v3.12.0
)
# ИСПРАВЛЕНО: Имя внутри MakeAvailable должно строго совпадать с именем в Declare выше!
FetchContent_MakeAvailable(nlohmann_json)

# 2. ЯВНО СКАЧИВАЕМ ASIO (Это решает проблему с ошибкой Could NOT find asio)
FetchContent_Declare(
    asio
    GIT_REPOSITORY https://github.com/chriskohlhoff/asio
    GIT_TAG asio-1-38-0
)
FetchContent_MakeAvailable(asio)

# Принудительно передаем путь к скачанному Asio для скрипта Crow
set(ASIO_INCLUDE_DIR "${asio_SOURCE_DIR}/asio/include" CACHE PATH "" FORCE)
include_directories(${ASIO_INCLUDE_DIR})

# 3. Скачиваем Crow
FetchContent_Declare(
    Crow
    GIT_REPOSITORY https://github.com/crowcpp/crow
    GIT_TAG v1.3.2
)
FetchContent_MakeAvailable(Crow)

# 4. Скачиваем jwt-cpp
FetchContent_Declare(
    jwt-cpp
    GIT_REPOSITORY https://github.com/Thalhammer/jwt-cpp
    GIT_TAG v0.7.2
)
FetchContent_MakeAvailable(jwt-cpp)

# 5. Скачиваем picosha2
FetchContent_Declare(
    picosha2
    GIT_REPOSITORY https://github.com/okdshin/PicoSHA2
    GIT_TAG v1.0.1
)
FetchContent_MakeAvailable(picosha2)

# --- НАСТРОЙКА НАШЕГО ПРИЛОЖЕНИЯ ---

# Указываем главный файл нашего сервера
add_executable(project_testing main.cpp)

# Включаем поддержку SSL в Crow
target_compile_definitions(project_testing PRIVATE CROW_ENABLE_SSL)

# Говорим CMake связать наш сервер с библиотеками и OpenSSL
target_link_libraries(project_testing PRIVATE 
    Crow::Crow 
    nlohmann_json::nlohmann_json
    jwt-cpp::jwt-cpp
    picosha2
)

# Для Windows и компилятора MSVC (cl.exe) автоматически подключаем OpenSSL
if(MSVC)
    target_include_directories(project_testing PRIVATE "C:/Program Files/OpenSSL-Win64/include")
    target_link_directories(project_testing PRIVATE "C:/Program Files/OpenSSL-Win64/lib/VC/x64/MT")
    target_link_libraries(project_testing PRIVATE libssl_static libcrypto_static user32 advapi32 crypt32)
endif()
