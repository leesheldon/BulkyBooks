using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBooks.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Category { get; }

        IStoredProc_Call StoredProc_Call { get; }

    }
}
