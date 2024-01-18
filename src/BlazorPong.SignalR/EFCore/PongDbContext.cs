using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BlazorPong.SignalR.EFCore;

public partial class PongDbContext : DbContext
{
    public PongDbContext(DbContextOptions<PongDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => new { e.Username, e.ConnectionId }).HasName("PK__tmp_ms_x__776823AC2783C4ED");

            entity.ToTable("Client");

            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.ConnectionId).HasMaxLength(50);

            entity.HasOne(d => d.Room).WithMany(p => p.Clients)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Client__RoomId__02FC7413");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Room__3214EC0783A5D6A8");

            entity.ToTable("Room");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ServerName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
