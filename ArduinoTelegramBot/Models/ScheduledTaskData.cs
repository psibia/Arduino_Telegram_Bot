﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models
{
    public class ScheduledTaskData
    {
        public string TaskId { get; set; } = Guid.NewGuid().ToString();
        public string CommandName { get; set; }
        public string ChatId { get; set; }
        public TimeSpan? DailyTime { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime? NextRunTime { get; set; }
    }
}
