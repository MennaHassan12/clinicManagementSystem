using clinicManagementSystem.Repositories.IRepositories;
using ClinicManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace clinicManagementSystem.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;
        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        //Create
        public async Task CreateAsync(T entity,CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
        }

        //Update
        public  void  Update(T entity)
        {
             _dbSet.Update(entity);
        }

        //Delete
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        //commit
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
        }

        //Get
  
        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>>? expression = null, Func<IQueryable<T>, 
            IOrderedQueryable<T>>? orderBy = null,
            Expression<Func<T, object>>?[]? includes = null,
             bool tracked = true,
             CancellationToken cancellationToken = default)
        {
            IQueryable<T> entities = _dbSet.AsQueryable();

            // Apply Filter
            if (expression is not null)
                entities = entities.Where(expression);

            // Apply Includes
            if (includes is not null)
            {
                foreach (var item in includes)
                {
                    if (item is not null)
                        entities = entities.Include(item);
                }
            }

            // Apply Ordering
            if (orderBy is not null)
                entities = orderBy(entities);

            // Apply Tracking
            if (!tracked)
                entities = entities.AsNoTracking();

            return await entities.ToListAsync(cancellationToken);
        }
  
        public async Task<T?> GetOneAsync(
            Expression<Func<T, bool>>? expression = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Expression<Func<T, object>>?[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default)
        {
            return (await GetAsync(expression, orderBy, includes, tracked, cancellationToken)).FirstOrDefault();
        }
    }
}
