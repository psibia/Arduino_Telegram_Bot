using ArduinoTelegramBot.Models;
using ArduinoTelegramBot.Models.Sheduler;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface IPermissionsDatabaseService
    {
        Task<AccessKey> GetPermissionsAsync(string key);
        Task<ConcurrentDictionary<long, string>> LoadUserKeysAsync();
        Task SaveUserKeysAsync(ConcurrentDictionary<long, string> userKeys);
        Task<(string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits)> LoadSerialPortConfigAsync();
        Task SaveSerialPortConfigAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task SaveScheduledTaskAsync(ScheduledTaskData taskData);
        Task DeleteScheduledTaskAsync(string taskId);//для единообразности можно передавать объект ScheduledTaskData, но и так сойдет
        Task<List<ScheduledTaskData>> LoadScheduledTasksAsync();
    }
}
