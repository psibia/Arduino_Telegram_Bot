using ArduinoTelegramBot.Models.Authorization;
using ArduinoTelegramBot.Repositories.Authorization.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ArduinoTelegramBot.Repositories.Authorization
{
    public class MySQLAccessControlService : IAccessControlService
    {
        private readonly AccessControlDbContext _context;

        public MySQLAccessControlService(AccessControlDbContext context)
        {
            _context = context;
        }

        public async Task<AccessKey> GetPermissionsAsync(string key)
        {
            var accessKey = await _context.AccessKeys
                .Where(ak => ak.Key == key && ak.IsActive)
                .FirstOrDefaultAsync();

            if (accessKey == null)
                throw new KeyNotFoundException($"Ключ '{key}' не найден или был заблокирован.");

            return accessKey;
        }

        public async Task<Dictionary<long, string>> LoadUserKeysAsync()
        {
            return await _context.UserKeys
                .ToDictionaryAsync(uk => uk.UserId, uk => uk.Key);
        }

        public async Task SaveUserKeysAsync(Dictionary<long, string> userKeys)
        {
            foreach (var userKey in userKeys)
            {
                var existingUserKey = await _context.UserKeys.FindAsync(userKey.Key);
                if (existingUserKey != null)
                {
                    existingUserKey.Key = userKey.Value;
                }
                else
                {
                    await _context.UserKeys.AddAsync(new UserKey { UserId = userKey.Key, Key = userKey.Value });
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task CreateAccessKeyAsync(AccessKey accessKey)
        {
            await _context.AccessKeys.AddAsync(accessKey);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAccessKeyAsync(string key)
        {
            var accessKey = await _context.AccessKeys.FindAsync(key);
            if (accessKey != null)
            {
                // Удаление всех пользователей, связанных с ключом
                var usersWithKey = _context.UserKeys.Where(uk => uk.Key == key);
                _context.UserKeys.RemoveRange(usersWithKey);

                // Удаление самого ключа доступа
                _context.AccessKeys.Remove(accessKey);

                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException($"Ключ '{key}' не найден.");
            }
        }


        public async Task BlockAccessKeyAsync(string key)
        {
            var accessKey = await _context.AccessKeys.FindAsync(key);
            if (accessKey != null)
            {
                accessKey.IsActive = false;
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException($"Ключ '{key}' не найден.");
            }
        }

        public async Task UnblockAccessKeyAsync(string key)
        {
            var accessKey = await _context.AccessKeys.FindAsync(key);
            if (accessKey != null)
            {
                accessKey.IsActive = true;
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException($"Ключ '{key}' не найден.");
            }
        }
    }
}
