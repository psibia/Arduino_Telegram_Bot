using ArduinoTelegramBot.Handlers.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ArduinoTelegramBot.Services;

public class BotService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ICommandHandler _commandHandler;

    public BotService(ITelegramBotClient botClient, ICommandHandler commandHandler)
    {
        _botClient = botClient;
        _commandHandler = commandHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Сервис телеграм-бота: Запуск приема сообщений");

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = { } }, // Подписываемся на все типы обновлений
            stoppingToken
        );

        stoppingToken.Register(() => Log.Information("Сервис телеграм-бота: Остановка приема сообщений"));
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type == UpdateType.Message)
            {
                Log.Debug("Сервис телеграм-бота: Получено сообщение от {id}: {text}", update.Message.From.Id, update.Message.Text);
                await _commandHandler.HandleCommandAsync(botClient, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                Log.Debug("Сервис телеграм-бота: Получен CallbackQuery от {id}: {data}", update.CallbackQuery.From.Id, update.CallbackQuery.Data);
                await _commandHandler.HandleCallbackQueryAsync(botClient, update.CallbackQuery);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Сервис телеграм-бота: Ошибка при обработке обновления");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Log.Error(exception, "Сервис телеграм-бота: Произошла ошибка при приеме обновления");
        return Task.CompletedTask;
    }
}