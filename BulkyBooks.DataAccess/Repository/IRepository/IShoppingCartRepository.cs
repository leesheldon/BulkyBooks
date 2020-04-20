using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulkyBooks.DataAccess.Repository.IRepository
{
    public interface IShoppingCartRepository : IRepository<ShoppingCart>
    {
        void Update(ShoppingCart shoppingCart);

    }
}
