#include "JSONDatabase.hpp"

JSONDatabase::JSONDatabase(std::string file_path) : db_file(std::move(file_path)) {}

void JSONDatabase::init(){
    load_from_disk();
}

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

void JSONDatabase::save_to_disk_internal() const {
    std::ofstream file(db_file);
    file << cache_data.dump(4);
}

json JSONDatabase::read() const {
    std::shared_lock<std::shared_mutex> lock(db_mutex);
    return cache_data;
}

void JSONDatabase::update(std::function<void(json&)> transform_func) {
    std::unique_lock<std::shared_mutex> lock(db_mutex);
    transform_func(cache_data);
    save_to_disk_internal();
}

UnindexedJSONDatabase::UnindexedJSONDatabase(std::string file_path) : JSONDatabase(std::move(file_path)) {}

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
