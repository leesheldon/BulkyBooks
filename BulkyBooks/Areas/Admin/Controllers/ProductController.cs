using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBooks.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using BulkyBooks.Models;
using Microsoft.AspNetCore.Hosting;
using BulkyBooks.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using BulkyBooks.Utilities;

namespace BulkyBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult InsertOrUpdate(int? id)
        {
            ProductVM vm = new ProductVM()
            {
                Product = new Product(),
                CategoryList=_unitOfWork.Category.GetAll().Select(i => new SelectListItem { 
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            if (id == null)
            {
                // Create new Category
                return View(vm);
            }

            // Update Category
            vm.Product = _unitOfWork.Product.Get(id.GetValueOrDefault());
            if (vm.Product == null)
            {
                return NotFound();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InsertOrUpdate(ProductVM vm)
        {
            if (ModelState.IsValid)
            {
                if (vm.Product.Id != 0)
                {
                    Product productFromDb = _unitOfWork.Product.Get(vm.Product.Id);
                    vm.Product.ImageUrl = productFromDb.ImageUrl;
                }

                string webRootPath = _hostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(webRootPath, @"images\products");
                    var extension = Path.GetExtension(files[0].FileName);

                    if (vm.Product.ImageUrl != null)
                    {
                        // User Edit Product, change new image and we need to remove old image
                        var old_ImagePath = Path.Combine(webRootPath, vm.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(old_ImagePath))
                        {
                            System.IO.File.Delete(old_ImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        files[0].CopyTo(fileStream);
                    }

                    vm.Product.ImageUrl = @"\images\products\" + fileName + extension;
                }                

                // Add new or Update Product
                if (vm.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(vm.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(vm.Product);
                }

                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }

            // State of data validation is False
            vm.CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });

            vm.CoverTypeList = _unitOfWork.CoverType.GetAll().Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });

            if (vm.Product.Id != 0)
            {
                vm.Product = _unitOfWork.Product.Get(vm.Product.Id);
            }

            return View(vm);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var allProducts = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
            return Json(new { data = allProducts });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var proFromDb = _unitOfWork.Product.Get(id);
            if (proFromDb == null)
            {
                return Json(new { success = false, message = "Error in deleting Product!" });
            }

            // We need to delete physical image before delete Product
            string webRootPath = _hostEnvironment.WebRootPath;
            var imagePath = Path.Combine(webRootPath, proFromDb.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            // Delete Product
            _unitOfWork.Product.Remove(proFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Product successful!" });
        }

        #endregion

    }
}