using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Models
{
    public class Booking
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int bookingId { get; set; }

        public int ID { get; set; }


        [ForeignKey("ID")]
        public virtual User User { get; set; }

        [Required(ErrorMessage = "Please select a room")]
        public int roomId { get; set; }

        [ForeignKey("roomId")]
        public virtual Room Room { get; set; }

        [Required(ErrorMessage = "Please enter a date")]
        public DateTime date { get; set; }

        [Required(ErrorMessage = "Please enter a time")]
        public DateTime time { get; set; }

        [Required(ErrorMessage = "Please enter a duration")]
        public int duration { get; set; }

        public bool isRecurring { get; set; } 

        public string recurrenceFrequency { get; set; } 

        public DateTime? recurrenceEndDate { get; set; } 

        public int? recurringBookingId { get; set; }
    }
}
