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

        public IActionResult Index(string search)
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

            return View(rooms.ToList());
        }

        public IActionResult CreateRoom()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateRoom(Room room)
        {
            try
            {
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
