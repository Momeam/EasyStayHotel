using System;
using System.Linq;
using EasyStayHotel.Models;

namespace EasyStayHotel.Services
{
    public static class DatabaseInitializer
    {
        public static void EnsureBranchRooms(EasystaydbContext context)
        {
            try
            {
                var branches = new[] { "Chonburi", "Phuket" };
                bool changed = false;

                foreach (var branch in branches)
                {
                    int availableCount = context.Rooms.Count(r => r.Branch == branch && r.Status == "Available");
                    if (availableCount == 0)
                    {
                        var existingRooms = context.Rooms.Where(r => r.Branch == branch).ToList();
                        if (existingRooms.Any())
                        {
                            foreach (var room in existingRooms)
                            {
                                room.Status = "Available";
                            }
                            changed = true;
                        }
                        else
                        {
                            var types = context.Roomtypes.Where(rt => rt.RoomTypeId >= 1 && rt.RoomTypeId <= 3).ToList();
                            if (types.Any())
                            {
                                foreach (var type in types)
                                {
                                    string prefix = type.RoomTypeId switch
                                    {
                                        1 => "10",
                                        2 => "20",
                                        3 => "30",
                                        _ => "40"
                                    };

                                    for (int i = 1; i <= 3; i++)
                                    {
                                        context.Rooms.Add(new Room
                                        {
                                            RoomNo = $"{prefix}{i}",
                                            RoomTypeId = type.RoomTypeId,
                                            Status = "Available",
                                            Branch = branch
                                        });
                                    }
                                }
                                changed = true;
                            }
                        }
                    }
                }

                if (changed)
                {
                    context.SaveChanges();
                }
            }
            catch
            {
                // Graceful fallback if database is not reachable at startup
            }
        }
    }
}
