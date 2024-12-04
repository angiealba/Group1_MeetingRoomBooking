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

		public ActionResult Index(string search, int page = 1, int pageSize = 10)
		{
			var users = _userService.GetUsers(); // Get all users

			// search query
			if (!string.IsNullOrEmpty(search))// if search is provided filter by name and email
			{
				users = users.Where(u => (u.name != null && u.name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
										 (u.email != null && u.email.Contains(search, StringComparison.OrdinalIgnoreCase)));
			}


			// Pagination logic
			var totalUsers = users.Count(); // get total users af filtering
			var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize); // cal total page
			var paginatedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

			// display
			ViewBag.CurrentPage = page; 
			ViewBag.TotalPages = totalPages;
			ViewBag.SearchQuery = search;

			return View(paginatedUsers);
		}

        // functions and methods operations to handle UI operations

		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(UserViewModel userViewModel)
        {
            if (_userService.UserExists(userViewModel.userID))// checks user duplicates
            {
                ModelState.AddModelError("UserId", "A user with this User ID already exists."); // if dup prompt error
            }

            if (!ModelState.IsValid)//  checks if the model has no errors
            {
                var users = _userService.GetUsers().ToList();// retrieves current list of users
                ViewBag.UserViewModel = userViewModel; // passes users from controlelr to view
                return View("Index", users); // reutnr to view
            }

            try
            {
                _userService.AddUser(userViewModel); // adding users
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the user."; // db issue prompt error
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (ModelState.IsValid)
            {                      // retrieves list of users then match users
                var existingUser = _userService.GetUsers().FirstOrDefault(u => u.ID == user.ID); // if not match existuser is null
                if (existingUser != null) // checks if users exist, iff exists proceed to update user
                {
                    existingUser.name = user.name; // update details
                    existingUser.email = user.email;
                    existingUser.role = user.role;

                    if (!string.IsNullOrEmpty(user.password))// if not empty encrypts
                    {
                        existingUser.password = PasswordManager.EncryptPassword(user.password);
                    }

                    _userService.UpdateUser(existingUser); // saves updated details to db
                    TempData["SuccessMessage"] = "Admin successfully updated";
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
            TempData["SuccessMessage"] = "Admin has been deleted";
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
                _userService.AddUser(model);// calls method AddUser to add user to db
                TempData["SuccessMessage"] = "Admin successfully added";
                return RedirectToAction("Index", "Admin");
            }
            catch (InvalidDataException ex) // error if user alreaady exist
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
        
    }
}
