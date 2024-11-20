using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Repositories
{
    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        public IQueryable<User> GetUsers() => GetDbSet<User>();

        public bool UserExists(string userID) => GetDbSet<User>().Any(x => x.userID == userID);

        public void AddUser(User user)
        {
            GetDbSet<User>().Add(user);
            UnitOfWork.SaveChanges();
        }

        public void UpdateUser(User user)
        {
            var existingUser = GetDbSet<User>().FirstOrDefault(u => u.userID == user.userID);

            if (existingUser != null)
            {
                existingUser.name = user.name;
                existingUser.email = user.email;
                existingUser.role = user.role;
                existingUser.defaultBookingDuration = user.defaultBookingDuration;
                if (!string.IsNullOrEmpty(user.password))
                {
                    existingUser.password = user.password;
                }

                GetDbSet<User>().Update(existingUser);
                UnitOfWork.SaveChanges();
            }
        }

        public void DeleteUser(User user)
        {
            GetDbSet<User>().Remove(user);
            UnitOfWork.SaveChanges();
        }
    }
}
