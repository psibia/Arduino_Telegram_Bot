using ArduinoTelegramBot.Models;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISerialPortService
    {
        void Initialize(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task<ActionStatusResult> ListAvailablePortsAsync();
        Task<ActionStatusResult> OpenPortAsync();
        Task<ActionStatusResult> ClosePortAsync();
        Task<ActionStatusResult> SendDataAsync(string data);
        void ActivateDataReceiving(Action<string, long> onDataReceived);
        Task<ActionStatusResult> TryOpenPortAsync();
        SerialPort CurrentSerialPort();
    }
}
