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

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // GET: BookingController
        public ActionResult Index(string search, int page = 1, int pageSize = 6)
        {
            int id = 0;
            // Get the userId from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
                                            || b.time.ToString("HH:mm").Contains(search));
            }

            // Pagination logic
            var totalBookings = bookings.Count();
            var totalPages = (int)Math.Ceiling((double)totalBookings / pageSize);
            var paginatedBookings = bookings.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchQuery = search;

            return View(paginatedBookings);
        }

        // POST: BookingController/CreateBooking
        [HttpPost]
        public IActionResult CreateBooking(Booking booking)
        {
            try
            {
                // Validate the booking object
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToAction("Index");
                }

                // Get the userId from claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    int id = _bookingService.GetUserID(userId);
                    booking.ID = id;
                }

                // Handle recurring bookings
                if (booking.isRecurring)
                {
                    // Validate recurrence frequency and end date
                    if (string.IsNullOrEmpty(booking.recurrenceFrequency) || booking.recurrenceEndDate == null)
                    {
                        TempData["ErrorMessage"] = "Please specify a recurrence frequency and end date.";
                        return RedirectToAction("Index");
                    }

                    // Ensure the 'until' date is after the initial booking date
                    if (booking.recurrenceEndDate <= booking.date)
                    {
                        TempData["ErrorMessage"] = "'Until' date must be after the initial booking date.";
                        return RedirectToAction("Index");
                    }

                    // Generate a unique identifier for the recurring series
                    int recurringSeriesId = _bookingService.GetRecurringIdTracker();

                    // Add the original booking with the recurring series ID
                    booking.recurringBookingId = recurringSeriesId; // Set the recurringBookingId to the unique ID
                    _bookingService.AddBooking(booking);

                    // Create multiple bookings based on the recurrence frequency
                    DateTime currentDate = booking.date;
                    while (currentDate < booking.recurrenceEndDate)
                    {
                        // Increment the date based on the recurrence frequency
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

                        // Create a new booking for the current date
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
                            recurringBookingId = booking.recurringBookingId // Reference to the unique recurring series ID
                        };

                        // Add the new booking
                        _bookingService.AddBooking(newBooking);
                    }
                }
                else
                {
                    // If not recurring, set recurrence fields to null
                    booking.recurrenceFrequency = null;
                    booking.recurrenceEndDate = null;
                    booking.recurringBookingId = null;

                    // Add the original booking
                    _bookingService.AddBooking(booking);
                }

                TempData["SuccessMessage"] = "Your booking was successful.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("Index");
            }
        }

        // POST: BookingController/EditBooking
        [HttpPost]
        public IActionResult EditBooking(Booking booking, string editRecurringUpdate)
        {
            try
            {
                // Validate the booking object
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToAction("Index");
                }

                // Get the existing booking by ID
                var existingBooking = _bookingService.GetBookingById(booking.bookingId);

                if (existingBooking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Index");
                }

                if (!existingBooking.isRecurring)
                {
                    // If the booking is not recurring, update it immediately
                    _bookingService.UpdateBooking(booking);
                    TempData["SuccessMessage"] = "Booking updated successfully.";
                }
                else
                {
                    // If the booking is recurring, handle based on editRecurringUpdate value
                    switch (editRecurringUpdate)
                    {
                        case "this":
                            // Update only this occurrence
                            _bookingService.UpdateBooking(booking);
                            TempData["SuccessMessage"] = "This occurrence of the booking has been updated.";
                            break;

                        case "following":
                            // Update this occurrence and all following occurrences
                            var recurringBookings = _bookingService.GetRecurringBookings(existingBooking.recurringBookingId);
                            var currentBookingDate = existingBooking.date;

                            // Update the current booking
                            _bookingService.UpdateBooking(booking);

                            // Update all following bookings
                            foreach (var followingBooking in recurringBookings.Where(rb => rb.date > currentBookingDate))
                            {
                                followingBooking.roomId = booking.roomId;
                                //followingBooking.date = booking.date;
                                followingBooking.time = booking.time;
                                followingBooking.duration = booking.duration;
                                _bookingService.UpdateBooking(followingBooking);
                            }

                            TempData["SuccessMessage"] = "This occurrence and all following occurrences have been updated.";
                            break;

                        case "all":
                            // Update all occurrences in the series
                            var allRecurringBookings = _bookingService.GetRecurringBookings(existingBooking.recurringBookingId);
                            foreach (var recurringBooking in allRecurringBookings)
                            {
                                recurringBooking.roomId = booking.roomId;
                                //recurringBooking.date = booking.date;
                                recurringBooking.time = booking.time;
                                recurringBooking.duration = booking.duration;
                                _bookingService.UpdateBooking(recurringBooking);
                            }
                            TempData["SuccessMessage"] = "All occurrences of the booking have been updated.";
                            break;

                        default:
                            TempData["ErrorMessage"] = "Invalid update option.";
                            break;
                    }
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the booking.";
            }

            return RedirectToAction("Index");
        }
        public ActionResult BookingSummary(string room, DateTime? date, string userName)
        {
            // Get rooms for the dropdown
            var rooms = _bookingService.GetRooms();
            ViewBag.Rooms = new SelectList(rooms, "roomId", "roomName");

            // Store filter values for form persistence
            ViewBag.SelectedRoom = room;
            ViewBag.SelectedDate = date;
            ViewBag.SelectedUser = userName;

            // Fetch all bookings
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
        [HttpPost]
        public IActionResult DeleteBooking(int bookingId, string cancelRecurring)
        {
            try
            {
                // Get the booking by ID
                var booking = _bookingService.GetBookingById(bookingId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Index");
                }

                if (!booking.isRecurring)
                {
                    // If the booking is not recurring, delete it immediately
                    _bookingService.DeleteBooking(booking);
                    TempData["SuccessMessage"] = "Booking deleted successfully.";
                }
                else
                {
                    // If the booking is recurring, handle based on cancelRecurring value
                    switch (cancelRecurring)
                    {
                        case "this":
                            // Delete only this occurrence
                            _bookingService.DeleteBooking(booking);
                            TempData["SuccessMessage"] = "This occurrence of the booking has been deleted.";
                            break;

                        case "following":
                            // Delete this occurrence and all following occurrences
                            var recurringBookings = _bookingService.GetRecurringBookings(booking.recurringBookingId);
                            var currentBookingDate = booking.date;

                            // Filter to get only the bookings that occur after the current booking date
                            var followingBookings = recurringBookings
                                .Where(rb => rb.date > currentBookingDate)
                                .ToList();

                            // Delete the current booking
                            _bookingService.DeleteBooking(booking);

                            // Delete all following bookings
                            foreach (var followingBooking in followingBookings)
                            {
                                _bookingService.DeleteBooking(followingBooking);
                            }

                            TempData["SuccessMessage"] = "This occurrence and all following occurrences have been deleted.";
                            break;

                        case "all":
                            // Delete all occurrences in the series
                            var allRecurringBookings = _bookingService.GetRecurringBookings(booking.recurringBookingId);
                            foreach (var recurringBooking in allRecurringBookings)
                            {
                                _bookingService.DeleteBooking(recurringBooking);
                            }
                            TempData["SuccessMessage"] = "All occurrences of the booking have been deleted.";
                            break;

                        default:
                            TempData["ErrorMessage"] = "Invalid cancellation option.";
                            break;
                    }
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the booking.";
            }

            return RedirectToAction("Index");
        }
        public ActionResult Sample()
        {
            return View();



        }
        public ActionResult Analytics(string room, DateTime? start, DateTime? end, string userName)
        {
            // Get rooms for the dropdown
            var rooms = _bookingService.GetRooms();
            ViewBag.Rooms = new SelectList(rooms, "roomId", "roomName");

            // Store filter values for form persistence
            ViewBag.SelectedRoom = room;
            ViewBag.SelectedStartDate = start;
            ViewBag.SelectedEndDate = end;
            ViewBag.SelectedUser = userName;

            // Fetch all bookings
            (bool result, IEnumerable<Booking> bookings) = _bookingService.GetAllBookings();

            if (!result)
            {
                TempData["ErrorMessage"] = "Failed to load bookings.";
                return View(new List<BookingAnalyticsViewModel>());
            }

            // Apply filters
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

            // Group bookings by date and calculate statistics
            var analyticsData = bookings
                .GroupBy(b => b.date.Date)
                .Select(g =>
                {
                    // Get the hour with most bookings (peak usage time)
                    var hourlyBookings = g.GroupBy(b => b.time.Hour)
                                        .OrderByDescending(h => h.Count())
                                        .FirstOrDefault();

                    var peakHour = hourlyBookings?.Key ?? 0;
                    var totalBookings = g.Count();

                    // Determine activity level
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

    }
}

