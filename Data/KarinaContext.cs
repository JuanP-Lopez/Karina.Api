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

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<Source> Sources => Set<Source>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Response> Responses => Set<Response>();

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
           entity.Property(u => u.wa_id).HasColumnName("wa_id");
           entity.Property(u => u.role).HasColumnName("role");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(u => u.session_id);

            entity.Property(u => u.session_id).HasColumnName("session_id");
            entity.Property(u => u.user_id).HasColumnName("user_id");
            entity.Property(u => u.created_at).HasColumnName("created_at");
            entity.Property(u => u.ended_at).HasColumnName("ended_at");
            entity.Property(u => u.last_activity).HasColumnName("last_activity");

            entity.HasOne(u => u.user).WithMany(r => r.sessions).HasForeignKey(u => u.user_id);
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.ToTable("Sources");
            entity.HasKey(u => u.id);

            entity.Property(u => u.id).HasColumnName("id");
            entity.Property(u => u.name).HasColumnName("name");
            entity.Property(u => u.description).HasColumnName("description");
        });

        modelBuilder.Entity<Message>(entity =>
        {
           entity.ToTable("Messages");
           entity.HasKey(u => u.id);

           entity.Property(u => u.id).HasColumnName("id");
           entity.Property(u => u.user_id).HasColumnName("user_id");
           entity.Property(u => u.session_id).HasColumnName("session_id");
           entity.Property(u => u.header).HasColumnName("header");
           entity.Property(u => u.content).HasColumnName("content");
           entity.Property(u => u.source).HasColumnName("source");
           entity.Property(u => u.sent_at).HasColumnName("sent_at");

           entity.HasOne(u => u.user).WithMany(r => r.messages).HasForeignKey(u => u.user_id);
           entity.HasOne(u => u.session).WithMany(r => r.messages).HasForeignKey(u => u.session_id);
           entity.HasOne(u => u.source_nav).WithMany(r => r.messages).HasForeignKey(u => u.source);
        });

        modelBuilder.Entity<Response>(entity =>
        {
           entity.ToTable("Responses");
           entity.HasKey(u => u.id);

           entity.Property(u => u.id).HasColumnName("id");
           entity.Property(u => u.response_to).HasColumnName("response_to");
           entity.Property(u => u.session_id).HasColumnName("session_id");
           entity.Property(u => u.header).HasColumnName("header");
           entity.Property(u => u.content).HasColumnName("content");
           entity.Property(u => u.source).HasColumnName("source");
           entity.Property(u => u.sent_at).HasColumnName("sent_at");

           entity.HasOne(u => u.message).WithOne(r => r.response).HasForeignKey<Response>(u => u.response_to);
           entity.HasOne(u => u.session).WithMany(r => r.responses).HasForeignKey(u => u.session_id);
           entity.HasOne(u => u.source_nav).WithMany(r => r.responses).HasForeignKey(u => u.source);
        });
    }
}