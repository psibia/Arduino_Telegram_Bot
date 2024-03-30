using Serilog;
using ArduinoTelegramBot.Services.Interfaces;
using System.Collections.Concurrent;
using ArduinoTelegramBot.Repositories.Authorization.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using ArduinoTelegramBot.Models.Authorization;

namespace ArduinoTelegramBot.Services;

public class UserAuthorizationService : IUserAuthorizationService
{
    private readonly IAccessControlService _authorizationDataService;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration; // Длительность хранения кэша

    public UserAuthorizationService(IAccessControlService authorizationDataService, IMemoryCache cache)
    {
        _authorizationDataService = authorizationDataService;
        _cache = cache;
        _cacheDuration = TimeSpan.FromSeconds(60); //установка времени жизни кэша в секундах
    }

    public async Task<bool> CheckUserAuthorization(long userId, string commandName)
    {
        // Ключ для кэша
        var cacheKey = $"Auth_{userId}";

        if (!_cache.TryGetValue(cacheKey, out AccessKey accessKey))
        {
            // Попытка получить ключ доступа пользователя и его права доступа из базы данных
            try
            {
                var userKeys = await _authorizationDataService.LoadUserKeysAsync();
                if (userKeys.TryGetValue(userId, out var key))
                {
                    accessKey = await _authorizationDataService.GetPermissionsAsync(key);
                    // Сохраняем в кэше с заданной длительностью
                    _cache.Set(cacheKey, accessKey, _cacheDuration);
                }
                else
                {
                    Log.Information("Сервис авторизации: Не найден ключ авторизации для пользователя {UserId}", userId);
                    return false;
                }
            }
            catch (KeyNotFoundException)
            {
                Log.Information("Сервис авторизации: Отсутствуют разрешения для пользователя {UserId}", userId);
                return false;
            }
        }

        // Проверяем, имеет ли пользователь доступ к команде
        var hasAccess = accessKey.IsMasterKey || (accessKey.AvailableCommands?.Contains(commandName, StringComparer.InvariantCultureIgnoreCase) ?? false);
        Log.Information("Сервис авторизации: Пользователь {UserId} {Access} доступ к команде {CommandName}", userId, hasAccess ? "имеет" : "не имеет", commandName);
        return hasAccess;
    }

    public async Task<AuthorizationResult> AttemptAuthorizationWithKey(string key, long userId)
    {
        try
        {
            var accessKey = await _authorizationDataService.GetPermissionsAsync(key);
            // Дополнительная логика проверки валидности ключа может быть реализована тута
            await _authorizationDataService.SaveUserKeysAsync(new System.Collections.Generic.Dictionary<long, string> { { userId, key } });

            var message = "Вы успешно авторизованы.";
            Log.Information("Сервис авторизации: Пользователь {UserId} успешно авторизован с ключом {Key}", userId, key);
            return new AuthorizationResult { Success = true, Message = message };
        }
        catch (KeyNotFoundException)
        {
            var message = "Данный ключ не найден или неверен.";
            Log.Warning("Сервис авторизации: Пользователь {UserId} пытался авторизоваться с неверным ключом {Key}.", userId, key);
            return new AuthorizationResult { Success = false, Message = message };
        }
    }
}