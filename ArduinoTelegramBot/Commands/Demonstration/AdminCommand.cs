using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Commands.Interfaces;

namespace ArduinoTelegramBot.Commands.Demonstration;

public class AdminCommand : IAuthorizedCommand
{
    public string Name { get; set; } = "/упс";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "Это админская команда");
    }

    public static AdminCommand Create(string name)
    {
        return new AdminCommand()
        {
            Name = name
        };
    }
}