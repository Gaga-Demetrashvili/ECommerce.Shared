using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Shared.Infrastructure.Outbox;

internal class OutboxContextFactory : IDesignTimeDbContextFactory<OutboxContext>
{
    public OutboxContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OutboxContext>();
        optionsBuilder.UseSqlServer();

        return new OutboxContext(optionsBuilder.Options);
    }
}
