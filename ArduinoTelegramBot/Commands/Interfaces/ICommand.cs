using Telegram.Bot.Types;
using Telegram.Bot;

namespace ArduinoTelegramBot.Commands.Interfaces;

public interface ICommand
{
    string Name { get; set; }
    Task ExecuteAsync(ITelegramBotClient botClient, Message message);
}
