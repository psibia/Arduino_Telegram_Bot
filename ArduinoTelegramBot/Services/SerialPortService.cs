using ArduinoTelegramBot.Handlers.Interfaces;
using ArduinoTelegramBot.Models;
using ArduinoTelegramBot.Services.Interfaces;
using Serilog;
using System.IO.Ports;
using Telegram.Bot.Types;

namespace ArduinoTelegramBot.Services
{
    public class SerialPortService : ISerialPortService
    {
        private SerialPort _serialPort = new SerialPort();
        private Action<string, long> _onDataReceived;
        private ISerialDataHandler _dataHandler;
        private readonly Dictionary<string, long> _requestGuidToChatIdMap = new();
        private readonly HashSet<long> _subscribedChatIds = new HashSet<long>();


        public SerialPort CurrentSerialPort() => _serialPort;

        public async Task<SerialPortOperationResult> InitializeAndOpenAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            if (_serialPort.IsOpen)
            {
                if (_serialPort.PortName == portName && _serialPort.BaudRate == baudRate &&
                    _serialPort.Parity == parity && _serialPort.DataBits == dataBits &&
                    _serialPort.StopBits == stopBits)
                {
                    Log.Information("Сервис последовательного порта: SerialPort уже открыт с этими параметрами.");
                    return SerialPortOperationResult.Error("SerialPort уже открыт с этими параметрами.");
                }
                else
                {
                    _serialPort.Close();
                }
            }

            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;
            _serialPort.Parity = parity;
            _serialPort.DataBits = dataBits;
            _serialPort.StopBits = stopBits;
            Log.Information($"Сервис последовательного порта: SerialPort инициализирован: {portName}, {baudRate}, {parity.ToString()}, {dataBits}, {stopBits.ToString()}");

            return await TryOpenPortAsync();
        }

        public async Task<SerialPortOperationResult> Subscribe(long chatId)
        {
            bool added = _subscribedChatIds.Add(chatId);
            if (added)
            {
                Log.Information($"ChatId {chatId} подписался на уведомления.");
                return SerialPortOperationResult.Ok($"ChatId {chatId} успешно подписался на уведомления.");
            }
            else
            {
                return SerialPortOperationResult.Error($"ChatId {chatId} уже подписан на уведомления.");
            }
        }

        public async Task<SerialPortOperationResult> Unsubscribe(long chatId)
        {
            bool removed = _subscribedChatIds.Remove(chatId);
            if (removed)
            {
                Log.Information($"ChatId {chatId} отписался от уведомлений.");
                return SerialPortOperationResult.Ok($"ChatId {chatId} успешно отписался от уведомлений.");
            }
            else
            {
                return SerialPortOperationResult.Error($"ChatId {chatId} не найден среди подписчиков.");
            }
        }

        public void ActivateDataReceiving(Action<string, long> onDataReceived)
        {
            _onDataReceived = onDataReceived;
            _serialPort.DataReceived += (sender, e) =>
            {
                string data = _serialPort.ReadExisting();
                string guid = ExtractGuid(data);
                if (!string.IsNullOrEmpty(guid) && _requestGuidToChatIdMap.TryGetValue(guid, out long chatId))
                {
                    string actualData = data.Substring(guid.Length + 1); //удаляем GUID и двоеточие из данных
                    _onDataReceived?.Invoke(actualData, chatId);
                    _requestGuidToChatIdMap.Remove(guid);
                }
                else if (guid == null) //если gui нет, то это уведомление, которое отправляем всем подписчикам
                {
                    foreach (var subscribedChatId in _subscribedChatIds)
                    {
                        _onDataReceived?.Invoke(data, subscribedChatId);
                    }
                }
            };
        }

        public async Task<SerialPortOperationResult> ListAvailablePortsAsync()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                Log.Information("Сервис последовательного порта: Запрос доступных портов выполнен.");
                return SerialPortOperationResult.Ok("Доступные порты получены.", ports);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Сервис последовательного порта: Ошибка при получении списка доступных портов.");
                return SerialPortOperationResult.Error("Ошибка при получении списка доступных портов.");
            }
        }

        public async Task<SerialPortOperationResult> ClosePortAsync()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                Log.Information("Сервис последовательного порта: SerialPort {sp} закрыт", _serialPort.PortName);
                return SerialPortOperationResult.Ok($"Порт {_serialPort.PortName} успешно закрыт.");
            }
            else
            {
                Log.Information("Сервис последовательного порта: SerialPort {sp} уже закрыт или не был открыт.", _serialPort.PortName);
                return SerialPortOperationResult.Error($"Порт {_serialPort.PortName} уже закрыт или не был открыт.");
            }
        }


        public async Task<SerialPortOperationResult> SendDataAsync(string data, long chatId)
        {
            try
            {
                string guid = Guid.NewGuid().ToString();
                _requestGuidToChatIdMap[guid] = chatId;

                string dataWithGuid = $"{guid}:{data}";
                if (_serialPort.IsOpen)
                {
                    _serialPort.Write(dataWithGuid);
                    Log.Information($"Данные с GUID отправлены: {dataWithGuid}");
                    return SerialPortOperationResult.Ok("Данные отправлены.");
                }
                else
                {
                    return SerialPortOperationResult.Error("SerialPort не открыт.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при отправке данных: {Message}", ex.Message);
                return SerialPortOperationResult.Error($"Ошибка при отправке данных: {ex.Message}");
            }
        }


        private async Task<SerialPortOperationResult> TryOpenPortAsync()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    Log.Information($"Сервис последовательного порта: SerialPort успешно открыт: {_serialPort.PortName}, {_serialPort.BaudRate}, {_serialPort.Parity}, {_serialPort.DataBits}, {_serialPort.StopBits}");
                    return SerialPortOperationResult.Ok($"Порт успешно открыт с параметрами: {_serialPort.PortName}, {_serialPort.BaudRate}, {_serialPort.Parity}, {_serialPort.DataBits}, {_serialPort.StopBits}");
                }
                else
                {
                    return SerialPortOperationResult.Error($"Порт {_serialPort.PortName} уже открыт.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Сервис последовательного порта: Ошибка при открытии SerialPort: {ex.Message}");
                return SerialPortOperationResult.Error($"Ошибка при открытии порта: {ex.Message}");
            }
        }

        private string ExtractGuid(string data)
        {
            int colonIndex = data.IndexOf(':');
            if (colonIndex > 0)
            {
                return data.Substring(0, colonIndex);
            }
            return null; // или throw new Exception("GUID не найден"), в зависимости от предпочтений обработки ошибок. Пока и такй сойдет на время тестов
        }
    }
}
