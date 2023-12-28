using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BlazorPong.Web.Server.src.BlazorPong.Web.Server.EFCore.Models;

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
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC079C63C2C9");

            entity.ToTable("Client");

            entity.Property(e => e.Id).HasMaxLength(50);

            entity.HasOne(d => d.Room).WithMany(p => p.Clients)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Client__RoomId__6FE99F9F");
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
