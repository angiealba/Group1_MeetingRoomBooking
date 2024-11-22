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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            User currentUser = null;

            if (userId != null)
            {
                id = _bookingService.GetUserID(userId);
                currentUser = _userService.GetUserByUserId(id); 
            }


            ViewBag.defaultBookingDuration = currentUser?.defaultBookingDuration ?? 1;

            if (userId != null)
            {
                id = _bookingService.GetUserID(userId);
            }

            (bool result, IEnumerable<Booking> bookings) = _bookingService.GetBookingsByUserId(id);
            var rooms = _bookingService.GetRooms();
            ViewBag.Rooms = new SelectList(rooms, "roomId", "roomName");

            if (!result)
            {
                return View(null);
            }


            if (!string.IsNullOrEmpty(search))
            {
                bookings = bookings.Where(b => b.Room.roomName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                            || b.date.ToString("MM-dd-yyyy").Contains(search)
                                            || b.time.ToString("HH:mm").Contains(search)
                                            || b.bookingRefId.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

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
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToAction("Index");
                }
                if (booking.date.Date < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "You cannot book a room for a past date.";
                    return RedirectToAction("Index");
                }
                var rooms = _bookingService.GetRooms(); 

                var room = rooms.FirstOrDefault(r => r.roomId == booking.roomId);
                string roomName = room != null ? room.roomName : "Unknown Room";

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    int id = _bookingService.GetUserID(userId);
                    booking.ID = id;
                }

                if (booking.isRecurring)
                {
                    int numBookings = 0; // number of bookings created

                    if (string.IsNullOrEmpty(booking.recurrenceFrequency) || booking.recurrenceEndDate == null)
                    {
                        TempData["ErrorMessage"] = "Please specify a recurrence frequency and end date.";
                        return RedirectToAction("Index");
                    }

                    if (booking.recurrenceEndDate <= booking.date)
                    {
                        TempData["ErrorMessage"] = "'Until' date must be after the initial booking date.";
                        return RedirectToAction("Index");
                    }

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

                    int recurringSeriesId = _bookingService.GetRecurringIdTracker();

                    booking.recurringBookingId = recurringSeriesId;

                    if (!isTimeBooked)
                    {
                        _bookingService.AddBooking(booking);
                        numBookings++;
                    }

                    DateTime currentDate = booking.date;
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

                        // Check for existing bookings for recurring dates
                        existingBookings = _bookingService.GetBookingsByRoomAndDate(booking.roomId, currentDate);
                        isTimeBooked = existingBookings.Any(b =>
                            (b.date == currentDate) && // Same date
                            (b.time < bookingEndTime) && 
                            (booking.time < b.time.AddHours(b.duration))
                        );

                        if (!isTimeBooked)
                        {
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
                            numBookings++;
                        }
                    }

                    if(numBookings == 0)
                    {
                        TempData["ErrorMessage"] = $"The room '{roomName}' is already booked at this time.";
                        return RedirectToAction("Index");
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
                    $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been created."
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            int id = _bookingService.GetUserID(userId);

            var upcomingBookings = _bookingService.GetBookingsWithinNextHour(id);

            foreach (var booking in upcomingBookings)
            {
                var room = _bookingService.GetRooms().FirstOrDefault(r => r.roomId == booking.roomId);
                string roomName = room != null ? room.roomName : "Unknown Room";

                _notificationService.AddNotification(
                    booking.ID,
                    "Upcoming Booking Reminder",
                    $"Reminder: You have a booking for {roomName} in one hour at {booking.time.ToString("HH:mm")}."
                );
            }
        }

        [HttpPost]
        public IActionResult EditBooking(Booking booking, string editRecurringUpdate)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToAction("Index");
                }

                var rooms = _bookingService.GetRooms();
                var newRoom = rooms.FirstOrDefault(r => r.roomId == booking.roomId);
                string newRoomName = newRoom != null ? newRoom.roomName : "Unknown Room";

                var existingBooking = _bookingService.GetBookingById(booking.bookingId);

                if (existingBooking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Index");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    int id = _bookingService.GetUserID(userId);
                    booking.ID = id;
                }

                var oldRoom = rooms.FirstOrDefault(r => r.roomId == existingBooking.roomId);
                string oldRoomName = oldRoom != null ? oldRoom.roomName : "Unknown Room";

                bool isRoomChanged = existingBooking.roomId != booking.roomId;
                bool isBookingUpdated = existingBooking.date != booking.date || existingBooking.time != booking.time || existingBooking.duration != booking.duration;
                bool isDateChanged = existingBooking.date != booking.date;
                bool isTimeChanged = existingBooking.time != booking.time;
                bool isDurationChanged = existingBooking.duration != booking.duration;

                if (!existingBooking.isRecurring)
                {
                    _bookingService.UpdateBooking(booking);

                    if (isRoomChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Room Change",
                            $"Your booking for {oldRoomName} on {existingBooking.date.ToString("MM-dd-yyyy")} has been moved to {newRoomName}."
                        );
                    }

                    if (isDateChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Date Change",
                            $"Your booking for {oldRoomName} has been rescheduled to {booking.date.ToString("MM-dd-yyyy")}."
                        );
                    }
                    if (isTimeChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Duration Change",
                            $"Your booking for {oldRoomName} has been changed to {booking.time}."
                        );
                    }
                    if (isDurationChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Duration Change",
                            $"Your booking for {oldRoomName} has been changed to {booking.duration} hours."
                        );
                    }

                    if (isBookingUpdated && !isRoomChanged && !isDateChanged && !isDurationChanged)
                    {
                        _notificationService.AddNotification(
                            booking.ID,
                            "Booking Update",
                            $"Your booking for {newRoomName} on {booking.date.ToString("MM-dd-yyyy")} has been updated to {booking.time.ToString("HH:mm")}."
                        );
                    }

                    TempData["SuccessMessage"] = "Booking updated successfully.";
                }
                else
                {
                    switch (editRecurringUpdate)
                    {
                        case "this":
                            _bookingService.UpdateBooking(booking);

                            if (isRoomChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Room Change",
                                    $"Your booking for {oldRoomName} on {existingBooking.date.ToString("MM-dd-yyyy")} has been moved to {newRoomName}."
                                );
                            }

                            if (isDateChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Date Change",
                                    $"Your booking for {newRoomName} on has been rescheduled to {booking.date.ToString("MM-dd-yyyy")}."
                                );
                            }

                            if (isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Duration Change",
                                    $"Your booking for {oldRoomName} has been changed to {booking.duration} hours."
                                );
                            }

                            if (isBookingUpdated && !isRoomChanged && !isDateChanged && !isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Booking Update",
                                    $"Your booking for {newRoomName} on {booking.date.ToString("MM-dd-yyyy")} has been updated to {booking.time.ToString("HH:mm")}."
                                );
                            }

                            TempData["SuccessMessage"] = "This occurrence of the booking has been updated.";
                            break;

                        case "following":
                            var recurringBookings = _bookingService.GetRecurringBookings(existingBooking.recurringBookingId);
                            var currentBookingDate = existingBooking.date;

                            _bookingService.UpdateBooking(booking);

                            if (isRoomChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Room Change",
                                    $"Your booking for {oldRoomName} on {existingBooking.date.ToString("MM-dd-yyyy")} has been moved to {newRoomName} at {booking.time.ToString("HH:mm")}."
                                );
                            }

                            if (isDateChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Date Change",
                                    $"Your booking for {newRoomName} has been rescheduled to {booking.date.ToString("MM-dd-yyyy")}."
                                );
                            }

                            if (isDurationChanged)
                            {
                                _notificationService.AddNotification(
                                    booking.ID,
                                    "Duration Change",
                                    $"Your booking for {oldRoomName} has been changed to {booking.duration} hours."
                                );
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
                                    $"Your booking for {newRoomName} has been updated to {booking.time.ToString("HH:mm")}."
                                );
                            }

                            TempData["SuccessMessage"] = "This occurrence and all following occurrences have been updated.";
                            break;

                        case "all":
                            var allRecurringBookings = _bookingService.GetRecurringBookings(existingBooking.recurringBookingId);
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
                                    $"Your booking for {newRoomName} on {booking.date.ToString("MM-dd-yyyy")} has been updated to {booking.time.ToString("HH:mm")}."
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
                bookings = bookings.Where(b => b.User.userID.Contains(userName, StringComparison.OrdinalIgnoreCase));
            }

            return View(bookings);
        }

        // POST: BookingController/DeleteBooking
        // POST: BookingController/DeleteBooking
        [HttpPost]
        public IActionResult DeleteBooking(int bookingId, string cancelRecurring)
        {
            
                var booking = _bookingService.GetBookingById(bookingId);
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    int id = _bookingService.GetUserID(userId); 
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
                                 $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled."
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
                                 $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled."
                            );
                            TempData["SuccessMessage"] = "This occurrence of the booking has been deleted.";
                            break;

                        case "following":
                           
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
                                     $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled."
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
                                     $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled."
                                );
                            }

                            _notificationService.AddNotification(
                                booking.ID,
                                "Booking Deletion",
                                 $"Your booking for {roomName} on {booking.date.ToString("MM-dd-yyyy")} at {booking.time.ToString("HH:mm")} has been cancelled."
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
                bookings = bookings.Where(b => b.User.userID.Contains(userName, StringComparison.OrdinalIgnoreCase));
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

            // Get the userId from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int id = 0;
            if (userId != null)
            {
                id = _bookingService.GetUserID(userId);
            }

            // Get the bookings for the user
            (bool result, IEnumerable<Booking> bookings) = _bookingService.GetBookingsByUserId(id);

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

