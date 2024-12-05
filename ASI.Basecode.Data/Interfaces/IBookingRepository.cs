using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IBookingRepository
    {
        IEnumerable<Booking> ViewBookings();
        void AddBooking(Booking booking);
        (bool, IEnumerable<Booking>) GetBookings();
        IEnumerable<Booking> GetBookingsByuserName(int id);
        IEnumerable<Room> GetRooms();

        public IEnumerable<Booking> GetBookingsWithinNextHour(int userName);
        Booking GetBookingById(int id);
        void UpdateBooking(Booking booking);
        void DeleteBooking(Booking booking);
        int GetuserName(string userName);
        int GetRecurringIdTracker();
        IEnumerable<Booking> GetRecurringBookings(int? recurringBookingId);
        IEnumerable<Booking> GetBookingsByRoomAndDate(int roomId, DateTime date);
    }
}
