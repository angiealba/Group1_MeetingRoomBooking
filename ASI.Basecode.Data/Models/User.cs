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
        public string userID { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        [MaxLength(255)]
        public string password { get; set; }

        public string role { get; set; }
        public string createdBy { get; set; }
        public DateTime createdTime { get; set; }
        public string updatedBy { get; set; }
        public DateTime updatedTime { get; set; }
    }
}
