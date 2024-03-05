using ArduinoTelegramBot.Handlers.Interfaces;
using Telegram.Bot;
using Serilog;
using ArduinoTelegramBot.Processors.Arduino.Interfaces;
using ArduinoTelegramBot.Processors.Arduino;
using Microsoft.Extensions.DependencyInjection;
using ArduinoTelegramBot.Models;

namespace ArduinoTelegramBot.Handlers
{
    public class SerialDataHandler : ISerialDataHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly List<ArduinoProcessorIndex> _processorIndexes;
        private readonly IServiceProvider _serviceProvider;

        public SerialDataHandler(ITelegramBotClient botClient, IEnumerable<ISerialDataProcessor> processors, IServiceProvider serviceProvider)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _processorIndexes = processors.Select(p => new ArduinoProcessorIndex(p.Pattern, p)).ToList();
        }

        public async Task HandleReceivedDataAsync(string data, long chatId)
        {
            Log.Information("Обработчик входящих данных SerialPort: Получены данные {data} для {chatId}", data, chatId);

            var processorIndex = _processorIndexes.FirstOrDefault(pi => pi.Pattern.IsMatch(data));
            if (processorIndex != null)
            {
                await processorIndex.Processor.ProcessAsync(data, chatId, _botClient);
                Log.Information("Обработчик входящих данных SerialPort: Данные обработаны процессором {ProcessorName} для {chatId}", processorIndex.Processor.GetType().Name, chatId);
            }
            else
            {
                //пробуем получить дефолтный процессор. если он зареган в програм.цс должно сработать, иначе ошибка где-то здесь
                var defaultProcessor = _serviceProvider.GetService<DefaultDataProcessor>();
                if (defaultProcessor != null)
                {
                    await defaultProcessor.ProcessAsync(data, chatId, _botClient);
                    Log.Information("Обработчик входящих данных SerialPort: Обработаны данные дефолтным процессором для {chatId}", chatId);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Получен неизвестный тип данных из последовательного порта, процессор по умолчанию не установлен: " + data);
                    Log.Warning("Обработчик входящих данных SerialPort: Получен неизвестный тип данных для {chatId}, процессор по умолчанию не установлен", chatId);
                }
            }
        }
    }
}
