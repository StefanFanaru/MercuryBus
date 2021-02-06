using Microsoft.EntityFrameworkCore;

namespace MercuryBus.Database
{
    public class MercuryBusDbContextProvider : IMercuryBusDbContextProvider
    {
        private readonly DbContextOptions<MercuryBusDbContext> _dbContextOptions;
        private readonly MercuryBusSchema _mercuryBusSchema;

        public MercuryBusDbContextProvider(DbContextOptions<MercuryBusDbContext>
            dbContextOptions, MercuryBusSchema mercuryBusSchema)
        {
            _mercuryBusSchema = mercuryBusSchema;
            _dbContextOptions = dbContextOptions;
        }

        public MercuryBusDbContext CreateDbContext()
        {
            return new MercuryBusDbContext(_dbContextOptions, _mercuryBusSchema);
        }
    }
}
