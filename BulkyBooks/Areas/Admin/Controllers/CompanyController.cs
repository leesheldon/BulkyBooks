using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBooks.DataAccess.Repository.IRepository;
using BulkyBooks.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult InsertOrUpdate(int? id)
        {
            Company company = new Company();
            if (id == null)
            {
                // Create new Company
                return View(company);
            }

            // Update Company
            company = _unitOfWork.Company.Get(id.GetValueOrDefault());
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InsertOrUpdate(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    _unitOfWork.Company.Add(company);
                }
                else
                {
                    _unitOfWork.Company.Update(company);
                }

                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            return View(company);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var allCompanies = _unitOfWork.Company.GetAll();
            return Json(new { data = allCompanies });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var companyFromDb = _unitOfWork.Company.Get(id);
            if (companyFromDb == null)
            {
                return Json(new { success = false, message = "Error in deleting Company!" });
            }

            _unitOfWork.Company.Remove(companyFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Company successful!" });
        }

        #endregion

    }
}