using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IBookingService
    {
        (bool result, IEnumerable<Booking> bookings) GetAllBookings();

        (bool result, IEnumerable<Booking> bookings) GetBookingsByuserName(int id);

        void AddBooking(Booking booking);

        void UpdateBooking(Booking booking);

        void DeleteBooking(Booking booking);

        IEnumerable<Room> GetRooms();

        Booking GetBookingById(int id);

        int GetuserName(string userName);

        int GetRecurringIdTracker();

        IEnumerable<Booking> GetBookingsWithinNextHour(int userName);

        IEnumerable<Booking> GetRecurringBookings(int? recurringBookingId);
        IEnumerable<Booking> GenerateRoomUsageReport(string room, DateTime startDate, DateTime endDate);

        IEnumerable<Booking> GetBookingsByRoomAndDate(int roomId, DateTime date);
    }
}
