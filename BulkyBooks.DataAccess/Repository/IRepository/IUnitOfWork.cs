using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBooks.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Category { get; }

        ICoverTypeRepository CoverType { get; }

        IProductRepository Product { get; }

        ICompanyRepository Company { get; }

        IApplicationUserRepository ApplicationUser { get; }

        IStoredProc_Call StoredProc_Call { get; }

        void Save();
    }
}
