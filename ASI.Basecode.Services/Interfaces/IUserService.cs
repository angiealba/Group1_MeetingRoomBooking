using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System.Collections.Generic;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
	public interface IUserService
	{
		LoginResult AuthenticateUser(string userName, string password, ref User user);
		void AddUser(UserViewModel model);
		IEnumerable<User> GetUsers();
		void UpdateUser(User user);
		void DeleteUser(int ID);
		bool UserExists(string userName);
		void UpdateUserSettings(string userName, bool enableNotifications, int defaultBookingDuration);

		User GetUserByuserName(int id);
	}
}
