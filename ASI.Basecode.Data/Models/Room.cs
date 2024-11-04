using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Models
{
    public class Room
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [Key]
        public int roomId { get; set; }
        [Required]
        public string roomName { get; set; }
        [Required]
        public string roomLocation { get; set; }
        [Required]
        public int roomCapacity { get; set; }
        [Required]
        public string availableFacilities { get; set; }
        [Required]
        public DateTime DateTimeCreated = DateTime.Now;
        public string createdBy { get; set; }
    }
}
