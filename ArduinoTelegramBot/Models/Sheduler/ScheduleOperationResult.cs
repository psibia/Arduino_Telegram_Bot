using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models.Sheduler
{
    public class ScheduleOperationResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }

        private ScheduleOperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static ScheduleOperationResult Ok(string message)
        {
            return new ScheduleOperationResult(true, message);
        }

        public static ScheduleOperationResult Error(string message)
        {
            return new ScheduleOperationResult(false, message);
        }
    }
}
