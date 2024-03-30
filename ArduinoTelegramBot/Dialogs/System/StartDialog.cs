using ArduinoTelegramBot.Dialogs.Interfaces;
using ArduinoTelegramBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using ArduinoTelegramBot.Dialogs.Test;

namespace ArduinoTelegramBot.Dialogs.System;

public class StartDialog : IDialog
{
    public string Name { get; set; }
    public bool IsComplete { get; private set; } = false;
    private IUserAuthorizationService _authorizationService;
    private ITelegramBotClient _botClient;
    private long _chatId;
    private bool _awaitingKey = false;
    private bool _awaitingHelpDecision = false;

    public StartDialog(ITelegramBotClient botClient, IUserAuthorizationService authorizationService, string name)
    {
        _botClient = botClient;
        _authorizationService = authorizationService;
        Name = name;
    }

    public static StartDialog Create(IServiceProvider serviceProvider, string name)
    {
        return new StartDialog(
            serviceProvider.GetRequiredService<ITelegramBotClient>(),
            serviceProvider.GetRequiredService<IUserAuthorizationService>(),
            name);
    }

    public async Task StartAsync(ITelegramBotClient botClient, Message message)
    {
        _chatId = message.Chat.Id;
        var welcomeMessage = "Добро пожаловать! У вас есть авторизационный ключ?";
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Да", "yes"),
            InlineKeyboardButton.WithCallbackData("Нет", "no")
        });
        await botClient.SendTextMessageAsync(message.Chat.Id, welcomeMessage, replyMarkup: keyboard);
    }

    public async Task<bool> NextAsync(ITelegramBotClient botClient, Message message)
    {
        if (IsComplete) return false;

        if (_awaitingKey)
        {
            return await ProcessAuthorizationKey(message.Text);
        }

        if (_awaitingHelpDecision)
        {
            ProcessHelpDecision(message.Text);
            return false;
        }

        return true;
    }

    private async Task<bool> ProcessAuthorizationKey(string key)
    {
        var result = await _authorizationService.AttemptAuthorizationWithKey(key, _chatId);
        await _botClient.SendTextMessageAsync(_chatId, result.Message);

        if (result.Success)
        {
            _awaitingHelpDecision = true;
            var helpKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Да", "help_yes"),
                InlineKeyboardButton.WithCallbackData("Нет", "help_no")
            });
            await _botClient.SendTextMessageAsync(_chatId, "Хотите получить подробную справку по работе с приложением?", replyMarkup: helpKeyboard);
        }
        else
        {
            var retryKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Да", "retry"),
                InlineKeyboardButton.WithCallbackData("Нет", "no_retry")
            });
            await _botClient.SendTextMessageAsync(_chatId, "Хотите попробовать ввести ключ еще раз?", replyMarkup: retryKeyboard);
        }

        _awaitingKey = false;
        return true;
    }

    private void ProcessHelpDecision(string decision)
    {
        _awaitingHelpDecision = false;
        IsComplete = true;

        if (decision == "help_yes")
        {
            // Отправляем подробную справку
            _botClient.SendTextMessageAsync(_chatId, "Чтобы отобразить список всех доступных команд, нажмите на эту команду: /help").Wait();
        }
        else if (decision == "help_no")
        {
            // Пожелания приятной работы и завершение диалога
            _botClient.SendTextMessageAsync(_chatId, "Приятной работы!").Wait();
        }
    }

    public async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        _chatId = callbackQuery.Message.Chat.Id;

        if (_awaitingHelpDecision)
        {
            ProcessHelpDecision(callbackQuery.Data);
        }
        else
        {
            switch (callbackQuery.Data)
            {
                case "yes":
                    _awaitingKey = true;
                    await _botClient.SendTextMessageAsync(_chatId, "Пожалуйста, введите ваш ключ.");
                    break;
                case "no":
                case "no_retry":
                    await _botClient.SendTextMessageAsync(_chatId, "Пожалуйста, обратитесь к системному администратору за ключом.");
                    IsComplete = true;
                    break;
                case "retry":
                    _awaitingKey = true;
                    await _botClient.SendTextMessageAsync(_chatId, "Введите ключ авторизации.");
                    break;
            }
        }

        // Удаляем клавиатуру после ответа
        await _botClient.EditMessageReplyMarkupAsync(_chatId, callbackQuery.Message.MessageId, null);
    }
}