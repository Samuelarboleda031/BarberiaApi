using Microsoft.EntityFrameworkCore;
using BarberiaApi.Domain.Interfaces;
using BarberiaApi.Infrastructure.Data;

namespace BarberiaApi.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly BarberiaContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(BarberiaContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    public async Task<List<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public IQueryable<T> Query() => _dbSet.AsQueryable();
    public void Add(T entity) => _dbSet.Add(entity);
    public void AddRange(IEnumerable<T> entities) => _dbSet.AddRange(entities);
    public void Update(T entity) => _dbSet.Update(entity);
    public void Remove(T entity) => _dbSet.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);
}
