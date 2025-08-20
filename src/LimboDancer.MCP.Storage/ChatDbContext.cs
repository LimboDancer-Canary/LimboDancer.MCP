using LimboDancer.MCP.Core.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace LimboDancer.MCP.Storage;

public sealed class ChatDbContext : DbContext
{
    private readonly ITenantAccessor _tenant;

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MemoryItem> MemoryItems => Set<MemoryItem>();

    public ChatDbContext(DbContextOptions<ChatDbContext> options, ITenantAccessor tenant)
        : base(options) => _tenant = tenant;

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Session
        b.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TenantId).IsRequired();
            e.Property(x => x.UserId).HasMaxLength(256).IsRequired();
            e.Property(x => x.TagsJson).HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz")
                .HasDefaultValueSql("now() at time zone 'utc'");
            e.HasIndex(x => new { x.TenantId, x.CreatedAt });
            e.HasMany(x => x.Messages).WithOne(m => m.Session).HasForeignKey(m => m.SessionId);
        });

        // Message
        b.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TenantId).IsRequired();
            e.Property(x => x.Content).HasColumnType("text").IsRequired();
            e.Property(x => x.ToolCallsJson).HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz")
                .HasDefaultValueSql("now() at time zone 'utc'");
            e.HasIndex(x => new { x.TenantId, x.SessionId, x.CreatedAt });
        });

        // MemoryItem
        b.Entity<MemoryItem>(e =>
        {
            e.ToTable("memory_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TenantId).IsRequired();
            e.Property(x => x.ExternalId).HasMaxLength(256).IsRequired();
            e.Property(x => x.MetaJson).HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz")
                .HasDefaultValueSql("now() at time zone 'utc'");
            e.HasIndex(x => new { x.TenantId, x.Kind, x.CreatedAt });
            e.HasIndex(x => new { x.TenantId, x.Kind, x.ExternalId }).IsUnique(false);
        });

        // Global filters (only applied if TenantId != Guid.Empty)
        var tenantId = _tenant.TenantId;
        if (tenantId != Guid.Empty)
        {
            b.Entity<Session>().HasQueryFilter(x => x.TenantId == tenantId);
            b.Entity<Message>().HasQueryFilter(x => x.TenantId == tenantId);
            b.Entity<MemoryItem>().HasQueryFilter(x => x.TenantId == tenantId);
        }
    }
}
