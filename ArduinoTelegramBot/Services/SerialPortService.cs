using ArduinoTelegramBot.Handlers.Interfaces;
using ArduinoTelegramBot.Models;
using ArduinoTelegramBot.Services.Interfaces;
using Serilog;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;
using Telegram.Bot.Types;

namespace ArduinoTelegramBot.Services
{
    public class SerialPortService : ISerialPortService
    {
        private SerialPort _serialPort = new SerialPort();
        private Action<string, long> _onDataReceived;
        private ISerialDataHandler _dataHandler;
        private readonly Dictionary<string, long> _requestGuidToChatIdMap = new();
        private readonly Dictionary<string, HashSet<long>> _subscriptions = new Dictionary<string, HashSet<long>>();
        private IPermissionsDatabaseService _permissionsDatabaseService;

        public SerialPortService(IPermissionsDatabaseService permissionsDatabaseService)
        {
            _permissionsDatabaseService = permissionsDatabaseService;
            _subscriptions = _permissionsDatabaseService.LoadSubscriptionsAsync().Result;
        }
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
            Log.Information("Сервис последовательного порта: SerialPort инициализирован: {portName}, {baudRate}, {parity}, {dataBits}, {stopBits}", portName, baudRate, parity, dataBits, stopBits);
            return await TryOpenPortAsync();
        }

        public async Task<SerialPortOperationResult> Subscribe(long chatId, string command)
        {
            if (!_subscriptions.ContainsKey(command))
            {
                _subscriptions[command] = new HashSet<long>();
            }

            bool added = _subscriptions[command].Add(chatId);
            if (added)
            {
                await _permissionsDatabaseService.SaveSubscriptionsAsync(_subscriptions);
                Log.Information("Сервис последовательного порта: ChatId {chatId} подписался на уведомления {command}.", chatId, command);
                return SerialPortOperationResult.Ok($"Вы успешно подписались на уведомления с идентификатором {command}.");
            }
            else
            {
                return SerialPortOperationResult.Error($"Вы уже подписаны на уведомления с идентификатором {command}.");
            }
        }

        public async Task<SerialPortOperationResult> Unsubscribe(long chatId, string command)
        {
            if (_subscriptions.ContainsKey(command) && _subscriptions[command].Remove(chatId))
            {
                if (_subscriptions[command].Count == 0)
                {
                    _subscriptions.Remove(command);
                }
                await _permissionsDatabaseService.SaveSubscriptionsAsync(_subscriptions);
                Log.Information("Сервис последовательного порта: ChatId {chatId} отписался от уведомлений {command}", chatId, command);
                return SerialPortOperationResult.Ok($"Вы успешно отписались от уведомлений с идентификатором {command}.");
            }
            else
            {
                return SerialPortOperationResult.Error($"Вы не подписаны на уведомления с идентификатором {command}.");
            }
        }

        public async Task<List<string>> GetUserSubscriptionsAsync(long chatId)
        {
            List<string> userSubscriptions = new List<string>();

            foreach (var subscription in _subscriptions)
            {
                if (subscription.Value.Contains(chatId))
                {
                    userSubscriptions.Add(subscription.Key);
                }
            }

            return userSubscriptions;
        }

        public void ActivateDataReceiving(Action<string, long> onDataReceived)
        {
            _onDataReceived = onDataReceived;
            StringBuilder buffer = new StringBuilder(); // Буфер для накопления данных

            _serialPort.DataReceived += (sender, e) =>
            {
                string data = _serialPort.ReadExisting();
                buffer.Append(data);

                string bufferContent = buffer.ToString();
                int newLineIndex = bufferContent.IndexOf('\n');
                while (newLineIndex != -1)
                {
                    string message = bufferContent.Substring(0, newLineIndex).Trim();
                    string[] parts = message.Split(new[] { ':' }, 3);
                    if (parts.Length >= 3)
                    {
                        string type = parts[0];
                        string identifier = parts[1];
                        string content = parts[2];

                        if (type == "RES")
                        {
                            if (_requestGuidToChatIdMap.TryGetValue(identifier, out long chatId))
                            {
                                _onDataReceived?.Invoke(content, chatId);
                                _requestGuidToChatIdMap.Remove(identifier);
                            }
                        }
                        else if (type == "ERR")
                        {
                            if (_requestGuidToChatIdMap.TryGetValue(identifier, out long chatId))
                            {
                                Log.Error("Сервис последовательного порта: Ошибка на устройстве, подключенном к {PortName}. Код ошибки: {ErrorCode}", _serialPort.PortName, content);
                                _onDataReceived?.Invoke("Error: " + DescribeErrorCode(content), chatId);
                                _requestGuidToChatIdMap.Remove(identifier);
                            }
                        }
                        else if (type == "NOT")
                        {
                            if (_subscriptions.ContainsKey(identifier))
                            {
                                foreach (var subscribedChatId in _subscriptions[identifier])
                                {
                                    _onDataReceived?.Invoke(content, subscribedChatId);
                                }
                            }
                        }

                        buffer.Remove(0, newLineIndex + 1);
                        bufferContent = buffer.ToString();
                        newLineIndex = bufferContent.IndexOf('\n');
                    }
                    else
                    {
                        break; //если строка не содержит достаточно частей, прерываем обработку
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

                //форматирование данных с префиксом "REQ" для всех реквестов в сириал порт
                string dataWithGuidAndType = $"REQ:{guid}:{data}";
                if (_serialPort.IsOpen)
                {
                    _serialPort.Write(dataWithGuidAndType);
                    Log.Information("Сервис последовательного порта: Данные отправлены: {dataWithGuidAndType}", dataWithGuidAndType);
                    return SerialPortOperationResult.Ok("Данные отправлены.");
                }
                else
                {
                    return SerialPortOperationResult.Error("SerialPort не открыт.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Сервис последовательного порта: Ошибка при отправке данных: {Message}", ex.Message);
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

        private string DescribeErrorCode(string errorCode)
        {
            return errorCode switch
            {
                "001" => "Не удалось обработать переданные данные",
                "002" => "Описание ошибки с кодом 002",
                
                _ => "Неизвестный код ошибки"
            };
        }
    }
}
