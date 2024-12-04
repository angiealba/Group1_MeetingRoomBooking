using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace ASI.Basecode.WebApp.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        public UserController(IUserService userService, INotificationService notificationService)
        {
            _userService = userService;
            _notificationService = notificationService;
        }

        public ActionResult Index(string search, int page = 1, int pageSize = 10)
        {
            var users = _userService.GetUsers();

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u => u.name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         u.email.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var totalUsers = users.Count();
            var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            var paginatedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchQuery = search;

            return View(paginatedUsers);
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
                    TempData["SuccessMessage"] = "User successfully updated";
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
            _userService.DeleteUser(id);
            TempData["SuccessMessage"] = "User successfully deleted";
            return RedirectToAction("Index");
        }
       

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Register(UserViewModel model)
        {
            try
            {
                _userService.AddUser(model);
                TempData["SuccessMessage"] = "User successfully added";
                return RedirectToAction("Index", "User");
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Resources.Messages.Errors.ServerError;
            }
            TempData["ErrorMessage"] = "Username is already registered";
            return RedirectToAction("Index");
        }
        public ActionResult Notification()
        {
            
            int id = 0;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

           
            if (userId != null)
            {
                id = _notificationService.GetUserID(userId); 
            }

           
            var notifications = _notificationService.GetNotifications()
                .Where(n => n.userId == id)
                .OrderByDescending(n => n.Date)
                .ToList();

            return View(notifications);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteNotification(int id)
        {
            try
            {
                _notificationService.DeleteNotification(id);
                TempData["SuccessMessage"] = "Notification deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete the notification: " + ex.Message;
            }

            // Redirect back to the Notifications page
            return RedirectToAction("Notification");
        }

        [HttpPost]
        [HttpPost]
        public IActionResult SaveSettings(bool? enableNotifications, int defaultBookingDuration)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                bool notificationsEnabled = enableNotifications ?? false;

                _userService.UpdateUserSettings(userId, notificationsEnabled, defaultBookingDuration);

                TempData["SuccessMessage"] = "Settings updated successfully.";
                return RedirectToAction("Setting");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to update settings: {ex.Message}";
                return RedirectToAction("Setting");
            }
        }

        public ActionResult Setting()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            // Retrieve the updated user from the database
            var user = _userService.GetUsers().FirstOrDefault(u => u.userID == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            return View(user);
        }


    }
}

