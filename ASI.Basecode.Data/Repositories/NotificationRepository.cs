using ASI.Basecode.Data;
using ASI.Basecode.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class NotificationRepository : INotificationRepository
{
    private readonly AsiBasecodeDBContext _dbContext;

    public NotificationRepository(AsiBasecodeDBContext context)
    {
        _dbContext = context;
    }

    public void AddNotification(Notification notification)
    {
        _dbContext.Notifications.Add(notification);
        _dbContext.SaveChanges();
    }

    public List<Notification> GetNotifications()
    {
        return _dbContext.Notifications
                       .OrderByDescending(n => n.Date)
                       .Take(5)
                       .ToList();
    }

    public void MarkAsRead(int id)
    {
        var notification = _dbContext.Notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            notification.IsRead = true;
            _dbContext.SaveChanges();
        }
    }
    public int GetuserName(string userName)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.userName == userName);
        if (user == null)
        {
            throw new Exception("User not found!");
        }
        return user.ID;
    }

    public void DeleteNotification(int id)
    {
        var notification = _dbContext.Notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            _dbContext.Notifications.Remove(notification);
            _dbContext.SaveChanges();
        }
    }
}
