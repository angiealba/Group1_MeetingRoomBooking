using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System.Collections.Generic;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserService
    {
        LoginResult AuthenticateUser(string userID, string password, ref User user);
        void AddUser(UserViewModel model);
        IEnumerable<User> GetUsers();
        void UpdateUser(User user);
        void DeleteUser(int ID);
        bool UserExists(string userID);
        void UpdateUserSettings(string userId, bool enableNotifications, int defaultBookingDuration);
        User GetUserByUserId(int id);
    }
}
