using MercuryBus.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MercuryBus.IntegrationTests.TestHelpers
{
	/// <summary>
	/// Used to ensure a new model is created for each different MercurySchema
	/// Otherwise, the first model created is cached and used even if the MercurySchema has changed
	/// </summary>
	public class DynamicMercurySchemaModelCacheKeyFactory : IModelCacheKeyFactory
	{
		public object Create(DbContext context)
		{
			if (context is MercuryBusDbContext mercuryBusDbContext)
			{
				return (context.GetType(), mercuryBusDbContext.MercuryBusDatabaseSchema);
			}
			return context.GetType();
		}
	}
}
