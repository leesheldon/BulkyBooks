using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBooks.DataAccess.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category category);

    }
}
