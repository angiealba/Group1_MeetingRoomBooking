using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Data.Repositories;
using ASI.Basecode.Services.Interfaces;
using RTools_NTS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;

        public BookingService(IBookingRepository bookingRepository)
        {
            this._bookingRepository = bookingRepository;
        }

        public (bool, IEnumerable<Booking>) GetAllBookings()
        {
            var bookings = _bookingRepository.ViewBookings();
            if (bookings != null)
            {
                return (true, bookings);
            }
            return (false, null);
        }



        public (bool, IEnumerable<Booking>) GetBookingsByuserName(int id)
        {
            var bookings = _bookingRepository.GetBookingsByuserName(id);
            if (bookings != null)
            {
                return (true, bookings);
            }
            return (false, null);
        }

        public void AddBooking(Booking booking)
        {
            if (booking == null)
            {
                throw new ArgumentException("Booking cannot be null.");
            }

            var newBooking = new Booking();

            newBooking.ID = booking.ID;
            newBooking.bookingRefId = Math.Abs(System.Guid.NewGuid().GetHashCode()).ToString();
            newBooking.roomId = booking.roomId;
            newBooking.date = booking.date;
            newBooking.time = booking.time;
            newBooking.duration = booking.duration;
            newBooking.isRecurring = booking.isRecurring;
            newBooking.recurrenceFrequency = booking.recurrenceFrequency;
            newBooking.recurrenceEndDate = booking.recurrenceEndDate;
            newBooking.recurringBookingId = booking.recurringBookingId;

            _bookingRepository.AddBooking(newBooking);
        }

        public IEnumerable<Room> GetRooms()
        {
            return _bookingRepository.GetRooms();
        }

        public Booking GetBookingById(int id)
        {
            return _bookingRepository.GetBookingById(id);
        }

        public void UpdateBooking(Booking booking)
        {
            if (booking == null)
            {
                throw new ArgumentException("Booking cannot be null.");
            }

            _bookingRepository.UpdateBooking(booking);
        }

        public IEnumerable<Booking> GetBookingsWithinNextHour(int userName)
        {
            return _bookingRepository.GetBookingsWithinNextHour(userName);

        }

        public void DeleteBooking(Booking booking)
        {
            if (booking == null)
            {
                throw new ArgumentException("Booking cannot be null.");
            }

            _bookingRepository.DeleteBooking(booking);
        }

        public int GetuserName(string userName)
        {
            return _bookingRepository.GetuserName(userName);
        }

        public int GetRecurringIdTracker()
        {
            return _bookingRepository.GetRecurringIdTracker();
        }

        public IEnumerable<Booking> GetRecurringBookings(int? recurringBookingId)
        {
            return _bookingRepository.GetRecurringBookings(recurringBookingId);
        }
        public IEnumerable<Booking> GenerateRoomUsageReport(string room,DateTime startDate, DateTime endDate)
        {
            // Fetch all bookings within the specified date range without aggregation or grouping
            var bookings = _bookingRepository.ViewBookings()
                            .Where(b => b.date >= startDate && b.date <= endDate);

            return bookings;
        }
        public IEnumerable<Booking> GetBookingsByRoomAndDate(int roomId, DateTime date)
        {
            return _bookingRepository.GetBookingsByRoomAndDate(roomId, date);
        }
    }
}
