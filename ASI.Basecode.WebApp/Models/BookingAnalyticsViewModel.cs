using System;

namespace ASI.Basecode.WebApp.Models
{
    public class BookingAnalyticsViewModel
    {
        public DateTime Date { get; set; }
        public int TotalBookings { get; set; }
        public string PeakUsageTime { get; set; }
        public string ActivityLevel { get; set; }
    }
}
