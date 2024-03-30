using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArduinoTelegramBot.Models.Authorization;

[Table("user_keys")]
public class UserKey
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("key")]
    public string Key { get; set; }
}
