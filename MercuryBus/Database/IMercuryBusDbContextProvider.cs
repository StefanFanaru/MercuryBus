namespace MercuryBus.Database
{
    public interface IMercuryBusDbContextProvider
    {
        MercuryBusDbContext CreateDbContext();
    }
}