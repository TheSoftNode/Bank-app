namespace Data_service.Repository;

using Data_service.Data;
using Data_service.IRepository;
using Entities_Dtos.DBSets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected SMSAlertDbContext _context;
    protected DbSet<T> dbSet;
    protected readonly ILogger<GenericRepository<T>> _logger;

    public GenericRepository(SMSAlertDbContext context, ILogger<GenericRepository<T>> logger)
    {
        _context = context;
        _logger = logger;
        dbSet = context.Set<T>();
    }

    public virtual async Task<T> GetByIdAsync(Guid id)
    {
        try
        {
            return await dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetByIdAsync method error", typeof(GenericRepository<T>));
            return null;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await dbSet.AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} GetAllAsync method error", typeof(GenericRepository<T>));
            return new List<T>();
        }
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            await dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} AddAsync method error", typeof(GenericRepository<T>));
            return null!;
        }
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        try
        {
            dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} UpdateAsync method error", typeof(GenericRepository<T>));
            return false;
        }
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await dbSet.FindAsync(id);
            if (entity == null) return false;
            dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Repo} DeleteAsync method error", typeof(GenericRepository<T>));
            return false;
        }
    }

    //public virtual async Task<bool> ExistsAsync(Guid id)
    //{
    //    try
    //    {
    //        return await dbSet.AnyAsync(e => e.Id == id);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "{Repo} ExistsAsync method error", typeof(GenericRepository<T>));
    //        return false;
    //    }
    //}
}
