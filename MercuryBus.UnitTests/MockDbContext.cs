using System.Collections.Generic;
using System.Linq;
using MercuryBus.Database;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace MercuryBus.UnitTests
{
    public class MockDbContext<T>
        where T : class
    {
        public static MercuryBusDbContext Create() => Create(new List<T>());

        public static MercuryBusDbContext Create(List<T> entities)
        {
            var queryable = entities.AsQueryable();
            var mockSet = Substitute.For<DbSet<T>, IQueryable<T>>();

            // Query the set
            ((IQueryable<T>)mockSet).Provider.Returns(queryable.Provider);
            ((IQueryable<T>)mockSet).Expression.Returns(queryable.Expression);
            ((IQueryable<T>)mockSet).ElementType.Returns(queryable.ElementType);
            ((IQueryable<T>)mockSet).GetEnumerator().Returns(queryable.GetEnumerator());

            // Modify the set
            mockSet.When(set => set.Add(Arg.Any<T>())).Do(info => entities.Add(info.Arg<T>()));
            mockSet.When(set => set.Remove(Arg.Any<T>())).Do(info => entities.Remove(info.Arg<T>()));

            var dbContext = Substitute.For<MercuryBusDbContext>();
            dbContext.Set<T>().Returns(mockSet);

            return dbContext;
        }
    }
}
