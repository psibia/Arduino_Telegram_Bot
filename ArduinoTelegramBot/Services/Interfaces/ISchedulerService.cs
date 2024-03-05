using ArduinoTelegramBot.Commands.Interfaces;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISchedulerService
    {
        void ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval);
        void ScheduleDailyTask(IAuthorizedCommand command, string chatId, TimeSpan dailyTime);
        void CancelScheduledCommand(string commandName);
        void CancelScheduledDailyTask(string commandName, TimeSpan taskTime);
        void CancelAllScheduledTasks(string commandName);
    }
}
