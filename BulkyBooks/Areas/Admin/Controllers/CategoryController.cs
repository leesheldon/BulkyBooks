using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBooks.DataAccess.Repository.IRepository;
using BulkyBooks.Models;
using BulkyBooks.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
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

        public async Task<IActionResult> InsertOrUpdate(int? id)
        {
            Category category = new Category();
            if (id == null)
            {
                // Create new Category
                return View(category);
            }

            // Update Category
            category = await _unitOfWork.Category.Get_Async(id.GetValueOrDefault());
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InsertOrUpdate(Category category)
        {
            if (ModelState.IsValid)
            {
                if(category.Id == 0)
                {
                    await _unitOfWork.Category.Add_Async(category);
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
        public async Task<IActionResult> GetAll()
        {
            var allCat = await _unitOfWork.Category.GetAll_Async();
            return Json(new { data = allCat });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var catFromDb = await _unitOfWork.Category.Get_Async(id);
            if (catFromDb == null)
            {
                return Json(new { success = false, message = "Error in deleting Category!" });
            }

            await _unitOfWork.Category.Remove_Async(catFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Category successful!" });
        }

        #endregion

    }
}