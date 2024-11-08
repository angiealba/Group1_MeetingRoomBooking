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
        (bool result, IEnumerable<Booking> bookings) GetBookings();

        (bool result, IEnumerable<Booking> bookings) GetBookingsByUserId(int id);

        void AddBooking(Booking booking);

        void UpdateBooking(Booking booking);

        void DeleteBooking(Booking booking);

        IEnumerable<Room> GetRooms();

        Booking GetBookingById(int id);

        int GetUserID(string userId);

        int GetRecurringIdTracker();

        IEnumerable<Booking> GetRecurringBookings(int? recurringBookingId);
    }
}
