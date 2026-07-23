using System.Linq.Expressions;

namespace clinicManagementSystem.Repositories.IRepositories
{
    public interface IRepository<T> where T : class
    {
        Task CreateAsync(T entity, CancellationToken cancellationToken = default);

        public void Update(T entity);

        public void Delete(T entity);


        Task<int> CommitAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? expression = null,
            Func<IQueryable<T>,
            IOrderedQueryable<T>>? orderBy = null,
            Expression<Func<T, object>>?[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default);


        Task<T?> GetOneAsync(
            Expression<Func<T, bool>>? expression = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Expression<Func<T, object>>?[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default);
    }
    
    }

