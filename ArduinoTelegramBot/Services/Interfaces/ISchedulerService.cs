using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISchedulerService
    {
        public OperationResult ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval);
        OperationResult ScheduleDailyTask(IAuthorizedCommand command, string chatId, TimeSpan dailyTime);
        OperationResult CancelScheduledCommand(string commandName);
        OperationResult CancelScheduledDailyTask(string commandName, TimeSpan taskTime);
        OperationResult CancelAllScheduledTasks(string commandName);
    }
}
