#pragma once

#include <nlohmann/json.hpp>
#include <fstream>
#include <shared_mutex>
#include <string>
#include <functional>

using json = nlohmann::json;

class JSONDatabase {
private:
    std::string db_file;
    mutable std::shared_mutex db_mutex;
    json cache_data;

    void load_from_disk();

    void save_to_disk_internal() const; 

public:
    explicit JSONDatabase(std::string file_path);

    json read() const;

    void update(std::function<void(json&)> transform_func);
};
