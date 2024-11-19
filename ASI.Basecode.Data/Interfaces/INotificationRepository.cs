using ASI.Basecode.Data.Models;
using System.Collections.Generic;

public interface INotificationRepository
{
    void AddNotification(Notification notification);
    List<Notification> GetNotifications();
    void MarkAsRead(int id);
    void DeleteNotification(int id);
}
