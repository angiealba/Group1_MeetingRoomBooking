﻿using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuGet.Protocol;
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
        private readonly IBookingService _bookingService;
        public UserController(IUserService userService, INotificationService notificationService, IBookingService bookingService)
        {
            _userService = userService;
            _notificationService = notificationService;
            _bookingService = bookingService;
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
            if (_userService.UserExists(userViewModel.userName))
            {
                ModelState.AddModelError("userName", "A user with this User ID already exists.");
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
                    if (!string.Equals(existingUser.email, user.email, StringComparison.OrdinalIgnoreCase))
                    {
                        var emailExists = _userService.GetUsers().Any(u => u.email == user.email);
                        if (emailExists)
                        {
                            TempData["ErrorMessage"] = "Email is already registered.";
                            return RedirectToAction("Index");
                        }
                    }
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
                var emailExists = _userService.GetUsers().Any(u => u.email == model.email);
                if (emailExists)
                {
                    TempData["ErrorMessage"] = "Email is already registered.";
                    return RedirectToAction("Index");
                }
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

            var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

           
            if (userName != null)
            {
                id = _notificationService.GetuserName(userName); 
            }

           
            var notifications = _notificationService.GetNotifications()
                .Where(n => n.userName == id)
                .OrderByDescending(n => n.Date)
                .ToList();

            var bookings = _bookingService.GetBookingsByuserName(id);
            ViewBag.Bookings = JsonConvert.SerializeObject(bookings);
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

            return RedirectToAction("Notification");
        }

        [HttpPost]
        [HttpPost]
        public IActionResult SaveSettings(bool? enableNotifications, int defaultBookingDuration)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userName == null)
                {
                    return Unauthorized();
                }

                bool notificationsEnabled = enableNotifications ?? false;

                _userService.UpdateUserSettings(userName, notificationsEnabled, defaultBookingDuration);

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
            var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userName == null)
            {
                return Unauthorized();
            }

            var user = _userService.GetUsers().FirstOrDefault(u => u.userName == userName);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            return View(user);
        }


    }
}

