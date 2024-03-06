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
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _commands = serviceProvider.GetServices<ICommand>()
            .Concat(serviceProvider.GetServices<IAuthorizedCommand>())
            .Distinct()
            .ToDictionary(c => c.Name.ToLower(), c => c);
    }

    public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message)
    {
        var commandText = message.Text.Split(' ')[0].ToLower();
        if (!_commands.TryGetValue(commandText, out var command))
        {
            await HandleDefaultCommandAsync(botClient, message);
            return;
        }

        Log.Information("Обработчик команд: Обработка команды: {Name}, chatId: {ChatId}", command.Name, message.Chat.Id);
        if (command is IAuthorizedCommand authorizedCommand)
        {
            if (!await CheckUserAuthorization(message.Chat.Id, authorizedCommand.Name))
            {
                await SendAuthorizationFailureMessage(botClient, message.Chat.Id, authorizedCommand.Name);
                return;
            }
        }

        await ExecuteCommandAsync(command, botClient, message);
    }

    private async Task<bool> CheckUserAuthorization(long chatId, string commandName)
    {
        var isAuthorized = await _authorizationService.CheckUserAuthorization(chatId, commandName);
        if (!isAuthorized)
        {
            Log.Warning("Обработчик команд: Пользователь {ChatId} пытался выполнить команду без достаточных прав: {CommandName}", chatId, commandName);
            return false;
        }

        return true;
    }

    private async Task SendAuthorizationFailureMessage(ITelegramBotClient botClient, long chatId, string commandName)
    {
        var responseMessage = $"У вас недостаточно прав для выполнения команды {commandName}.";
        await botClient.SendTextMessageAsync(chatId, responseMessage);
        Log.Warning("Обработчик команд: Отправлено сообщение о недостаточных правах пользователю {ChatId} для команды {CommandName}", chatId, commandName);
    }

    private async Task ExecuteCommandAsync(ICommand command, ITelegramBotClient botClient, Message message)
    {
        await command.ExecuteAsync(botClient, message);
        Log.Information("Обработчик команд: Команда успешно выполнена: {Name}, chatId: {ChatId}", command.Name, message.Chat.Id);
    }

    private async Task HandleDefaultCommandAsync(ITelegramBotClient botClient, Message message)
    {
        var defaultCommand = _serviceProvider.GetRequiredService<DefaultCommand>();
        if (defaultCommand != null)
        {
            await defaultCommand.ExecuteAsync(botClient, message);
            Log.Information("Обработчик команд: Выполнена команда по умолчанию, chatId: {ChatId}", message.Chat.Id);
        }
        else
        {
            const string responseMessage = "Команда не распознана, команда по умолчанию не установлена.";
            await botClient.SendTextMessageAsync(message.Chat.Id, responseMessage);
            Log.Warning("Обработчик команд: Попытка выполнения неизвестной команды, команда по умолчанию не установлена, chatId: {ChatId}", message.Chat.Id);
        }
    }
}