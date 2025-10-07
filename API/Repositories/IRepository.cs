using System.Linq.Expressions;

namespace API.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, bool asNoTracking = true);
        Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = true);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        Task<int> SaveChangesAsync();
    }
}
