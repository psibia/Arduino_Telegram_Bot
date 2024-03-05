using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Commands.Interfaces;

namespace ArduinoTelegramBot.Commands.System;

public class DefaultCommand : ICommand
{
    public string Name { get; set; } = "/default";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "Команда не распознана.");
    }

    public static DefaultCommand Create(string name)
    {
        return new DefaultCommand()
        {
            Name = name
        };
    }
}