using ArduinoTelegramBot.Models;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISerialPortService
    {
        void Initialize(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task<SerialPortOperationResult> ListAvailablePortsAsync();
        Task<SerialPortOperationResult> ClosePortAsync();
        Task<SerialPortOperationResult> SendDataAsync(string data);
        Task<SerialPortOperationResult> SendBinaryDataAsync(byte[] data);
        void ActivateDataReceiving(Action<string, long> onDataReceived);
        Task<SerialPortOperationResult> TryOpenPortAsync();
        SerialPort CurrentSerialPort();
    }
}
