
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

    public NotificationService(INotificationRepository notificationRepository, IUserRepository userRepository)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
    }

    public void AddNotification(int id, string type, string message)
    {
        if (id == 0 || string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Invalid notification parameters.");
        }

        // Check if notifications are enabled for the user
        var user = _userRepository.GetUsers().FirstOrDefault(u => u.ID == id);
        if (user == null || !user.enableNotifications)
        {
            return; // Do not add notification if user is not found or notifications are disabled
        }

        var notification = new Notification
        {
            userId = id,
            Type = type,
            Message = message,
            Date = DateTime.Now,
            IsRead = false
        };

        _notificationRepository.AddNotification(notification);
    }

    public List<Notification> GetNotifications()
    {
        return _notificationRepository.GetNotifications();  // Ensure this returns List<Notification>
    }

    public int GetUserID(string userId)
    {
        return _notificationRepository.GetUserID(userId);
    }

    public void MarkAsRead(int id)
    {
        _notificationRepository.MarkAsRead(id);
    }

    public void DeleteNotification(int id)
    {
        _notificationRepository.DeleteNotification(id);

    }
    
}
