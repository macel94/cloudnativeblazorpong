using BlazorPong.Web.Server.Room;
using Microsoft.EntityFrameworkCore;

public class RoomDbContext : DbContext
{
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Client> Clients { get; set; }

    public RoomDbContext(DbContextOptions<RoomDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Clients)
            .WithOne()
            .HasForeignKey(c => c.Id);
    }
}
