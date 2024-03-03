using ArduinoTelegramBot.Handlers.Interfaces;
using ArduinoTelegramBot.Models;
using ArduinoTelegramBot.Services.Interfaces;
using Serilog;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services
{
    public class SerialPortService : ISerialPortService
    {
        private SerialPort _serialPort = new SerialPort();
        private Action<string, long> _onDataReceived;
        private ISerialDataHandler _dataHandler;
        public string CurrentPortName() => _serialPort.IsOpen ? _serialPort.PortName : null;


        public void Initialize(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            if (_serialPort.IsOpen)
            {
                if (_serialPort.PortName == portName && _serialPort.BaudRate == baudRate &&
                    _serialPort.Parity == parity && _serialPort.DataBits == dataBits &&
                    _serialPort.StopBits == stopBits)
                {
                    Log.Information("Сервис последовательного порта: SerialPort уже открыт с этими параметрами.");
                    throw new InvalidOperationException("SerialPort уже открыт с этими параметрами.");
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
        }

        public void ActivateDataReceiving(Action<string, long> onDataReceived)
        {
            _onDataReceived = onDataReceived;

            _serialPort.DataReceived += (sender, e) =>
            {
                string data = _serialPort.ReadExisting();
                _onDataReceived?.Invoke(data, 0); // 0 - плейсхолдер для, будет задан в команде
            };

            Log.Information("Сервис последовательного порта: SerialPort настроен для получения данных.");
        }

        public async Task<ActionStatusResult> ListAvailablePortsAsync()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                Log.Information("Сервис последовательного порта: Запрос доступных портов выполнен.");
                return ActionStatusResult.Ok("Доступные порты получены.", ports);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Сервис последовательного порта: Ошибка при получении списка доступных портов.");
                return ActionStatusResult.Error("Ошибка при получении списка доступных портов.");
            }
        }

        public async Task<ActionStatusResult> OpenPortAsync()
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                Log.Information("Сервис последовательного порта: SerialPort {sp} открыт.", _serialPort.PortName);
                return ActionStatusResult.Ok($"Порт {_serialPort.PortName} открыт.");
            }
            else
            {
                Log.Warning("Сервис последовательного порта: SerialPort {sp} уже открыт.", _serialPort.PortName);
                return ActionStatusResult.Error($"Порт {_serialPort.PortName} уже открыт.");
            }
        }

        public async Task<ActionStatusResult> TryOpenPortAsync()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    Log.Information($"Сервис последовательного порта: SerialPort успешно открыт: {_serialPort.PortName}, {_serialPort.BaudRate}, {_serialPort.Parity}, {_serialPort.DataBits}, {_serialPort.StopBits}");
                    return ActionStatusResult.Ok($"Порт успешно открыт с параметрами: {_serialPort.PortName}, {_serialPort.BaudRate}, {_serialPort.Parity}, {_serialPort.DataBits}, {_serialPort.StopBits}");
                }
                else
                {
                    return ActionStatusResult.Error($"Порт {_serialPort.PortName} уже открыт.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Сервис последовательного порта: Ошибка при открытии SerialPort: {ex}", ex.Message);
                return ActionStatusResult.Error($"Ошибка при открытии порта: {ex.Message}");
            }
        }


        public SerialPort CurrentSerialPort() => _serialPort;

        public async Task<ActionStatusResult> ClosePortAsync()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                Log.Information("Сервис последовательного порта: SerialPort {sp} закрыт", _serialPort.PortName);
                return ActionStatusResult.Ok($"Порт {_serialPort.PortName} успешно закрыт.");
            }
            else
            {
                Log.Information("Сервис последовательного порта: SerialPort {sp} уже закрыт или не был открыт.", _serialPort.PortName);
                return ActionStatusResult.Error($"Порт {_serialPort.PortName} уже закрыт или не был открыт.");
            }
        }


        public async Task<ActionStatusResult> SendDataAsync(string data)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Write(data);
                Log.Information("Сервис последовательного порта: Данные отправлены в SerialPort {sp}: {data}", _serialPort.PortName, data);
                return ActionStatusResult.Ok("Данные отправлены.");
            }
            else
            {
                Log.Warning("Сервис последовательного порта: Попытка отправить данные в SerialPort завершилась неудачей - порт не открыт.");
                return ActionStatusResult.Error($"Порт не открыт.");
            }
        }
    }
}
