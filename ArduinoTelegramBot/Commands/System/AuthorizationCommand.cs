using ArduinoTelegramBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Commands.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ArduinoTelegramBot.Commands.System;

public class AuthorizationCommand : ICommand
{
    private readonly IUserAuthorizationService authorizationService;

    public string Name { get; set; } = "/auth";

    public AuthorizationCommand(IUserAuthorizationService authorizationService)
    {
        this.authorizationService = authorizationService;
    }

    public static AuthorizationCommand Create(IServiceProvider serviceProvider, string name)
    {
        return new AuthorizationCommand(
            serviceProvider.GetRequiredService<IUserAuthorizationService>())
        {
            Name = name
        };
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
    {
        var parts = message.Text.Split(' ');
        if (parts.Length == 2)
        {
            var key = parts[1];
            var result = await authorizationService.ProcessAuthorizationAttempt(key, message.Chat.Id);

            //Отправляем результат авторизации пользователю
            await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
        }
        else
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, $"Пожалуйста, предоставьте ключ. Команда должна выглядеть следующим образом: {this.Name} [ключ]");
        }
    }
}