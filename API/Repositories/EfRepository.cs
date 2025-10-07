using API.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace API.Repositories
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        protected readonly AppDBContext _db;
        protected readonly DbSet<T> _set;

        public EfRepository(AppDBContext db)
        {
            _db = db;
            _set = _db.Set<T>();
        }

        // Henter via id

        public async Task<T?> GetByIdAsync(int id, bool asNoTracking = true)
        {
            if (asNoTracking)
                return await _set.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);

            return await _set.FindAsync(id);
        }

        // List med filter

        public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = true)
        {
            IQueryable<T> q = _set;
            if (predicate != null) q = q.Where(predicate);
            if (asNoTracking) q = q.AsNoTracking();
            return await q.ToListAsync();
        }

        public Task AddAsync(T entity) { _set.Add(entity); return Task.CompletedTask; }
        public void Update(T entity) => _set.Update(entity);
        public void Remove(T entity) => _set.Remove(entity);
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}