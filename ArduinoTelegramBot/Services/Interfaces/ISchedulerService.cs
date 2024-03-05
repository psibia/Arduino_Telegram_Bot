using ArduinoTelegramBot.Commands.Interfaces;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISchedulerService
    {
        void ScheduleCommand(IAuthorizedCommand command, string chatId, TimeSpan interval);
    }
}
