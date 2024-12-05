using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASI.Basecode.Data.Models
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int ID { get; set; }
        public string userName { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        [MaxLength(255)]
        public string password { get; set; }
        public string role { get; set; }
        public string createdBy { get; set; }
        public DateTime createdTime { get; set; }
        public string updatedBy { get; set; }
        public DateTime updatedTime { get; set; }
        public bool enableNotifications { get; set; } = true; // Default to true
        public int defaultBookingDuration { get; set; } = 1; // Default to 1 hour
    }
}
