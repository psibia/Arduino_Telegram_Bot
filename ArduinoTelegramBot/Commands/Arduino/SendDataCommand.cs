using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.Extensions.DependencyInjection;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class SendDataCommand : IAuthorizedCommand
    {
        public string Name { get; set; } = "/упс";
        private readonly ITelegramBotClient _botClient;
        private readonly ISerialPortService _serialPortService;

        public SendDataCommand(ITelegramBotClient botClient, ISerialPortService serialPortService)
        {
            _botClient = botClient;
            _serialPortService = serialPortService;
        }

        public static SendDataCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new SendDataCommand(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<ISerialPortService>())
            {
                Name = name
            };
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            //мзвлекаем данные для отправки из сообщения
            var dataToSend = message.Text.Substring(Name.Length).Trim();

            if (string.IsNullOrEmpty(dataToSend))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, укажите данные для отправки после команды /serial.");
                return;
            }

            var result = await _serialPortService.SendDataAsync(dataToSend);
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
