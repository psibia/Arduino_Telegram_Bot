using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Commands.System;
using ArduinoTelegramBot.Handlers.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace ArduinoTelegramBot.Handlers;

public class CommandHandler : ICommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserAuthorizationService _authorizationService;
    private readonly Dictionary<string, ICommand> _commands;

    public CommandHandler(IServiceProvider serviceProvider, IUserAuthorizationService authorizationService)
    {
        _serviceProvider = serviceProvider;
        _authorizationService = authorizationService;
        _commands = serviceProvider.GetServices<ICommand>().Concat(serviceProvider.GetServices<IAuthorizedCommand>()).Distinct().ToDictionary(c => c.Name.ToLower(), c => c);
    }

    public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message)
    {
        var commandText = message.Text.Split(' ')[0].ToLower();

        if (_commands.TryGetValue(commandText, out var command))
        {
            Log.Information("Обработчик команд: Обработка команды: {name}", command.Name);

            if (command is IAuthorizedCommand authorizedCommand)
            {
                var isAuthorized = await _authorizationService.IsAuthorized(message.Chat.Id, authorizedCommand.Name);
                if (!isAuthorized)
                {
                    var responseMessage = $"У вас недостаточно прав для выполнения команды {command.Name}.";
                    Log.Warning("Обработчик команд: Пользователь {id} пытался выполнить команду без достаточных прав: {name}", message.Chat.Id, authorizedCommand.Name);
                    await botClient.SendTextMessageAsync(message.Chat.Id, responseMessage);
                    return;
                }
            }

            await command.ExecuteAsync(botClient, message);
            Log.Information("Обработчик команд: Команда успешно выполнена: {name}", command.Name);
        }
        else
        {
            await HandleDefaultCommandAsync(botClient, message);
        }
    }

    private async Task HandleDefaultCommandAsync(ITelegramBotClient botClient, Message message)
    {
        //получение всех команд, в том числе дефолтной
        var defaultCommand = _serviceProvider.GetServices<ICommand>().OfType<DefaultCommand>().FirstOrDefault();

        if (defaultCommand != null)
        {
            await defaultCommand.ExecuteAsync(botClient, message);
            Log.Information("Обработчик команд: Выполнена команда по умолчанию");
        }
        else
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "Команда не распознана, команда по умолчанию не установлена.");
            Log.Information("Обработчик команд: Попытка выполнения неизвестной команды, команда по умолчанию не установлена");
        }
    }
}