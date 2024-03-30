using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ArduinoTelegramBot.Models.Authorization;

[Table("access_keys")]
public class AccessKey
{
    [Key]
    public string Key { get; set; }

    [Column("available_commands")]
    public string AvailableCommandsJson { get; set; }

    [NotMapped]
    public List<string> AvailableCommands
    {
        get => JsonConvert.DeserializeObject<List<string>>(AvailableCommandsJson) ?? new List<string>();
        set => AvailableCommandsJson = JsonConvert.SerializeObject(value);
    }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_master_key")]
    public bool IsMasterKey { get; set; }
}
