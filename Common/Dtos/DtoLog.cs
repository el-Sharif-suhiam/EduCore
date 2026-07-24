using Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoLog
    {
        public int Id { get; set; }
        public enLogType LogType { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestPath { get; set; }
    }
}
