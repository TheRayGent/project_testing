#include "JSONDatabase.hpp"

JSONDatabase::JSONDatabase(std::string file_path) : db_file(std::move(file_path)) {}

// Инициализация бд
void JSONDatabase::init(){
    load_from_disk();
}

// Загрузка бд из файла в опреативную память
void JSONDatabase::load_from_disk() {
    std::ifstream file(db_file);
    if (file.is_open() && file.peek() != std::ifstream::traits_type::eof()) {
        try {
            file >> cache_data;

            if (!cache_data.contains("data")) cache_data["data"] = json::object();
            if (!cache_data.contains("next_id")) cache_data["next_id"] = 1;

            return;
        }
        catch (const json::parse_error&) {
            cache_data = {{"next_id", 1}, {"data", json::object()}};
        }
    } else {
        cache_data = {{"next_id", 1}, {"data", json::object()}};
    }
}

// Запись бд в файл
void JSONDatabase::save_to_disk_internal() const {
    std::ofstream file(db_file);
    file << cache_data.dump(4);
}

// Чтение бд из оперативной памяти
json JSONDatabase::read() const {
    std::shared_lock<std::shared_mutex> lock(db_mutex);
    return cache_data;
}

// Потокозащищённая запись бд на в файл
void JSONDatabase::update(std::function<void(json&)> transform_func) {
    std::unique_lock<std::shared_mutex> lock(db_mutex);
    transform_func(cache_data);
    save_to_disk_internal();
}

// Дочерний класс
UnindexedJSONDatabase::UnindexedJSONDatabase(std::string file_path) : JSONDatabase(std::move(file_path)) {}

// Загрузка бд, только без переменной next_id
void UnindexedJSONDatabase::load_from_disk() {
    std::ifstream file(db_file);
    if (file.is_open() && file.peek() != std::ifstream::traits_type::eof()) {
        try {
            file >> cache_data;

            return;
        }
        catch (const json::parse_error&) {
            cache_data = json::object();
        }
    } else {
        cache_data = json::object();
    }
}
