using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class GetTemperatureDataCommand : IAuthorizedCommand
    {
        public string Name { get; set; }
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;

        public GetTemperatureDataCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }

        public static GetTemperatureDataCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new GetTemperatureDataCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            var result = await _serialPortService.SendDataAsync("temperature", message.Chat.Id);
            if (result.Success)
            {
                //await botClient.SendTextMessageAsync(message.Chat.Id, "Данные успешно отправлены в SerialPort.");
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
            }
        }
    }
}
