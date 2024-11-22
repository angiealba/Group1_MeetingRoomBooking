using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Models
{
	public class Notification
	{
		public int Id { get; set; }
		public int userId { get; set; }
		public string Type { get; set; }
		public string Message { get; set; }
		public DateTime Date { get; set; }
		public bool IsRead { get; set; }

		// New property for booking date
		public DateTime? BookingDate { get; set; }
	}

}
