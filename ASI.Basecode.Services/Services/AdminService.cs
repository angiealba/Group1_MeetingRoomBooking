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

        public LoginResult AuthenticateUser(string userName, string password, ref User user)
        {
            if (userName == "superadmin")
            {
                user = _repository.GetUsers().FirstOrDefault(x => x.userName == userName && x.role == "Superadmin" && x.password == password);
                return user != null ? LoginResult.Success : LoginResult.Failed;
            }

            user = _repository.GetUsers().FirstOrDefault(x => x.userName == userName && x.password == PasswordManager.EncryptPassword(password));
            return user != null ? LoginResult.Success : LoginResult.Failed;
        }

        // functions and methods operations for storing data to database

        public void AddUser(UserViewModel model)
        {
            if (!_repository.UserExists(model.userName))// if user does not exist proceed to add if exxist throw data exception
            {
                var user = _mapper.Map<User>(model);
                user.password = PasswordManager.EncryptPassword(model.password);
                user.createdTime = DateTime.Now;
                user.updatedTime = DateTime.Now;
                user.createdBy = Environment.UserName;// know who updated or created 
                user.updatedBy = Environment.UserName;
                user.role = "Admin";

                _repository.AddUser(user);
            }
            else
            {
                throw new InvalidDataException("User already exists");
            }
        }

        public IEnumerable<User> GetUsers() => _repository.GetUsers().Where(u => u.role == "Admin").OrderByDescending(u => u).ToList(); // sorting and list pagination

        public void UpdateUser(User user)
        {
            var existingUser = _repository.GetUsers().FirstOrDefault(u => u.userName == user.userName);// retrieve users then finds the ifrst user then match the userName

            if (existingUser != null)// checks if an user is found
            {

                existingUser.name = user.name;// proceed to update
                existingUser.email = user.email;
                existingUser.role = "Admin";


                if (!string.IsNullOrEmpty(user.password) && user.password != existingUser.password) // compares the new pass to exis user pass
                {
                    existingUser.password = PasswordManager.EncryptPassword(user.password);//proceeds to encrypt
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

        public bool UserExists(string userName) => _repository.UserExists(userName);
    }
}
