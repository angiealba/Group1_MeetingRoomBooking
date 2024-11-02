using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;

namespace ASI.Basecode.WebApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _userService;

        public AdminController(IAdminService userService)
        {
            _userService = userService;
        }

        public ActionResult Index(string search)
        {
            var users = _userService.GetUsers();

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         u.email.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return View(users.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(UserViewModel userViewModel)
        {
            if (_userService.UserExists(userViewModel.userID))
            {
                ModelState.AddModelError("UserId", "A user with this User ID already exists.");
            }

            if (!ModelState.IsValid)
            {
                var users = _userService.GetUsers().ToList();
                ViewBag.UserViewModel = userViewModel;
                return View("Index", users);
            }

            try
            {
                _userService.AddUser(userViewModel);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the user.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = _userService.GetUsers().FirstOrDefault(u => u.ID == user.ID);
                if (existingUser != null)
                {
                    existingUser.name = user.name;
                    existingUser.email = user.email;
                    existingUser.role = user.role;

                    if (!string.IsNullOrEmpty(user.password))
                    {
                        existingUser.password = PasswordManager.EncryptPassword(user.password);
                    }

                    _userService.UpdateUser(existingUser);
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", "User not found.");
            }

            return View(user);
        }

        [HttpPost]
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            _userService.DeleteUser(id);  // Permanently delete the user
            return RedirectToAction("Index");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Register(UserViewModel model)
        {
            try
            {
                _userService.AddUser(model);
                return RedirectToAction("Index", "SuperAdmin");
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Resources.Messages.Errors.ServerError;
            }
            return View();
        }
        //public ActionResult SuperAdminUsers(string search)
        //{
        //    var users = _userService.GetUsers();

        //    if (!string.IsNullOrEmpty(search))
        //    {
        //        users = users.Where(u => u.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        //                                 u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        //    }

        //    return View(users.ToList());
        //}
    }
}
