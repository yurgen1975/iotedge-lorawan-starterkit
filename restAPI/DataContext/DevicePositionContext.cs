using Microsoft.EntityFrameworkCore;
using restAPI.DataContracts;

namespace restAPI.DataContext.Models
{
    public class DevicePositionContext : DbContext
    {
        public DevicePositionContext(DbContextOptions<DevicePositionContext> options)
            : base(options)
        {
        }

        public DbSet<DeviceMapPoint> DevicePositions { get; set; }
    }
}