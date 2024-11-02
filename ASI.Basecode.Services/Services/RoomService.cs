using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        public RoomService(IRoomRepository roomRepository)
        {
            this._roomRepository = roomRepository;
        }

        public (bool, IEnumerable<Room>) GetRooms()
        {

            var rooms = _roomRepository.ViewRooms();
            if (rooms != null)
            {
                return (true, rooms);
            }
            return (false, null);
        }

        public void AddRoom(Room room)
        {
            if (room == null)
            {
                throw new ArgumentException();
            }
            var newRoom = new Room();

            newRoom.roomName = room.roomName;
            newRoom.roomLocation = room.roomLocation;
            newRoom.roomCapacity = room.roomCapacity;
            newRoom.availableFacilities = room.availableFacilities;
            _roomRepository.AddRoom(newRoom);

        }
        public void DeleteRoom(Room room)
        {
            if (room == null)
            {
                throw new ArgumentException();
            }


            _roomRepository.DeleteRoom(room);
        }

        public void UpdateRoom(Room room)
        {
            if (room == null)
            {
                throw new ArgumentException();
            }


            _roomRepository.UpdateRoom(room);
        }
    }
}
