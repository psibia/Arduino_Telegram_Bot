using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Models
{
    public class OperationResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }

        private OperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static OperationResult Ok(string message)
        {
            return new OperationResult(true, message);
        }

        public static OperationResult Fail(string message)
        {
            return new OperationResult(false, message);
        }
    }
}
