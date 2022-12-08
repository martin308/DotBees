using Microsoft.EntityFrameworkCore;

class BeeDb : DbContext
{
    public BeeDb(DbContextOptions<BeeDb> options)
        : base(options) { }

    public DbSet<Bee> Bees => Set<Bee>();
}
