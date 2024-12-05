using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NUlid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace ASI.Basecode.WebApp.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        public BookingController(IBookingService bookingService, INotificationService notificationService, IUserService userService)
        {
            _bookingService = bookingService;
            _notificationService = notificationService;
            _userService = userService;
        }

        
        public ActionResult Index(string search, int page = 1, int pageSize = 6)
        {
            int id = 0;
            var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            User currentUser = null;

            if (userName != null)
            {
                id = _bookingService.GetuserName(userName);
                currentUser = _userService.GetUserByuserName(id);
            }


            ViewBag.defaultBookingDuration = currentUser?.defaultBookingDuration ?? 1;

            if (userName != null)
            {
                id = _bookingService.GetuserName(userName); 
            }

            // get user bookings
            (bool result, IEnumerable<Booking> bookings) = _bookingService.GetBookingsByuserName(id);

            // get rooms
            var rooms = _bookingService.GetRooms();
            ViewBag.Rooms = new SelectList(rooms, "roomId", "roomName");

            if (!result)
            {
                return View(null);
            }

            // if search is not null or empty
            if (!string.IsNullOrEmpty(search))
            {
                bookings = bookings.OrderByDescending(b=>bookings).Where(b => b.Room.roomName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                            || b.date.ToString("yyyy-MM-dd").Contains(search)
                                            || b.time.ToString("hh:mm tt").Contains(search)
                                            || b.bookingRefId.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // paginate
            var totalBookings = bookings.Count();
            var totalPages = (int)Math.Ceiling((double)totalBookings / pageSize);
            var paginatedBookings = bookings.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchQuery = search;

            return View(paginatedBookings);
        }

        [HttpPost]
        public IActionResult CreateBooking(Booking booking)
        {
            try
            {
                // check model state
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToAction("Index");
                }

                // check date
                if (booking.date.Date < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "You cannot book a room for a past date.";
                    return RedirectToAction("Index");
                }

                // check if date is more than one year in advance
                if (booking.date.Date > DateTime.Today.AddYears(1))
                {
					TempData["ErrorMessage"] = "You cannot book a room for more than one year in advance.";
					return RedirectToAction("Index");
				}

                // get roomname
                var rooms = _bookingService.GetRooms();
                var room = rooms.FirstOrDefault(r => r.roomId == booking.roomId);
                string roomName = room != null ? room.roomName : "Unknown Room";

                // get user ID
                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userName != null)
                {
                    int id = _bookingService.GetuserName(userName);
                    booking.ID = id;
                }

                if (booking.isRecurring)
                {
					// recurrenceFrequency & recurrenceEndDate validation
					if (string.IsNullOrEmpty(booking.recurrenceFrequency) || booking.recurrenceEndDate == null)
                    {
                        TempData["ErrorMessage"] = "Please specify a recurrence frequency and end date.";
                        return RedirectToAction("Index");
                    }

					// recurrenceEndDate validation
					if (booking.recurrenceEndDate <= booking.date)
                    {
                        TempData["ErrorMessage"] = "'Until' date must be after the initial booking date.";
                        return RedirectToAction("Index");
                    }

                    // Calculate the end time of the new booking
                    var bookingEndTime = booking.time.AddHours(booking.duration);

                    // Check for time conflicts for all recurring bookings
                    DateTime currentDate = booking.date;
                    while (currentDate < booking.recurrenceEndDate)
                    {
                        // Get existing bookings for the current date
                        var existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, currentDate);

                        // Check for time conflicts for the current date
                        bool isTimeBookedForDate = existingBookings.Any(b =>
                            (b.date == currentDate) && // Same date
                            (b.time < bookingEndTime) && // Existing booking starts before new booking ends
                            (booking.time < b.time.AddHours(b.duration)) // New booking starts before existing booking ends
                        );

                        // check if there is a conflict
                        if (isTimeBookedForDate)
                        {
                            TempData["ErrorMessage"] = $"The room '{roomName}' is already booked at {currentDate.ToString("MM-dd-yyyy")} {booking.time.ToString("hh:mm tt")}.";
                            return RedirectToAction("Index");
                        }

                        // Move to the next date based on recurrence frequency
                        switch (booking.recurrenceFrequency)
                        {
                            case "daily":
                                currentDate = currentDate.AddDays(1);
                                break;
                            case "weekly":
                                currentDate = currentDate.AddDays(7);
                                break;
                            case "monthly":
                                currentDate = currentDate.AddMonths(1);
                                break;
                        }
                    }

                    // if no conflict, add the recurring bookings
                    int recurringSeriesId = _bookingService.GetRecurringIdTracker(); // for unique recurring id
                    booking.recurringBookingId = recurringSeriesId;
                    _bookingService.AddBooking(booking);
                    currentDate = booking.date;
                    while (currentDate < booking.recurrenceEndDate)
                    {
                        switch (booking.recurrenceFrequency)
                        {
                            case "daily":
                                currentDate = currentDate.AddDays(1);
                                break;
                            case "weekly":
                                currentDate = currentDate.AddDays(7);
                                break;
                            case "monthly":
                                currentDate = currentDate.AddMonths(1);
                                break;
                        }

                        var newBooking = new Booking
                        {
                            ID = booking.ID,
                            roomId = booking.roomId,
                            date = currentDate,
                            time = booking.time,
                            duration = booking.duration,
                            isRecurring = true,
                            recurrenceFrequency = booking.recurrenceFrequency,
                            recurrenceEndDate = booking.recurrenceEndDate,
                            recurringBookingId = booking.recurringBookingId
                        };

                        _bookingService.AddBooking(newBooking);
                    }
                }
                else
                {
                    // Check for existing bookings
                    var existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, booking.date);

                    // Calculate the end time of the new booking
                    var bookingEndTime = booking.time.AddHours(booking.duration);

                    // Check for time conflicts
                    bool isTimeBooked = existingBookings.Any(b =>
                        (b.date == booking.date) && // Same date
                        (b.time < bookingEndTime) && // Existing booking starts before new booking ends
                        (booking.time < b.time.AddHours(b.duration)) // New booking starts before existing booking ends
                    );

                    if (isTimeBooked)
                    {
                        TempData["ErrorMessage"] = $"The room '{roomName}' is already booked at this time.";
                        return RedirectToAction("Index");
                    }

                    booking.recurrenceFrequency = null;
                    booking.recurrenceEndDate = null;
                    booking.recurringBookingId = null;

                    _bookingService.AddBooking(booking);
                }
                
                _notificationService.AddNotification(
                    booking.ID,
                    "Booking Confirmation",
                    $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm tt")} has been created.",
					 booking.time
				);


                TempData["SuccessMessage"] = "Your booking was successful.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("Index");
            }
        }

        public void CheckUpcomingBookings()
        {
            var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userName == null) return;

            int id = _bookingService.GetuserName(userName);

            var upcomingBookings = _bookingService.GetBookingsWithinNextHour(id);

            foreach (var booking in upcomingBookings)
            {
                var room = _bookingService.GetRooms().FirstOrDefault(r => r.roomId == booking.roomId);
                string roomName = room != null ? room.roomName : "Unknown Room";

                _notificationService.AddNotification(
                    booking.ID,
                    "Upcoming Booking Reminder",
                    $"Reminder: You have a booking for {roomName} in one hour at {booking.time.ToString("HH:mm")}.",
					 booking.time
				);
            }
        }

        [HttpPost]
        public IActionResult EditBooking(Booking booking, string editRecurringUpdate)
        {
            try
            {
				// check model state
				if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToAction("Index");
                }

                var rooms = _bookingService.GetRooms();
                var newRoom = rooms.FirstOrDefault(r => r.roomId == booking.roomId);
                string newRoomName = newRoom != null ? newRoom.roomName : "Unknown Room";

                // get booking
                var existingBooking = _bookingService.GetBookingById(booking.bookingId);

                if (existingBooking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Index");
                }

                // get user ID
                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userName != null)
                {
                    int id = _bookingService.GetuserName(userName);
                    booking.ID = id;
                }

                var oldRoom = rooms.FirstOrDefault(r => r.roomId == existingBooking.roomId); // for notification
                string oldRoomName = oldRoom != null ? oldRoom.roomName : "Unknown Room";

                bool isRoomChanged = existingBooking.roomId != booking.roomId;
                bool isBookingUpdated = existingBooking.date != booking.date || existingBooking.time != booking.time || existingBooking.duration != booking.duration;
                bool isDateChanged = existingBooking.date != booking.date;
                bool isTimeChanged = existingBooking.time != booking.time;
                bool isDurationChanged = existingBooking.duration != booking.duration;

                if (!existingBooking.isRecurring)
                {
					// Check for existing bookings
					var existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, booking.date).Where(b => b.bookingId != booking.bookingId);

					// Calculate the end time of the booking
					var bookingEndTime = booking.time.AddHours(booking.duration);

					// Check for time conflicts
					bool isTimeBooked = existingBookings.Any(b =>
						(b.date == booking.date) && // Same date
						(b.time < bookingEndTime) && // Existing booking starts before new booking ends
						(booking.time < b.time.AddHours(b.duration)) // New booking starts before existing booking ends
					);

					if (isTimeBooked)
					{
						TempData["ErrorMessage"] = $"Error Updating. The room '{newRoomName}' is already booked at this time.";
						return RedirectToAction("Index");
					}

					_bookingService.UpdateBooking(booking);

                    if (isRoomChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Room Change",
                            $"Your booking for {oldRoomName} on {existingBooking.date.ToString("MM-dd-yyyy")} has been moved to {newRoomName}.",
							 booking.time
						);
                    }

                    if (isDateChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Date Change",
                            $"Your booking for {oldRoomName} has been rescheduled to {booking.date.ToString("MM-dd-yyyy")}.",
							 booking.time
						);
                    }
                    if (isTimeChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Duration Change",
                            $"Your booking for {oldRoomName} has been changed to {booking.time}.",
							 booking.time
						);
                    }
                    if (isDurationChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Duration Change",
                            $"Your booking for {oldRoomName} has been changed to {booking.duration} hours.",
							 booking.time
						);
                    }

                    if (isBookingUpdated && !isRoomChanged && !isDateChanged && !isDurationChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Booking Update",
                            $"Your booking for {newRoomName} on {booking.date.ToString("MM-dd-yyyy")} has been updated to {booking.time.ToString("HH:mm")}.",
							 booking.time
						);
                    }

                    TempData["SuccessMessage"] = "Booking updated successfully.";
                }
                else
                {
                    switch (editRecurringUpdate)
                    {
                        case "this":
							// Check for existing bookings
							var existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, booking.date).Where(b => b.bookingId != booking.bookingId);

							// Calculate the end time of the booking
							var bookingEndTime = booking.time.AddHours(booking.duration);

							// Check for time conflicts
							bool isTimeBooked = existingBookings.Any(b =>
								(b.date == booking.date) && // Same date
								(b.time < bookingEndTime) && // Existing booking starts before new booking ends
								(booking.time < b.time.AddHours(b.duration)) // New booking starts before existing booking ends
							);

							if (isTimeBooked)
							{
								TempData["ErrorMessage"] = $"Error Updating. The room '{newRoomName}' is already booked at this time.";
								return RedirectToAction("Index");
							}

							_bookingService.UpdateBooking(booking);

                            if (isRoomChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Room Change",
                                    $"Your booking for {oldRoomName} on {existingBooking.date.ToString("MM-dd-yyyy")} has been moved to {newRoomName}.",
									 booking.time
								);
                            }

                            if (isDateChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Date Change",
                                    $"Your booking for {newRoomName} on has been rescheduled to {booking.date.ToString("MM-dd-yyyy")}.",
									 booking.time
								);
                            }

                            if (isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Duration Change",
                                    $"Your booking for {oldRoomName} has been changed to {booking.duration} hours.",
									 booking.time
								);
                            }

                            if (isBookingUpdated && !isRoomChanged && !isDateChanged && !isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Booking Update",
                                    $"Your booking for {newRoomName} on {booking.date.ToString("MM-dd-yyyy")} has been updated to {booking.time.ToString("HH:mm")}.",
									 booking.time
								);
                            }

                            TempData["SuccessMessage"] = "This occurrence of the booking has been updated.";
                            break;

                        case "following":
                            // get recurring bookings
                            var recurringBookings = _bookingService.GetRecurringBookings(existingBooking.recurringBookingId);
                            var currentBookingDate = existingBooking.date;

							// check for time conflict
							foreach (var recurringBooking in recurringBookings)
							{
								// Get existing bookings for the current date
								existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, recurringBooking.date).Where(b => b.bookingId != recurringBooking.bookingId);

								// Calculate the end time of the booking
								bookingEndTime = booking.time.AddHours(booking.duration);

								// Check for time conflicts for the current date
								bool isTimeBookedForDate = existingBookings.Any(b =>
									(b.date == recurringBooking.date) && // Same date
									(b.time < bookingEndTime) && // Existing booking starts before new booking ends
									(booking.time < b.time.AddHours(b.duration)) // New booking starts before existing booking ends
								);

								if (isTimeBookedForDate)
								{
									TempData["ErrorMessage"] = $"Error Updating. The room '{newRoomName}' is already booked at this time.";
									return RedirectToAction("Index");
								}
							}

                            existingBooking.roomId = booking.roomId;
                            existingBooking.time = booking.time;
                            existingBooking.duration = booking.duration;
							_bookingService.UpdateBooking(existingBooking);

                            if (isRoomChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Room Change",
                                    $"Your booking for {oldRoomName} on {existingBooking.date.ToString("MM-dd-yyyy")} has been moved to {newRoomName} at {booking.time.ToString("HH:mm")}.",
									 booking.time
								);
                            }

                            if (isDateChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Date Change",
                                    $"Your booking for {newRoomName} has been rescheduled to {booking.date.ToString("MM-dd-yyyy")}.",
									 booking.time
								);
                            }

                            if (isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Duration Change",
                                    $"Your booking for {oldRoomName} has been changed to {booking.duration} hours.",
									 booking.time
								);
                            }

                            // check for time conflict
							foreach (var followingBooking in recurringBookings.Where(rb => rb.date > currentBookingDate))
							{
                                // Get existing bookings for the current date
                                existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, followingBooking.date).Where(b => b.bookingId != followingBooking.bookingId);

								// Calculate the end time of the booking
								bookingEndTime = booking.time.AddHours(booking.duration);

								// Check for time conflicts for the current date
								bool isTimeBookedForDate = existingBookings.Any(b =>
									(b.date == followingBooking.date) && // Same date
									(b.time < bookingEndTime) && // Existing booking starts before new booking ends
									(booking.time < b.time.AddHours(b.duration)) // New booking starts before existing booking ends
								);

								if (isTimeBookedForDate)
								{
									TempData["ErrorMessage"] = $"Error Updating. The room '{newRoomName}' is already booked at this time.";
									return RedirectToAction("Index");
								}
							}

							foreach (var followingBooking in recurringBookings.Where(rb => rb.date > currentBookingDate))
                            {
                                followingBooking.roomId = booking.roomId;
                                followingBooking.time = booking.time;
                                followingBooking.duration = booking.duration;
                                _bookingService.UpdateBooking(followingBooking);
                            }

                            if (isBookingUpdated && !isRoomChanged && !isDateChanged && !isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Booking Update",
                                    $"Your booking for {newRoomName} has been updated to {booking.time.ToString("HH:mm")}.",
									 booking.time
								);
                            }

                            TempData["SuccessMessage"] = "This occurrence and all following occurrences have been updated.";
                            break;

                        case "all":
                            var allRecurringBookings = _bookingService.GetRecurringBookings(existingBooking.recurringBookingId);

							// check for time conflict
							foreach (var recurringBooking in allRecurringBookings)
							{
								// Get existing bookings for the current date
								existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, recurringBooking.date).Where(b => b.bookingId != recurringBooking.bookingId);

								// Calculate the end time of the booking
								bookingEndTime = booking.time.AddHours(booking.duration);

								// Check for time conflicts for the current date
								bool isTimeBookedForDate = existingBookings.Any(b =>
									(b.date == recurringBooking.date) && // Same date
									(b.time < bookingEndTime) && // Existing booking starts before new booking ends
									(booking.time < b.time.AddHours(b.duration)) // New booking starts before existing booking ends
								);

								if (isTimeBookedForDate)
								{
									TempData["ErrorMessage"] = $"Error Updating. The room '{newRoomName}' is already booked at this time.";
									return RedirectToAction("Index");
								}
							}

							foreach (var recurringBooking in allRecurringBookings)
                            {
                                recurringBooking.roomId = booking.roomId;
                                recurringBooking.time = booking.time;
                                recurringBooking.duration = booking.duration;
                                _bookingService.UpdateBooking(recurringBooking);
                            }

                            if (isBookingUpdated && !isRoomChanged && !isDateChanged && !isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Booking Update",
                                    $"Your booking for {newRoomName} on {booking.date.ToString("MM-dd-yyyy")} has been updated to {booking.time.ToString("HH:mm")}.",
									 booking.time
								);
                            }

                            TempData["SuccessMessage"] = "All occurrences of the booking have been updated.";
                            break;

                        default:
                            TempData["ErrorMessage"] = "Invalid update option.";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the booking: {ex.Message}";
            }

            return RedirectToAction("Index");
        }



        [HttpGet]
        public IActionResult GetRoomDetails(int id)
        {
            var room = _bookingService.GetRooms().FirstOrDefault(r => r.roomId == id);

            var roomDetails = new
            {
                roomLocation = room.roomLocation,
                roomCapacity = room.roomCapacity, 
                availableFacilities = room.availableFacilities
            };

            return Json(roomDetails); 
        }



        public ActionResult BookingSummary(string room, DateTime? date, string userName)
        {
            var rooms = _bookingService.GetRooms();
            ViewBag.Rooms = new SelectList(rooms, "roomId", "roomName");

            ViewBag.SelectedRoom = room;
            ViewBag.SelectedDate = date;
            ViewBag.SelectedUser = userName;


            (bool result, IEnumerable<Booking> bookings) = _bookingService.GetAllBookings();

            if (!result)
            {
                TempData["ErrorMessage"] = "Failed to load bookings.";
                return View(new List<Booking>());
            }

            // Apply filters
            if (!string.IsNullOrEmpty(room))
            {
                bookings = bookings.Where(b => b.Room.roomId.ToString() == room);
            }

            if (date.HasValue)
            {
                bookings = bookings.Where(b => b.date.Date == date.Value.Date);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                bookings = bookings.Where(b => b.User.userName.Contains(userName, StringComparison.OrdinalIgnoreCase));
            }

            return View(bookings);
        }

        // POST: BookingController/DeleteBooking
        // POST: BookingController/DeleteBooking
        [HttpPost]
        public IActionResult DeleteBooking(int bookingId, string cancelRecurring)
        {
                // get booking
                var booking = _bookingService.GetBookingById(bookingId);
                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userName != null)
                {
                    int id = _bookingService.GetuserName(userName); 
                    booking.ID = id;  
                }
            var rooms = _bookingService.GetRooms();

            var room = rooms.FirstOrDefault(r => r.roomId == booking.roomId);
            string roomName = room != null ? room.roomName : "Unknown Room";
            if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Index");
                }

                if (!booking.isRecurring)
                {
                    // If the booking is not recurring, delete it immediately
                    _bookingService.DeleteBooking(booking);
                _notificationService.AddNotification(
                                booking.ID,
                                "Booking Deletion",
                                 $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled.",
								  booking.time
							);
                TempData["SuccessMessage"] = "Booking deleted successfully.";
                }
                else
                {
                   
                    switch (cancelRecurring)
                    {
                        case "this":
                            // Delete only this occurrence
                            _bookingService.DeleteBooking(booking);
                            _notificationService.AddNotification(
                                booking.ID,
                                "Booking Deletion",
                                 $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled.",
								  booking.time
							);
                            TempData["SuccessMessage"] = "This occurrence of the booking has been deleted.";
                            break;

                        case "following":
                            // get recurring bookings
                            var recurringBookings = _bookingService.GetRecurringBookings(booking.recurringBookingId);
                            var currentBookingDate = booking.date;

                            var followingBookings = recurringBookings
                                .Where(rb => rb.date > currentBookingDate)
                                .ToList();

                            _bookingService.DeleteBooking(booking);

                            foreach (var followingBooking in followingBookings)
                            {
                                _bookingService.DeleteBooking(followingBooking);
                                _notificationService.AddNotification(
                                    followingBooking.ID,
                                    "Booking Deletion",
                                     $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled.",
									  booking.time
								);
                            }

                            TempData["SuccessMessage"] = "This occurrence and all following occurrences have been cancelled.";
                            break;

                        case "all":
                            // Delete all occurrences in the series
                            var allRecurringBookings = _bookingService.GetRecurringBookings(booking.recurringBookingId);
                            foreach (var recurringBooking in allRecurringBookings)
                            {
                                _bookingService.DeleteBooking(recurringBooking);
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Booking Deletion",
                                     $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled.",
									  booking.time
								);
                            }

                            _notificationService.AddNotification(
                                booking.ID,
                                "Booking Deletion",
                                 $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled.",
								  booking.time
							);

                            TempData["SuccessMessage"] = "All occurrences of the booking have been deleted.";
                            break;

                        default:
                            TempData["ErrorMessage"] = "Invalid cancellation option.";
                            break;
                    }
                }
            
           
            return RedirectToAction("Index");
        }

        public ActionResult Sample()
        {
            return View();



        }

		public IActionResult Notifications()
		{
			var notifications = _notificationService.GetNotifications();

			// Log to check the notifications
			foreach (var notification in notifications)
			{
				Console.WriteLine($"Notification: {notification.Message}, Booking Date: {notification.BookingDate}");
			}

			return View(notifications);
		}
		public ActionResult Analytics(string room, DateTime? start, DateTime? end, string userName)
        {
            var rooms = _bookingService.GetRooms();
            ViewBag.Rooms = new SelectList(rooms, "roomId", "roomName");

            ViewBag.SelectedRoom = room;
            ViewBag.SelectedStartDate = start;
            ViewBag.SelectedEndDate = end;
            ViewBag.SelectedUser = userName;

            if (start.HasValue && end.HasValue && end.Value < start.Value)
            {
                TempData["ErrorMessage"] = "Invalid Date Range. The end date cannot be earlier than the start date.";
                return RedirectToAction("Analytics");
            }

            (bool result, IEnumerable<Booking> bookings) = _bookingService.GetAllBookings();

            if (!result)
            {
                TempData["ErrorMessage"] = "Failed to load bookings.";
                return View(new List<BookingAnalyticsViewModel>());
            }

            if (!string.IsNullOrEmpty(room))
            {
                bookings = bookings.Where(b => b.Room.roomId.ToString() == room);
            }

            if (start.HasValue)
            {
                bookings = bookings.Where(b => b.date >= start.Value.Date);
            }

            if (end.HasValue)
            {
                bookings = bookings.Where(b => b.date <= end.Value.Date);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                bookings = bookings.Where(b => b.User.userName.Contains(userName, StringComparison.OrdinalIgnoreCase));
            }

            var analyticsData = bookings
                .GroupBy(b => b.date.Date)
                .Select(g =>
                {
                    var hourlyBookings = g.GroupBy(b => b.time.Hour)
                                        .OrderByDescending(h => h.Count())
                                        .FirstOrDefault();

                    var peakHour = hourlyBookings?.Key ?? 0;
                    var totalBookings = g.Count();

                    string activityLevel = totalBookings switch
                    {
                        var n when n >= 8 => "High",
                        var n when n >= 4 => "Medium",
                        _ => "Low"
                    };

                    return new BookingAnalyticsViewModel
                    {
                        Date = g.Key,
                        TotalBookings = totalBookings,
                        PeakUsageTime = $"{peakHour:00}:00",
                        ActivityLevel = activityLevel
                    };
                })
                .OrderBy(x => x.Date)
                .ToList();

            return View(analyticsData);
        }

        public IActionResult Calendar()
        {
            var rooms = _bookingService.GetRooms();
            ViewBag.Rooms = new SelectList(rooms, "roomId", "roomName");

            // Get the userName from claims
            var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int id = 0;
            if (userName != null)
            {
                id = _bookingService.GetuserName(userName);
            }

            // Get the bookings for the user
            (bool result, IEnumerable<Booking> bookings) = _bookingService.GetBookingsByuserName(id);

            // Check if any booking date is in the past and set the error message
            foreach (var booking in bookings)
            {
                if (booking.date.Date < DateTime.Today)
                {
                    // Set the error message for the toast in ViewBag
                    ViewBag.ErrorMessage = "You cannot book a room for a past date.";
                    break; // We only need to set the error once
                }
            }

            // Set the bookings in a format that FullCalendar expects
            ViewBag.Bookings = bookings.Select(b => new
            {
                title = $"{b.Room.roomName}",
                start = $"{b.date.ToString("yyyy-MM-dd")}T{b.time.ToString("HH:mm:ss")}",
                end = $"{b.date.ToString("yyyy-MM-dd")}T{b.time.AddHours(b.duration).ToString("HH:mm:ss")}"
            });

            return View();
        }


    }
}

