using ArduinoTelegramBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models
{
    public class TimerInfo
    {
        public Timer Timer { get; set; }
        public string ChatId { get; set; }
        public IAuthorizedCommand Command { get; set; }
        public TimeSpan? DailyTime { get; set; }
    }
}
