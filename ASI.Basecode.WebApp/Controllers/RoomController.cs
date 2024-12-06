using ASI.Basecode.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ASI.Basecode.Data.Models;
using System.IO;
using System.Linq;
using System;

namespace ASI.Basecode.WebApp.Controllers
{
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

		public IActionResult Index(string search, int page = 1, int pageSize = 10)
        {
        (bool result, IEnumerable<Room> rooms) = _roomService.GetRooms();

        if (!result)
        {
            return View(null);
        }

        if (!string.IsNullOrEmpty(search))
        {
            rooms = rooms.Where(r => r.roomName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                      r.roomLocation.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var totalRooms = rooms.Count();
        var totalPages = (int)Math.Ceiling((double)totalRooms / pageSize);
        var paginatedRooms = rooms.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchQuery = search;

        return View(paginatedRooms);
    }


		public IActionResult CreateRoom()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public IActionResult CreateRoom(Room room)
        {
            try
            {
                if (_roomService.RoomExists(room.roomName))
                {
                    
                    TempData["ErrorMessage"] = "A room with this name already exists!";
                    return RedirectToAction("Index");
                }

                _roomService.AddRoom(room);
                TempData["SuccessMessage"] = "Room created successfully!";
                return RedirectToAction("Index");
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(room);
            }
        }


        public IActionResult Delete(int roomId)
        {
            (bool result, IEnumerable<Room> rooms) = _roomService.GetRooms();
            var room = rooms.FirstOrDefault(r => roomId == r.roomId);
            if (room != null)
            {
                _roomService.DeleteRoom(room);
                TempData["SuccessMessage"] = "Room deleted successfully!";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Update(int roomId)
        {
            (bool result, IEnumerable<Room> rooms) = _roomService.GetRooms();
            var room = rooms.FirstOrDefault(r => roomId == r.roomId);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        [HttpPost]
        public IActionResult Update(Room room)
        {
            (bool result, IEnumerable<Room> rooms) = _roomService.GetRooms();

            if (!result)
            {
                TempData["ErrorMessage"] = "An error occurred while retrieving rooms.";
                return RedirectToAction("Index");
            }

            var existingRoom = rooms.FirstOrDefault(r => r.roomId == room.roomId);

            if (existingRoom == null)
            {
                TempData["ErrorMessage"] = "Room not found.";
                return RedirectToAction("Index");
            }


            if (!string.Equals(existingRoom.roomName, room.roomName, StringComparison.OrdinalIgnoreCase))
            {
                if (rooms.Any(r => r.roomName.Equals(room.roomName, StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["ErrorMessage"] = "A room with this name already exists!";
                    return RedirectToAction("Index");
                }
            }

            if (ModelState.IsValid)
            {
                _roomService.UpdateRoom(room);
                TempData["SuccessMessage"] = "Room updated successfully!";
                return RedirectToAction("Index");
            }
            return View(room);
        }
    }
}
