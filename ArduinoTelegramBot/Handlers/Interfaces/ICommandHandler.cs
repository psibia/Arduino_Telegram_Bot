using Telegram.Bot;
using Telegram.Bot.Types;

namespace ArduinoTelegramBot.Handlers.Interfaces
{
    public interface ICommandHandler
    {
        Task HandleCommandAsync(ITelegramBotClient botClient, Message message);
    }
}
