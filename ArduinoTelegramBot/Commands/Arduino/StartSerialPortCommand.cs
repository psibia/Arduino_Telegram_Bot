﻿using ArduinoTelegramBot.Commands.Interfaces;
using ArduinoTelegramBot.Handlers.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.IO.Ports;
using ArduinoTelegramBot.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ArduinoTelegramBot.Commands.Arduino
{
    public class StartSerialPortCommand : IAuthorizedCommand
    {
        public string Name { get; set; }
        private readonly ISerialPortService _serialPortService;
        private readonly ISerialDataHandler _dataHandler;
        private readonly ITelegramBotClient _botClient;
        private readonly IPermissionsDatabaseService _permissionsDatabase;

        public StartSerialPortCommand(ISerialPortService serialPortService, ISerialDataHandler dataHandler, ITelegramBotClient botClient, IPermissionsDatabaseService permissionsDatabase)
        {
            _serialPortService = serialPortService;
            _dataHandler = dataHandler;
            _botClient = botClient;
            _permissionsDatabase = permissionsDatabase;
        }

        public static StartSerialPortCommand Create(IServiceProvider serviceProvider, string name)
        {
            return new StartSerialPortCommand(
                serviceProvider.GetRequiredService<ISerialPortService>(),
                serviceProvider.GetRequiredService<ISerialDataHandler>(),
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetRequiredService<IPermissionsDatabaseService>())
            {
                Name = name
            };
        }

        public async Task ExecuteAsync(ITelegramBotClient botClient, Message message)
        {
            string[] args = message.Text.Split(' ');
            if (args.Length >= 2)
            {
                string portName = args[1];
                int baudRate = args.Length > 2 ? int.Parse(args[2]) : 9600;
                Parity parity = args.Length > 3 ? (Parity)Enum.Parse(typeof(Parity), args[3], true) : Parity.None;
                int dataBits = args.Length > 4 ? int.Parse(args[4]) : 8;
                StopBits stopBits = args.Length > 5 ? (StopBits)Enum.Parse(typeof(StopBits), args[5], true) : StopBits.One;

                //попытка инициализации и открытия порта с заданными параметрами
                var result = await _serialPortService.InitializeAndOpenAsync(portName, baudRate, parity, dataBits, stopBits);
                await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
                if (result.Success)
                {
                    await _permissionsDatabase.SaveSerialPortConfigAsync(portName, baudRate, parity, dataBits, stopBits);
                }
            }
            else
            {
                try
                {
                    var config = await _permissionsDatabase.LoadSerialPortConfigAsync();
                    //попытка инициализации и открытия порта с сохранёнными параметрами
                    var result = await _serialPortService.InitializeAndOpenAsync(config.PortName, config.BaudRate, config.Parity, config.DataBits, config.StopBits);
                    await botClient.SendTextMessageAsync(message.Chat.Id, result.Message);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Не найдена сохраненная конфигурация. Укажите параметры для открытия порта.");
                }
            }

            if (_serialPortService.CurrentSerialPort().IsOpen)
            {
                // Активация приёма данных
                _serialPortService.ActivateDataReceiving(async (data, chatId) =>
                {
                    await _dataHandler.HandleReceivedDataAsync(data, chatId);
                });
            }
        }
    }
}
