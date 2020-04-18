using BulkyBooks.DataAccess.Data;
using BulkyBooks.DataAccess.Repository.IRepository;
using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BulkyBooks.DataAccess.Repository
{
    public class CoverTypeRepository : Repository<CoverType>, ICoverTypeRepository
    {
        private readonly ApplicationDbContext _db;

        public CoverTypeRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(CoverType coverType)
        {
            var ctFromDb = _db.CoverTypes.FirstOrDefault(s => s.Id == coverType.Id);
            if (ctFromDb != null)
            {
                ctFromDb.Name = coverType.Name;
            }
        }
    }
}
