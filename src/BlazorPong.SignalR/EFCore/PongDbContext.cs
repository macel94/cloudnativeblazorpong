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
            entity.HasKey(e => new { e.Username, e.ConnectionId }).HasName("pk_client");

            entity.ToTable("client");

            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.ConnectionId).HasMaxLength(50);

            entity.HasOne(d => d.Room).WithMany(p => p.Clients)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_client_room");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_room");

            entity.ToTable("room");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ServerName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
