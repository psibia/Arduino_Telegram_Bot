﻿using ArduinoTelegramBot.Models;
using System.IO.Ports;

namespace ArduinoTelegramBot.Services.Interfaces
{
    public interface ISerialPortService
    {
        Task<SerialPortOperationResult> InitializeAndOpenAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task<SerialPortOperationResult> ListAvailablePortsAsync();
        Task<SerialPortOperationResult> ClosePortAsync();
        Task<SerialPortOperationResult> SendDataAsync(string data, long chatId);
        void ActivateDataReceiving(Action<string, long> onDataReceived);
        SerialPort CurrentSerialPort();

        Task<SerialPortOperationResult> Subscribe(long chatId);
        Task<SerialPortOperationResult> Unsubscribe(long chatId);
    }
}
