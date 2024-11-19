using ASI.Basecode.Data.Models;
using System.Collections.Generic;

namespace ASI.Basecode.Services.Interfaces
{
    public interface INotificationService
    {
        void AddNotification(int id, string type, string message);
        List<Notification> GetNotifications();

        void MarkAsRead(int id);
        void DeleteNotification(int id);
        int GetUserID(string userId);
    }
}
