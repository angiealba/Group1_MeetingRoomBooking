using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Repositories
{
    public class RoomRepository : BaseRepository, IRoomRepository
    {
        List<Room> _allRoom = new();
        private readonly AsiBasecodeDBContext _dbContext;

        public RoomRepository(AsiBasecodeDBContext dbContext, IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _dbContext = dbContext;
        }
        public IEnumerable<Room> ViewRooms()
        {
            return _dbContext.Rooms.ToList();
        }

        public void AddRoom(Room room)
        {
            try
            {
                var newRoom = new Room();
                newRoom.roomName = room.roomName;
                newRoom.roomLocation = room.roomLocation;
                newRoom.roomCapacity = room.roomCapacity;
                newRoom.availableFacilities = room.availableFacilities;
                _dbContext.Rooms.Add(newRoom);
                _dbContext.SaveChanges();
            }
            catch (Exception)
            {
                throw new InvalidDataException("error adding rooms");
            }
        }
        public void DeleteRoom(Room room)
        {
            _dbContext.Rooms.Remove(room);
            _dbContext.SaveChanges();
        }

        public void UpdateRoom(Room room)
        {
            var existingRoom = _dbContext.Rooms.FirstOrDefault(r => r.roomId == room.roomId);
            if (existingRoom != null)
            {
                existingRoom.roomName = room.roomName;
                existingRoom.roomLocation = room.roomLocation;
                existingRoom.roomCapacity = room.roomCapacity;
                existingRoom.availableFacilities = room.availableFacilities;

                _dbContext.Rooms.Update(existingRoom);
                _dbContext.SaveChanges();

            }
        }
        public (bool, IEnumerable<Room>) GetRooms()
        {
            throw new NotImplementedException();
        }
    }
}
