using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DLL_ConnectionToDatabase
{
    public interface IEntityWithId
    {
        int Id { get; set; }
    }

    public static class DbMenu
    {
        public static AppDbContext CreateContext(
            string ip, 
            string? dbLogin = null, 
            string? dbPassword = null)
        {
            var builder = new SqlConnectionStringBuilder
            {
                InitialCatalog = "ConceptPlay",
                Encrypt = false 
            };

            if (dbLogin is null)
            {
                builder.DataSource = @"localhost\SQLEXPRESS";
                builder.IntegratedSecurity = true;        
                builder.TrustServerCertificate = true;   
            }
            else
            {
                builder.DataSource = ip;
                builder.UserID = dbLogin;                
                builder.Password = dbPassword;
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(builder.ConnectionString);

            return new AppDbContext(optionsBuilder.Options);
        }

        public static async Task InsertAsync<T>
            (T obj, AppDbContext context) 
        where T : class, IEntityWithId
        {
            context.Set<T>().Add(obj);
            await context.SaveChangesAsync();
        }

        public static async Task UpdateAsync<T>(
            T obj, AppDbContext context) 
        where T : class, IEntityWithId
        {

            context.Set<T>().Update(obj);
            await context.SaveChangesAsync();
        }

        public static async Task DeleteAsync<T>(
            T obj, AppDbContext context) 
        where T : class, IEntityWithId
        {
            context.Set<T>().Remove(obj);
            await context.SaveChangesAsync();
        }

        private static IQueryable<T> BuildQuery<T>(
            AppDbContext context) 
        where T : class, IEntityWithId
        {
            var dbSet = context.Set<T>()
                .AsQueryable();

            var entityType = context.Model
                .FindEntityType(typeof(T));

            if (entityType != null)
            {
                foreach (var navigation in entityType.GetNavigations())
                    dbSet = dbSet.Include(navigation.Name);
            }

            if (typeof(T) == typeof(Concept))
            {
                dbSet = (IQueryable<T>)((IQueryable<Concept>) dbSet)
                    .Include(c => c.Genres);
            }

            return dbSet;
        }

        public static async Task<List<T>> SelectAsync<T>(
            Expression<Func<T, bool>>? predicate,
            AppDbContext context)
        where T : class, IEntityWithId
        {
            var query = BuildQuery<T>(context);

            if (predicate is not null)
                query = query.Where(predicate);

            return await query.ToListAsync();
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(
            Expression<Func<T, bool>> predicate,
            AppDbContext context) 
        where T : class, IEntityWithId
        {
            return await BuildQuery<T>(context)
                .FirstOrDefaultAsync(predicate);
        }

        public static async Task<T?> GetByIdAsync<T>(
            int id, AppDbContext context) 
        where T : class, IEntityWithId
        {
            return await BuildQuery<T>(context)
                .FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}
