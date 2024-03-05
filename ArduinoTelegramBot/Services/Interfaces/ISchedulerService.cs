using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISchedulerService
    {
        ScheduleOperationResult ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval);
        ScheduleOperationResult ScheduleDailyTask(IAuthorizedCommand command, string chatId, TimeSpan dailyTime);
        ScheduleOperationResult CancelScheduledCommand(string commandName, string chatId);
        ScheduleOperationResult CancelScheduledDailyTask(string commandName, TimeSpan taskTime);
        ScheduleOperationResult CancelAllScheduledTasks(string commandName);
        List<ScheduledTaskInfo> GetScheduledTasksForChat(string chatId);
    }
}
