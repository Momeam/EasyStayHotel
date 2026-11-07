using Microsoft.AspNetCore.Mvc;
using EasyStayHotel.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace EasyStayHotel.Controllers
{
    public class StaffController : Controller
    {
        private readonly EasystaydbContext _context;

        public StaffController(EasystaydbContext context)
        {
            _context = context;
        }

        // GET: StaffController
        public ActionResult Index()
        {
            var rooms = _context.Rooms.Include(r => r.RoomType).ToList();

            return View(rooms);
        }

        public IActionResult CreateRoom()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CreateRoom(Room newRoom)
        {
            if (ModelState.IsValid)
            {
                _context.Rooms.Add(newRoom);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(newRoom);
        }

        public IActionResult EditRoom(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }
        [HttpPost]
        public IActionResult EditRoom(Room updatedRoom)
        {
            var roomInDb = _context.Rooms.Find(updatedRoom.RoomId);
            if (roomInDb != null)
            {   
                roomInDb.Status = updatedRoom.Status;
                
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(updatedRoom);
        }

        public IActionResult DeleteRoom(int id)
        {
            var room = _context.Rooms.Find(id);

            if (room != null)
            {
                _context.Rooms.Remove(room);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ระบบ walk-in

        public IActionResult WalkinBooking()
        {
            var availableRooms = _context.Rooms
                .Where(r => r.Status == "Available")
                .ToList();

            ViewBag.AvailableRooms = availableRooms;
            return View ();
        }

        // Httppost
    [HttpPost]
        public IActionResult WalkinBooking(Booking newBooking)
        {
            newBooking.BookingRef = "WK-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            newBooking.BookingStatus = "Confirmed";

            _context.Bookings.Add(newBooking);

            var room = _context.Rooms.Find(newBooking.RoomId);
            if (room != null)
            {
                room.Status = "Occupied";
            }
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult BookingList()
        {
            var bookings = _context.Bookings
            .Include(b => b.Room)
            .OrderByDescending(b => b.CheckInDate)
            .ToList();

            return View(bookings);
        }
    }
}