﻿using risk.control.system.AppConstant;
using System.ComponentModel.DataAnnotations;

namespace risk.control.system.Models
{
    public class VendorApplicationUser : ApplicationUser
    {
        [Display(Name = "Vendor code")]
        public long? VendorId { get; set; } = default!;
        public AgencyRole? UserRole { get; set; }
        public Vendor? Vendor { get; set; } = default!;
        public string? Comments { get; set; } = default!;
    }
}