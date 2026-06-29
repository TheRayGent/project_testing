#include "JSONDatabase.hpp"
#include <iostream>

JSONDatabase::JSONDatabase(std::string file_path) : db_file(std::move(file_path)) {}

// Инициализация бд
void JSONDatabase::init(){
    if (load_from_disk()){
        std::cout << "[WARNING] Не удалось открыть файл " << db_file << ", файл будет пересоздан по шаблону при следующей записи.\n";
    }
}

// Загрузка бд из файла в опреативную память
bool JSONDatabase::load_from_disk() {
    bool recreated = false;
    std::ifstream file(db_file);
    if (file.is_open() && file.peek() != std::ifstream::traits_type::eof()) {
        try {
            file >> cache_data;

            if (!cache_data.contains("data")) cache_data["data"] = json::object();
            if (!cache_data.contains("next_id")) cache_data["next_id"] = 1;
        }
        catch (const json::parse_error&) {
            cache_data = {{"next_id", 1}, {"data", json::object()}};
            recreated = true;
        }
    } else {
        cache_data = {{"next_id", 1}, {"data", json::object()}};
        recreated = true;
    }
    return recreated;
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
bool UnindexedJSONDatabase::load_from_disk() {
    bool recreated = false;
    std::ifstream file(db_file);
    if (file.is_open() && file.peek() != std::ifstream::traits_type::eof()) {
        try {
            file >> cache_data;
        }
        catch (const json::parse_error&) {
            cache_data = json::object();
            recreated = true;
        }
    } else {
        cache_data = json::object();
        recreated = true;
    }
    return recreated;
}
