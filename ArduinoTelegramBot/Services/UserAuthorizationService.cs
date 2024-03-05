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
        Initialize();
    }

    private async void Initialize()
    {
        try
        {
            Log.Information("Сервис авторизации: Начинается загрузка данных авторизации пользователей");
            _userKeys = await _permissionsDatabase.LoadUserKeysAsync();
            Log.Information("Сервис авторизации: Данные авторизации пользователей успешно загружены. Загружено {Count} записей", _userKeys.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Сервис авторизации: Произошла ошибка при загрузке данных авторизации пользователей");
        }
    }

    public async Task<bool> IsAuthorized(long userId, string commandName)
    {
        if (_userKeys.TryGetValue(userId, out var key) &&
            (!_lastChecked.ContainsKey(userId) || DateTime.UtcNow.Subtract(_lastChecked[userId]).TotalMinutes > CacheDurationInMinutes || !_permissions.ContainsKey(key)))
        {
            try
            {
                var accessKey = await _permissionsDatabase.GetPermissionsAsync(key);
                _lastChecked[userId] = DateTime.UtcNow;
                _permissions[key] = accessKey; //обновляем кэшированный объект AccessKey для ключа

                return accessKey.IsMasterKey || (accessKey.AvailableCommands?.Contains(commandName, StringComparer.InvariantCultureIgnoreCase) ?? false);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }
        else
        {
            //если ключ находится в кэше и не истекло время кэширования, проверяем доступность команды
            var cachedAccessKey = _permissions[key];
            return cachedAccessKey.IsMasterKey || (cachedAccessKey.AvailableCommands?.Contains(commandName, StringComparer.InvariantCultureIgnoreCase) ?? false);
        }
    }



    public async Task AuthorizeUser(long userId, string key)
    {
        try
        {
            var accessKey = await _permissionsDatabase.GetPermissionsAsync(key);
            if (accessKey != null)
            {
                _userKeys[userId] = key;
                _lastChecked[userId] = DateTime.UtcNow;
                _permissions[key] = accessKey; //Обновляем кэшированный объект AccessKey
                await _permissionsDatabase.SaveUserKeysAsync(_userKeys); //сохраняем обновленные данные авторизации.
            }
        }
        catch (KeyNotFoundException ex)
        {
            //Log.Warning(ex.Message);
            throw; //logiruen na lvl vishe
        }
    }

    public bool IsUserAuthorized(long userId)
    {
        return _userKeys.ContainsKey(userId) && _lastChecked.ContainsKey(userId) &&
               DateTime.UtcNow.Subtract(_lastChecked[userId]).TotalMinutes <= CacheDurationInMinutes;
    }

    public async Task<AuthorizationResult> ProcessAuthorizationAttempt(string key, long userId)
    {
        try
        {
            await AuthorizeUser(userId, key);
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