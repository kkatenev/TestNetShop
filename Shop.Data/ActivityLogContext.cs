using Microsoft.EntityFrameworkCore;

namespace Shop.Data;

public class ActivityLogContext(DbContextOptions<ActivityLogContext> options) : DbContext(options)
{
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
}
