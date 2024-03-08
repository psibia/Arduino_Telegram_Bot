using ArduinoTelegramBot.Models;
using ArduinoTelegramBot.Models.Sheduler;
using ArduinoTelegramBot.Services.Interfaces;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services;

public class PermissionsDatabaseService : IPermissionsDatabaseService
{
    private static readonly List<AccessKey> _accessKeys = new()
    {
        new AccessKey("adminKey", new List<string> {"/user", "/shedule"}, isActive: true, isMasterKey: true),
        new AccessKey("commandKey", new List<string> {"/admin", "/user", "/get_serial", "/start_serial", "/serial", "/temp", "/close_serial", "/open_serial"}, isActive: true, isMasterKey: false),
        new AccessKey("userKey", new List<string> {"/user"}, isActive: true, isMasterKey: false)
    };

    public async Task<AccessKey> GetPermissionsAsync(string key)
    {
        await Task.Delay(42); // Имитация асинхронной операции
        var accessKey = _accessKeys.FirstOrDefault(k => k.Key == key && k.IsActive);

        if (accessKey != null)
        {
            Log.Information("База данных: Доступ к ключу '{Key}' успешно получен. Активен: {IsActive}, Мастер-ключ: {IsMasterKey}.", key, accessKey.IsActive, accessKey.IsMasterKey);
            return accessKey;
        }
        else
        {
            Log.Warning("База данных: Ключ '{Key}' не найден или не активен.", key);
            throw new KeyNotFoundException($"Ключ '{key}' не найден в базе данных или не активен.");
        }
    }

    private static readonly string _authDataFilePath = "authData.json";
    public async Task<ConcurrentDictionary<long, string>> LoadUserKeysAsync()
    {
        if (File.Exists(_authDataFilePath))
        {
            var json = await File.ReadAllTextAsync(_authDataFilePath);
            var userKeys = JsonConvert.DeserializeObject<ConcurrentDictionary<long, string>>(json) ?? new ConcurrentDictionary<long, string>();

            Log.Information("База данных: Ключи пользователя успешно загружены из файла '{FilePath}'.", _authDataFilePath);
            Log.Debug("База данных: Загруженные ключи пользователей:\n{UserKeys}", json);
            return userKeys;
        }
        else
        {
            Log.Warning("База данных: Файл авторизационных данных '{FilePath}' не найден, начинаем с пустого набора ключей пользователей.", _authDataFilePath);
            return new ConcurrentDictionary<long, string>();
        }
    }

    public async Task SaveUserKeysAsync(ConcurrentDictionary<long, string> userKeys)
    {
        var json = JsonConvert.SerializeObject(userKeys, Formatting.Indented);
        await File.WriteAllTextAsync(_authDataFilePath, json);
        Log.Information("База данных: Ключи пользователей успешно сохранены в файл '{FilePath}'.", _authDataFilePath);
        Log.Debug("База данных: Сохраненные ключи пользователей: {UserKeys}", json);
    }

    private readonly string _serialPortConfigFile = "SerialPortConfig.json";
    public async Task<(string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits)> LoadSerialPortConfigAsync()
    {
        if (File.Exists(_serialPortConfigFile))
        {
            string jsonConfig = await File.ReadAllTextAsync(_serialPortConfigFile);
            var config = JsonConvert.DeserializeObject<dynamic>(jsonConfig);
            Log.Information("База данных: Конфигурация последовательного порта загружена из файла '{FilePath}'.", _serialPortConfigFile);
            return (config.PortName, config.BaudRate, config.Parity, config.DataBits, config.StopBits);
        }

        Log.Warning("База данных: Конфигурационный файл '{FilePath}' не найден.", _serialPortConfigFile);
        return default;
    }



    private readonly string _filePath = "subscriptions.json";
    public async Task<Dictionary<string, HashSet<long>>> LoadSubscriptionsAsync()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string json = await File.ReadAllTextAsync(_filePath);
                var subscriptions = JsonConvert.DeserializeObject<Dictionary<string, HashSet<long>>>(json);
                Log.Information("База данных: Подписки успешно загружены из файла.");
                return subscriptions ?? new Dictionary<string, HashSet<long>>();
            }
            else
            {
                Log.Information("База данных: Файл подписок не найден. Создается новый список подписок.");
                return new Dictionary<string, HashSet<long>>();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "База данных: Ошибка при загрузке подписок из файла.");
            throw; // Перебрасываем исключение, если обработка на уровне выше предполагается
        }
    }
    public async Task SaveSubscriptionsAsync(Dictionary<string, HashSet<long>> subscriptions)
    {
        try
        {
            string json = JsonConvert.SerializeObject(subscriptions, Formatting.Indented);
            await File.WriteAllTextAsync(_filePath, json);
            Log.Information("База данных: Подписки успешно сохранены в файл.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "База данных: Ошибка при сохранении подписок в файл.");
            throw; // Перебрасываем исключение, если обработка на уровне выше предполагается
        }
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
        Log.Information("База данных: Конфигурация последовательного порта сохранена в файл '{FilePath}'.", _serialPortConfigFile);
    }

    private readonly string _tasksFilePath = "scheduledTasks.json";
    public async Task SaveScheduledTaskAsync(ScheduledTaskData taskData)
    {
        List<ScheduledTaskData> tasks = await LoadScheduledTasksAsync();
        tasks.Add(taskData);
        var json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
        await File.WriteAllTextAsync(_tasksFilePath, json);
        Log.Information("База данных: Запланированная задача '{TaskId}' успешно сохранена.", taskData.TaskId);
    }

    public async Task DeleteScheduledTaskAsync(string taskId)
    {
        List<ScheduledTaskData> tasks = await LoadScheduledTasksAsync();
        var taskToRemove = tasks.FirstOrDefault(task => task.TaskId == taskId);
        if (taskToRemove != null)
        {
            tasks.Remove(taskToRemove);
            var json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
            await File.WriteAllTextAsync(_tasksFilePath, json);
            Log.Information("База данных: Запланированная задача '{TaskId}' успешно удалена.", taskId);
        }
        else
        {
            Log.Warning("База данных: Запланированная задача '{TaskId}' не найдена для удаления.", taskId);
        }
    }

    public async Task<List<ScheduledTaskData>> LoadScheduledTasksAsync()
    {
        if (!File.Exists(_tasksFilePath))
        {
            Log.Warning("База данных: Файл запланированных задач '{FilePath}' не найден. Возвращается пустой список задач.", _tasksFilePath);
            return new List<ScheduledTaskData>();
        }

        var json = await File.ReadAllTextAsync(_tasksFilePath);
        var tasks = JsonConvert.DeserializeObject<List<ScheduledTaskData>>(json) ?? new List<ScheduledTaskData>();
        Log.Information("База данных: Запланированные задачи успешно загружены из файла '{FilePath}'.", _tasksFilePath);
        return tasks;
    }
}