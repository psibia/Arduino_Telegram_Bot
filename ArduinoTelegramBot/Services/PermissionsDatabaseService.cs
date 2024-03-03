﻿using ArduinoTelegramBot.Services.Interfaces;
using Newtonsoft.Json;
using Serilog;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services;

public class PermissionsDatabaseService : IPermissionsDatabaseService
{
    private static readonly Dictionary<string, List<string>> _permissions = new()
    {
        {"adminKey", new List<string> {"/admin", "/user", "/get_serial", "/start_serial", "/serial", "/temp", "/close_serial", "/open_serial"}},
        {"userKey", new List<string> {"/user"}}
    };
    public async Task<List<string>> GetPermissionsAsync(string key)
    {
        await Task.Delay(42);
        if (_permissions.TryGetValue(key, out var permissions))
        {
            Log.Information("База данных: Разрешения для ключа '{Key}' успешно получены", key);
            return permissions;
        }
        else
        {
            Log.Warning("База данных: Ключ '{Key}' не найден", key);
            throw new KeyNotFoundException($"Ключ '{key}' не найден в базе данных.");
        }
    }




    private static readonly string _authDataFilePath = "authData.json";
    public async Task<Dictionary<long, string>> LoadUserKeysAsync()
    {
        if (File.Exists(_authDataFilePath))
        {
            var json = await File.ReadAllTextAsync(_authDataFilePath);
            var userKeys = JsonConvert.DeserializeObject<Dictionary<long, string>>(json) ?? new Dictionary<long, string>();

            Log.Information("База данных: Ключи пользователя успешно загружены");
            Log.Debug("База данных: Загруженные ключи пользователей: {UserKeys}", json);
            return userKeys;
        }
        else
        {
            Log.Warning("В БД не найдены авторизационные данные, начинаем с пустого набора ключей пользователей.");
            return new Dictionary<long, string>();
        }
    }

    public async Task SaveUserKeysAsync(Dictionary<long, string> userKeys)
    {
        var json = JsonConvert.SerializeObject(userKeys, Formatting.Indented);
        await File.WriteAllTextAsync(_authDataFilePath, json);
        Log.Information("База данных: Ключи пользователей успешно сохранены");
        Log.Debug("База данных: Сохраненные ключи пользователей: {UserKeys}", json);
    }




    private readonly string _serialPortConfigFile = "SerialPortConfig.json";
    public async Task<(string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits)> LoadSerialPortConfigAsync()
    {
        if (File.Exists(_serialPortConfigFile))
        {
            string jsonConfig = await File.ReadAllTextAsync(_serialPortConfigFile);
            var config = JsonConvert.DeserializeObject<dynamic>(jsonConfig);
            Log.Information($"Конфигурация последовательного порта загружена из {_serialPortConfigFile}");
            return (config.PortName, config.BaudRate, config.Parity, config.DataBits, config.StopBits);
        }

        Log.Warning($"Конфигурационный файл {_serialPortConfigFile} не найден");
        return default;
    }

    public async Task SaveSerialPortConfigAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
    {
        var config = new
        {
            PortName = portName,
            BaudRate = baudRate,
            Parity = parity,
            DataBits = dataBits,
            StopBits = stopBits
        };
        string jsonConfig = JsonConvert.SerializeObject(config, Formatting.Indented);
        await File.WriteAllTextAsync(_serialPortConfigFile, jsonConfig);
        Log.Information($"Конфигурация последовательного порта сохранена в {_serialPortConfigFile}");
    }
}