using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Commands.Interfaces;

namespace ArduinoTelegramBot.Commands.Demonstration;

public class PublicCommand : ICommand
{
    public string Name { get; set; } = "/упс";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "Это публичная команда, она может быть выполнена без авторизации");
    }

    public static PublicCommand Create(string name)
    {
        return new PublicCommand()
        {
            Name = name
        };
    }
}