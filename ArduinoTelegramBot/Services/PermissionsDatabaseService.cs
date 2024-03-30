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
    public async Task SaveScheduledTaskAsync(ScheduledLoadFromDBTaskInfo taskData)
    {
        List<ScheduledLoadFromDBTaskInfo> tasks = await LoadScheduledTasksAsync();
        tasks.Add(taskData);
        var json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
        await File.WriteAllTextAsync(_tasksFilePath, json);
        Log.Information("База данных: Запланированная задача '{TaskId}' успешно сохранена.", taskData.TaskId);
    }

    public async Task DeleteScheduledTaskAsync(string taskId)
    {
        List<ScheduledLoadFromDBTaskInfo> tasks = await LoadScheduledTasksAsync();
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

    public async Task<List<ScheduledLoadFromDBTaskInfo>> LoadScheduledTasksAsync()
    {
        if (!File.Exists(_tasksFilePath))
        {
            Log.Warning("База данных: Файл запланированных задач '{FilePath}' не найден. Возвращается пустой список задач.", _tasksFilePath);
            return new List<ScheduledLoadFromDBTaskInfo>();
        }

        var json = await File.ReadAllTextAsync(_tasksFilePath);
        var tasks = JsonConvert.DeserializeObject<List<ScheduledLoadFromDBTaskInfo>>(json) ?? new List<ScheduledLoadFromDBTaskInfo>();
        Log.Information("База данных: Запланированные задачи успешно загружены из файла '{FilePath}'.", _tasksFilePath);
        return tasks;
    }
}