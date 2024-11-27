using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

public class NotificationService : INotificationService
{
	private readonly INotificationRepository _notificationRepository;
	private readonly IUserRepository _userRepository;
	private readonly IBookingRepository _bookingRepository; // Assuming you have this repository for booking-related database operations

	public NotificationService(INotificationRepository notificationRepository, IUserRepository userRepository, IBookingRepository bookingRepository)
	{
		_notificationRepository = notificationRepository;
		_userRepository = userRepository;
		_bookingRepository = bookingRepository;
	}

	// Method to create a booking and send a notification
	public void CreateBooking(int userId, DateTime bookingTime)
	{
		// Create the booking
		var booking = new Booking
		{
			time = bookingTime,
			// Set other booking details like roomId, userId, etc.
		};

		// Save the booking to the database (you need to make sure the booking is saved)
		_bookingRepository.AddBooking(booking);  // Assuming you have an AddBooking method

		// After creating the booking, send a notification with the booking's time
		var message = $"Your booking is scheduled for {bookingTime.ToString("f")}.";
		AddNotification(userId, "Booking", message, booking.time);  // Pass the time field directly as bookingTime
	}

	// Method to create a notification
	public void AddNotification(int userId, string type, string message, DateTime? bookingDate)
	{
		if (userId == 0 || string.IsNullOrEmpty(message))
		{
			throw new ArgumentException("Invalid notification parameters.");
		}

		// Check if notifications are enabled for the user
		var user = _userRepository.GetUsers().FirstOrDefault(u => u.ID == userId);
		if (user == null || !user.enableNotifications)
		{
			return; // Do not add notification if user is not found or notifications are disabled
		}

		var notification = new Notification
		{
			userId = userId,
			Type = type,
			Message = message,
			Date = DateTime.Now,
			BookingDate = bookingDate,  // Set the booking date to the passed nullable DateTime
			IsRead = false
		};

		_notificationRepository.AddNotification(notification);  // Save the notification
	}

	// Get all notifications
	public List<Notification> GetNotifications()
	{
		return _notificationRepository.GetNotifications();  // Ensure this returns List<Notification>
	}

	// Mark notification as read
	public void MarkAsRead(int id)
	{
		_notificationRepository.MarkAsRead(id);
	}

	public int GetUserID(string userId)
	{
		return _notificationRepository.GetUserID(userId);
	}
	// Delete notification
	public void DeleteNotification(int id)
	{
		_notificationRepository.DeleteNotification(id);
	}
}
