using ArduinoTelegramBot.Handlers.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
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
        Log.Information("Сервис телеграм-бота: Запуск приема сообщений...");

        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() }, stoppingToken);

        stoppingToken.Register(() => Log.Information("Сервис телеграм-бота: Остановка приема сообщений..."));
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type == UpdateType.Message)
            {
                Log.Debug("Сервис телеграм-бота: Получено сообщение от {id}: {text}", update.Message.From.Id, update.Message.Text);
                await _commandHandler.HandleCommandAsync(botClient, update.Message);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Сервис телеграм-бота: Ошибка при обработке сообщения");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Log.Error(exception, "Сервис телеграм-бота: Произошла ошибка при приеме обновления");
        return Task.CompletedTask;
    }
}