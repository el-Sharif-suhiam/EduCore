using Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Dtos
{
    public class DtoAudit
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public enAuditActionType ActionType { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }

        public string Description { get; set; } = string.Empty;
        public DateTime DoneAt { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
