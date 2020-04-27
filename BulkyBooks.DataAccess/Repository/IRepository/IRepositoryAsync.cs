using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBooks.DataAccess.Repository.IRepository
{
    public interface IRepositoryAsync<T> where T : class
    {
        Task<T> Get_Async(int id);

        Task<IEnumerable<T>> GetAll_Async(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = null);

        Task<T> GetFirstOrDefault_Async(
            Expression<Func<T, bool>> filter = null,
            string includeProperties = null);

        Task Add_Async(T entity);

        Task Remove_Async(int id);

        Task Remove_Async(T entity);

        Task RemoveRange_Async(IEnumerable<T> entity);

    }
}
