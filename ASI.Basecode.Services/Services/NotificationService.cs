
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using System;
using System.Collections.Generic;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public void AddNotification(int id, string type, string message)
    {
        if (id == 0 || string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Invalid notification parameters.");
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

    public void MarkAsRead(int id)
    {
        _notificationRepository.MarkAsRead(id);
    }

    public void DeleteNotification(int id)
    {
        _notificationRepository.DeleteNotification(id);
    }
}
