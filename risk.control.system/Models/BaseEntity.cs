﻿namespace risk.control.system.Models
{
    public class BaseEntity
    {
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
        public string? UpdatedBy { get; set; } = default!;
    }
}