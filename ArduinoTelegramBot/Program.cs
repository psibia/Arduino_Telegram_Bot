using ArduinoTelegramBot.Commands.System;
using ArduinoTelegramBot.Commands.Demonstration;
using ArduinoTelegramBot.Handlers;
using ArduinoTelegramBot.Services;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Commands.Arduino;
using ArduinoTelegramBot.Handlers.Interfaces;
using ArduinoTelegramBot.Processors.Arduino.Interfaces;
using ArduinoTelegramBot.Processors.Arduino;

namespace ArduinoTelegramBot;

class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        Log.Information("Program: Запуск приложения");

        try
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Program: Произошла ошибка при запуске приложения");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
    {
        Log.Debug("Program: Настройка сервисов");

        services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient("7115970409:AAFBIPevz5fhaHD-BNskOJJZoMUWbckV5SQ"));
        services.AddSingleton<IUserAuthorizationService, UserAuthorizationService>();
        
        services.AddSingleton<ICommandHandler, CommandHandler>();
        services.AddSingleton<IPermissionsDatabaseService, PermissionsDatabaseService>();
        services.AddSingleton<ISerialPortService, SerialPortService>();
        services.AddSingleton<ISerialDataHandler, SerialDataHandler>();
        services.AddSingleton<ISchedulerService, SchedulerService>();
        services.AddTransient<ICommandFactory, CommandFactory>();

        #region команды не требующие авторизации
        services.AddTransient<ICommand>(serviceProvider => DefaultCommand.Create("/default"));
        services.AddTransient<ICommand>(serviceProvider => AuthorizationCommand.Create(serviceProvider, "/auth"));
        services.AddTransient<ICommand>(serviceProvider => PublicCommand.Create("/public"));
        services.AddTransient<ICommand>(serviceProvider => HelpCommand.Create(serviceProvider, "/help"));
        #endregion
        #region команды, для выполнения которых нужна авторизация
        services.AddTransient<IAuthorizedCommand>(serviceProvider => AdminCommand.Create("/admin"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => UserCommand.Create("/user"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => GetSerialPortsCommand.Create(serviceProvider, "/get_serial"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => StartSerialPortCommand.Create(serviceProvider, "/start_serial"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => SendDataCommand.Create(serviceProvider, "/serial"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => GetTemperatureDataCommand.Create(serviceProvider, "/temp"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => CloseSerialPortCommand.Create(serviceProvider, "/close_serial"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => ScheduleCommand.Create(serviceProvider, "/shedule"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => NotificationSubscriberCommand.Create(serviceProvider, "/subscribe"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => NotificationUnsubscriberCommand.Create(serviceProvider, "/unsubscribe"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => ShowSubscriptionsCommand.Create(serviceProvider, "/subscribe_get"));
        services.AddTransient<IAuthorizedCommand>(serviceProvider => TestFirstCommand.Create(serviceProvider, "/команда"));
        #endregion
        #region обработчики полученных данных с ардуинки (процессоры)
        services.AddTransient<ISerialDataProcessor, TemperatureDataProcessor>();
        services.AddTransient<ISerialDataProcessor, ErrorDataProcessor>();
        services.AddTransient<ISerialDataProcessor, DefaultDataProcessor>(); //обязательно оставить в самом низу списка процессоров, так как регулярка этого процессора подходит под все полученные данные. Те процессоры, что расположены ниже этого работать не будут, так как выполнится этот
        #endregion
        services.AddHostedService<BotService>();//не пытайся сделать интерфейс, там гемор
        services.AddHostedService(provider => provider.GetRequiredService<ISchedulerService>() as SchedulerService);

        Log.Debug("Program: Сервисы настроены");
    });
}