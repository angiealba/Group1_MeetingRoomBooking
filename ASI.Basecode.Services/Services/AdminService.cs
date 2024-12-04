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
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repository;
        private readonly IMapper _mapper;

        public AdminService(IAdminRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        public LoginResult AuthenticateUser(string userID, string password, ref User user)
        {
            if (userID == "superadmin")
            {
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
                user.role = "Admin";

                _repository.AddUser(user);
            }
            else
            {
                throw new InvalidDataException("User already exists");
            }
        }

		public IEnumerable<User> GetUsers() => _repository.GetUsers().Where(u => u.role == "Admin").OrderByDescending(u => u).ToList();

		public void UpdateUser(User user)
        {
            var existingUser = _repository.GetUsers().FirstOrDefault(u => u.userID == user.userID);

            if (existingUser != null)
            {

                existingUser.name = user.name;
                existingUser.email = user.email;
                existingUser.role = "Admin";


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
    }
}
