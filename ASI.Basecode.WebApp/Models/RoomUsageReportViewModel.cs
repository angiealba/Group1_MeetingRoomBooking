using System.Collections.Generic;
using System;

public class RoomUsageReportViewModel
{
    public List<RoomUsageSummary> BookingSummaries { get; set; }
}

public class RoomUsageSummary
{
    public DateTime Date { get; set; }
    public int TotalBookings { get; set; }
    public string PeakUsageTime { get; set; }
    public string UserActivity { get; set; }
}
