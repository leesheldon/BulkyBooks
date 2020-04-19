using BulkyBooks.DataAccess.Data;
using BulkyBooks.DataAccess.Repository.IRepository;
using BulkyBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BulkyBooks.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext _db;

        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Company company)
        {
            var companyFromDb = _db.Companies.FirstOrDefault(s => s.Id == company.Id);
            if (companyFromDb != null)
            {
                companyFromDb.Name = company.Name;
                companyFromDb.StreetAddress = company.StreetAddress;
                companyFromDb.City = company.City;
                companyFromDb.State = company.State;
                companyFromDb.PostalCode = company.PostalCode;
                companyFromDb.PhoneNumber = company.PhoneNumber;
                companyFromDb.IsAuthorizedCompany = company.IsAuthorizedCompany;
            }
        }

    }
}
