using ArduinoTelegramBot.Models;
using ArduinoTelegramBot.Models.Sheduler;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface IPermissionsDatabaseService
    {
        Task<(string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits)> LoadSerialPortConfigAsync();
        Task SaveSerialPortConfigAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task SaveSubscriptionsAsync(Dictionary<string, HashSet<long>> subscriptions);
        Task<Dictionary<string, HashSet<long>>> LoadSubscriptionsAsync();
        Task SaveScheduledTaskAsync(ScheduledLoadFromDBTaskInfo taskData);
        Task DeleteScheduledTaskAsync(string taskId);//для единообразности можно передавать объект ScheduledTaskData, но и так сойдет
        Task<List<ScheduledLoadFromDBTaskInfo>> LoadScheduledTasksAsync();
    }
}
