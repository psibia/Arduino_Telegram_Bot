using System.IO.Ports;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface IPermissionsDatabaseService
    {
        Task<List<string>> GetPermissionsAsync(string key);
        Task<Dictionary<long, string>> LoadUserKeysAsync();
        Task SaveUserKeysAsync(Dictionary<long, string> userKeys);
        Task<(string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits)> LoadSerialPortConfigAsync();
        Task SaveSerialPortConfigAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
    }
}
