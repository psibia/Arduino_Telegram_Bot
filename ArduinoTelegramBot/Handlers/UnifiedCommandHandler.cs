using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Dialogs.Interfaces;
using ArduinoTelegramBot.Handlers.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace ArduinoTelegramBot.Handlers;

public class UnifiedCommandHandler : ICommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserAuthorizationService _authorizationService;
    private readonly Dictionary<string, Func<IDialog>> _dialogFactories;
    private readonly Dictionary<string, Func<ICommand>> _commandFactories;
    private readonly Dictionary<long, IDialog> _activeDialogs = new Dictionary<long, IDialog>();

    public UnifiedCommandHandler(IServiceProvider serviceProvider, IUserAuthorizationService authorizationService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

        // Собираем команды
        _commandFactories = serviceProvider.GetServices<IAuthorizedCommand>()
            .Concat(serviceProvider.GetServices<ICommand>()).Distinct().ToDictionary(
                c => c.Name.ToLower(),
                c => (Func<ICommand>)(() => serviceProvider.GetServices<IAuthorizedCommand>().Concat(serviceProvider.GetServices<ICommand>()).FirstOrDefault(cmd => cmd.GetType() == c.GetType())));

        // Собираем диалоги
        _dialogFactories = serviceProvider.GetServices<IDialog>()
            .Concat(serviceProvider.GetServices<IAuthorizedDialog>()).Distinct().ToDictionary(
                d => d.Name.ToLower(),
                d => (Func<IDialog>)(() => serviceProvider.GetServices<IDialog>().Concat(serviceProvider.GetServices<IAuthorizedDialog>()).FirstOrDefault(dlg => dlg.GetType() == d.GetType())));
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        if (_activeDialogs.TryGetValue(chatId, out var activeDialog))
        {
            await activeDialog.HandleCallbackQueryAsync(callbackQuery);
        }
    }

    public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message)
    {
        try
        {
            var chatId = message.Chat.Id;
            var messageText = message.Text.Trim();
            var commandText = messageText.Split(' ')[0].ToLower();
            var args = messageText.Split(' ').Skip(1).ToArray();
            var hasArgs = args.Any();

            // Проверяем активные диалоги
            if (_activeDialogs.TryGetValue(chatId, out var activeDialog) && !activeDialog.IsComplete)
            {
                await activeDialog.NextAsync(botClient, message);
                if (activeDialog.IsComplete)
                {
                    _activeDialogs.Remove(chatId);
                }
                return;
            }

            // Поиск среди диалогов, если нет аргументов
            if (!hasArgs && _dialogFactories.TryGetValue(commandText, out var dialogFactory))
            {
                var dialog = dialogFactory();

                // Проверка авторизации
                if (dialog is IAuthorizedDialog authorizedDialog && !await _authorizationService.CheckUserAuthorization(chatId, authorizedDialog.Name))
                {
                    await botClient.SendTextMessageAsync(chatId, "Недостаточно прав для выполнения диалога.");
                    return;
                }

                _activeDialogs[chatId] = dialog;
                await dialog.StartAsync(botClient, message);
                return;
            }

            // Поиск среди команд, если есть аргументы или диалог не найден
            if (_commandFactories.TryGetValue(commandText, out var commandFactory))
            {
                var command = commandFactory();

                // Проверка авторизации
                if (command is IAuthorizedCommand authorizedCommand && !await _authorizationService.CheckUserAuthorization(chatId, authorizedCommand.Name))
                {
                    await botClient.SendTextMessageAsync(chatId, "Недостаточно прав для выполнения команды.");
                    return;
                }

                await command.ExecuteAsync(botClient, message);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Команда или диалог не распознаны.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Произошла ошибка при обработке команды/диалога.");
            await botClient.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка при выполнении вашей команды/диалога.");
        }
    }
}
