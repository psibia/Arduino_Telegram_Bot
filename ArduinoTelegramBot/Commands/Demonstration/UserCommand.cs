using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Commands.Interfaces;

namespace ArduinoTelegramBot.Commands.Demonstration;

public class UserCommand : IAuthorizedCommand
{
    public string Name { get; set; } = "/упс";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "Это юзерская команда");
    }

    public static UserCommand Create(string name)
    {
        return new UserCommand()
        {
            Name = name
        };
    }
}