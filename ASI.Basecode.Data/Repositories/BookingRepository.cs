using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASI.Basecode.Data.Models;
using System.IO;
using ASI.Basecode.Data.Interfaces;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Data.Repositories
{
    public class BookingRepository : BaseRepository, IBookingRepository
    {
        private readonly AsiBasecodeDBContext _dbContext;

        public BookingRepository(AsiBasecodeDBContext dbContext, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<Booking> ViewBookings()
        {
            return _dbContext.Bookings
             .Include(b => b.Room)
             .Include(b => b.User) 
             .ToList();
        }
        public void AddBooking(Booking booking)
        {
            try
            {
                var newBooking = new Booking();
                newBooking.ID = booking.ID;
                newBooking.bookingRefId = booking.bookingRefId;
                newBooking.roomId = booking.roomId;
                newBooking.date = booking.date;
                newBooking.time = booking.time;
                newBooking.duration = booking.duration;
                newBooking.isRecurring = booking.isRecurring;
                newBooking.recurrenceFrequency = booking.recurrenceFrequency;
                newBooking.recurrenceEndDate = booking.recurrenceEndDate;
                newBooking.recurringBookingId = booking.recurringBookingId;
                _dbContext.Bookings.Add(newBooking);
                _dbContext.SaveChanges();
            }
            catch (Exception)
            {
                throw new InvalidDataException("Error adding booking");
            }
        }

        public (bool, IEnumerable<Booking>) GetBookings()
        {
            try
            {
                var bookings = _dbContext.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.User)  // Include User navigation property
                    .ToList();
                return (true, bookings);
            }
            catch
            {
                return (false, null);
            }
        }

        public IEnumerable<Booking> GetBookingsByUserId(int id)
        {
            return _dbContext.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)  
            .Where(b => b.ID == id)
            .OrderByDescending(b => b.date)
            .ToList();
        }

        public IEnumerable<Room> GetRooms()
        {
            return _dbContext.Rooms.ToList();
        }

        public IEnumerable<Booking> GetBookingsWithinNextHour(int userId)
        {
            var now = DateTime.Now;
            var oneHourFromNow = now.AddHours(1);

            return _dbContext.Bookings
            .Where(b => b.ID == userId &&
                        b.date.Date == now.Date &&
                        b.time.TimeOfDay >= now.TimeOfDay &&
                        b.time.TimeOfDay <= oneHourFromNow.TimeOfDay)
            .ToList();

        }

        public Booking GetBookingById(int id)
        {
            var booking = _dbContext.Bookings.FirstOrDefault(x => x.bookingId == id);

            if (booking == null)
            {
                throw new Exception("Book not found!");
            }

            return booking;
        }

        public void UpdateBooking(Booking booking)
        {
            var existingBooking = GetBookingById(booking.bookingId);

            if (existingBooking != null)
            {
                existingBooking.roomId = booking.roomId;
                existingBooking.date = booking.date;
                existingBooking.time = booking.time;
                existingBooking.duration = booking.duration;

                _dbContext.Bookings.Update(existingBooking);
                _dbContext.SaveChanges();

            }
        }

        public void DeleteBooking(Booking booking)
        {
            var existingBooking = GetBookingById(booking.bookingId);
            _dbContext.Bookings.Remove(existingBooking);
            _dbContext.SaveChanges();
        }

        public int GetUserID(string userId)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.userID == userId);
            if (user == null)
            {
                throw new Exception("User not found!");
            }
            return user.ID;
        }
        public int GetRecurringIdTracker()
        {
            _dbContext.RecurringIdTrackers.Add(new RecurringIdTracker());
            _dbContext.SaveChanges();
            var tracker = _dbContext.RecurringIdTrackers.OrderByDescending(t => t.id).FirstOrDefault();
            return tracker.id;
        }

        public IEnumerable<Booking> GetRecurringBookings(int? recurringBookingId)
        {
            return _dbContext.Bookings
                .Where(b => b.recurringBookingId == recurringBookingId)
                .ToList();
        }
        public IEnumerable<Booking> GetBookingsByRoomAndDate(int roomId, DateTime date)
        {
            return _dbContext.Bookings
                .Where(b => b.roomId == roomId && b.date.Date == date.Date)
                .ToList();
        }
    }
}
