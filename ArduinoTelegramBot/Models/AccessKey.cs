namespace ArduinoTelegramBot.Models
{
    public class AccessKey
    {
        public string Key { get; set; }
        public List<string> AvailableCommands { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public bool IsMasterKey { get; set; }

        public AccessKey(string key, List<string> availableCommands, bool isActive, bool isMasterKey)
        {
            Key = key;
            AvailableCommands = availableCommands ?? new List<string>();
            IsActive = isActive;
            IsMasterKey = isMasterKey;
        }
    }
}
