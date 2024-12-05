using ASI.Basecode.Data.Models;
using System;
using System.Collections.Generic;

namespace ASI.Basecode.Services.Interfaces
{
    public interface INotificationService
    {
		void AddNotification(int userName, string type, string message, DateTime? bookingDate);
		List<Notification> GetNotifications();

        void MarkAsRead(int id);
        void DeleteNotification(int id);
        int GetuserName(string userName);
    }
}
