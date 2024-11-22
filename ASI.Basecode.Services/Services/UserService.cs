using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Services
{
	public class UserService : IUserService
	{
		private readonly IUserRepository _repository;
		private readonly IMapper _mapper;

		public UserService(IUserRepository repository, IMapper mapper)
		{
			_mapper = mapper;
			_repository = repository;
		}

		public LoginResult AuthenticateUser(string userID, string password, ref User user)
		{
			if (userID == "superadmin")
			{
				// Checking if the user has the 'Superadmin' role
				user = _repository.GetUsers().FirstOrDefault(x => x.userID == userID && x.role == "Superadmin" && x.password == password);
				return user != null ? LoginResult.Success : LoginResult.Failed;
			}

			user = _repository.GetUsers().FirstOrDefault(x => x.userID == userID && x.password == PasswordManager.EncryptPassword(password));
			return user != null ? LoginResult.Success : LoginResult.Failed;
		}

		public void AddUser(UserViewModel model)
		{
			if (!_repository.UserExists(model.userID))
			{
				var user = _mapper.Map<User>(model);
				user.password = PasswordManager.EncryptPassword(model.password);
				user.createdTime = DateTime.Now;
				user.updatedTime = DateTime.Now;
				user.createdBy = Environment.UserName;
				user.updatedBy = Environment.UserName;
				user.role = "User";

				_repository.AddUser(user);
			}
			else
			{
				throw new InvalidDataException("User already exists");
			}
		}

		public IEnumerable<User> GetUsers() => _repository.GetUsers().ToList();

		public void UpdateUser(User user)
		{
			var existingUser = _repository.GetUsers().FirstOrDefault(u => u.userID == user.userID);

			if (existingUser != null)
			{

				existingUser.name = user.name;
				existingUser.email = user.email;
				existingUser.role = "User";


				if (!string.IsNullOrEmpty(user.password) && user.password != existingUser.password)
				{
					existingUser.password = PasswordManager.EncryptPassword(user.password);
				}

				_repository.UpdateUser(existingUser);
			}
		}
		public void DeleteUser(int id)
		{
			var user = _repository.GetUsers().FirstOrDefault(u => u.ID == id);
			if (user != null)
			{
				_repository.DeleteUser(user);
			}
		}
		public bool UserExists(string userID) => _repository.UserExists(userID);
		public void UpdateUserSettings(string userId, bool enableNotifications, int defaultBookingDuration)
		{
			var user = _repository.GetUsers().FirstOrDefault(u => u.userID == userId);

			if (user != null)
			{
				// Directly assign the value without ternary operation
				user.enableNotifications = enableNotifications;
				user.defaultBookingDuration = defaultBookingDuration;
				user.updatedTime = DateTime.Now;
				user.updatedBy = Environment.UserName;

				// Update the user in the repository
				_repository.UpdateUser(user);
			}
		}



		public User GetUserByUserId(int id)
		{
			return _repository.GetUsers().FirstOrDefault(u => u.ID == id);
		}
	}
}