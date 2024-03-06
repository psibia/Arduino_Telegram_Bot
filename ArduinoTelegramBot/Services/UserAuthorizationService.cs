using Serilog;
using ArduinoTelegramBot.Services.Interfaces;
using ArduinoTelegramBot.Models;
using System.Collections.Concurrent;

namespace ArduinoTelegramBot.Services;

public class UserAuthorizationService : IUserAuthorizationService
{
    private ConcurrentDictionary<long, string> _userKeys = new ConcurrentDictionary<long, string>();
    private ConcurrentDictionary<long, DateTime> _lastChecked = new ConcurrentDictionary<long, DateTime>();
    private static readonly ConcurrentDictionary<string, AccessKey> _permissions = new ConcurrentDictionary<string, AccessKey>();
    private const int CacheDurationInMinutes = 1;
    private readonly IPermissionsDatabaseService _permissionsDatabase;

    public UserAuthorizationService(IPermissionsDatabaseService permissionsDatabase)
    {
        _permissionsDatabase = permissionsDatabase;
        InitializeAsync().ConfigureAwait(false);
    }

    public async Task<bool> CheckUserAuthorization(long userId, string commandName)
    {
        if (_userKeys.TryGetValue(userId, out var key) && NeedToRefreshPermissions(userId, key))
        {
            return await FetchAndCheckPermissionAsync(userId, key, commandName);
        }
        else if (_permissions.TryGetValue(key, out var cachedAccessKey))
        {
            return HasAccessToCommand(cachedAccessKey, commandName);
        }
        return false;
    }

    public async Task<AuthorizationResult> AttemptAuthorizationWithKey(string key, long userId)
    {
        try
        {
            await AuthorizeUserAsync(userId, key);
            var message = "Вы успешно авторизованы.";
            Log.Information($"Сервис авторизации: Пользователь {userId} успешно авторизован с ключом {key}");
            return new AuthorizationResult { Success = true, Message = message };
        }
        catch (KeyNotFoundException)
        {
            var message = "Данный ключ не найден или неверен.";
            Log.Warning($"Сервис авторизации: Пользователь {userId} пытался авторизоваться с неверным ключом {key}.");
            return new AuthorizationResult { Success = false, Message = message };
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            _userKeys = await _permissionsDatabase.LoadUserKeysAsync();
            Log.Information($"Сервис авторизации: Данные авторизации пользователей успешно загружены. Загружено {_userKeys.Count} записей");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Сервис авторизации: Произошла ошибка при загрузке данных авторизации пользователей");
        }
    }

    private bool NeedToRefreshPermissions(long userId, string key) => !_lastChecked.ContainsKey(userId) || DateTime.UtcNow.Subtract(_lastChecked[userId]).TotalMinutes > CacheDurationInMinutes || !_permissions.ContainsKey(key);

    private async Task<bool> FetchAndCheckPermissionAsync(long userId, string key, string commandName)
    {
        var accessKey = await _permissionsDatabase.GetPermissionsAsync(key);
        _lastChecked[userId] = DateTime.UtcNow;
        _permissions[key] = accessKey;
        return HasAccessToCommand(accessKey, commandName);
    }

    private bool HasAccessToCommand(AccessKey accessKey, string commandName)
    {
        return accessKey.IsMasterKey || (accessKey.AvailableCommands?.Contains(commandName, StringComparer.InvariantCultureIgnoreCase) ?? false);
    }

    private async Task AuthorizeUserAsync(long userId, string key)
    {
        var accessKey = await _permissionsDatabase.GetPermissionsAsync(key);
        if (accessKey != null)
        {
            _userKeys[userId] = key;
            _lastChecked[userId] = DateTime.UtcNow;
            _permissions[key] = accessKey;
            await _permissionsDatabase.SaveUserKeysAsync(_userKeys);
        }
    }
}