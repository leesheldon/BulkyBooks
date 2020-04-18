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
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult InsertOrUpdate(int? id)
        {
            Category category = new Category();
            if (id == null)
            {
                // Create new Category
                return View(category);
            }

            // Update Category
            category = _unitOfWork.Category.Get(id.GetValueOrDefault());
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InsertOrUpdate(Category category)
        {
            if (ModelState.IsValid)
            {
                if(category.Id == 0)
                {
                    _unitOfWork.Category.Add(category);
                }
                else
                {
                    _unitOfWork.Category.Update(category);
                }

                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var allCat = _unitOfWork.Category.GetAll();
            return Json(new { data = allCat });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var catFromDb = _unitOfWork.Category.Get(id);
            if (catFromDb == null)
            {
                return Json(new { success = false, message = "Error in deleting Category!" });
            }

            _unitOfWork.Category.Remove(catFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Category successful!" });
        }

        #endregion

    }
}