using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Models.Sheduler;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISchedulerService
    {
        Task<ScheduleOperationResult> ScheduleCycleTaskAsync(IAuthorizedCommand command, long chatId, TimeSpan interval, bool isStartupLoad = false, string taskId = null);
        Task<ScheduleOperationResult> ScheduleDailyTaskAsync(IAuthorizedCommand command, long chatId, TimeSpan dailyTime, bool isStartupLoad = false, string taskId = null);
        Task<ScheduleOperationResult> CancelScheduledCycleTaskAsync(string commandName, long chatId);
        Task<ScheduleOperationResult> CancelScheduledDailyTaskAsync(string commandName, long chatId, TimeSpan taskTime);
        Task<ScheduleOperationResult> CancelAllScheduledTasksAsync(string commandName, long chatId);
        Task<List<ScheduledTaskInfo>> GetScheduledTasksForChatAsync(long chatId);
    }
}
