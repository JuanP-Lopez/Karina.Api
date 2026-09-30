using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Karina.Api.Models;

namespace Karina.Api.Data;

public class KarinaContext : DbContext
{
    public KarinaContext(DbContextOptions<KarinaContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
           entity.ToTable("Users");
           entity.HasKey(u => u.user_id);

           entity.Property(u => u.user_id).HasColumnName("user_id"); 
           entity.Property(u => u.name).HasColumnName("name"); 
           entity.Property(u => u.nickname).HasColumnName("nickname"); 
           entity.Property(u => u.email).HasColumnName("email"); 
           entity.Property(u => u.phone_number).HasColumnName("phone_number"); 
           entity.Property(u => u.password).HasColumnName("password"); 
           entity.Property(u => u.registered_at).HasColumnName("registered_at"); 
        });
    }
}