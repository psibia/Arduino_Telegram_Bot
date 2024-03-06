using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISchedulerService
    {
        ScheduleOperationResult ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval, bool isStartupLoad = false, string taskId = null);
        ScheduleOperationResult ScheduleDailyTask(IAuthorizedCommand command, string chatId, TimeSpan dailyTime, bool isStartupLoad = false, string taskId = null);
        ScheduleOperationResult CancelScheduledCommand(string commandName, string chatId);
        ScheduleOperationResult CancelScheduledDailyTask(string commandName, string chatId, TimeSpan taskTime);
        ScheduleOperationResult CancelAllScheduledTasks(string commandName, string chatId);
        List<ScheduledTaskInfo> GetScheduledTasksForChat(string chatId);
    }
}
