using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BulkyBooks.Models.ViewModels;
using BulkyBooks.DataAccess.Repository.IRepository;
using BulkyBooks.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BulkyBooks.Utilities;
using Microsoft.AspNetCore.Http;

namespace BulkyBooks.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var count = _unitOfWork.ShoppingCart
                .GetAll(i => i.ApplicationUserId == claim.Value)
                .ToList().Count();

                HttpContext.Session.SetInt32(SD.ssShoppingCart, count);
            }

            return View(productList);
        }

        public IActionResult Details(int id)
        {
            var productFromDb = _unitOfWork.Product.GetFirstOrDefault(i => i.Id == id, includeProperties: "Category,CoverType");
            ShoppingCart cart = new ShoppingCart
            {
                Product = productFromDb,
                ProductId = productFromDb.Id
            };

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart cartFromUI)
        {
            cartFromUI.Id = 0;
            if (!ModelState.IsValid)
            {
                // Model state is not valid
                var productFromDb = _unitOfWork.Product.GetFirstOrDefault(i => i.Id == cartFromUI.ProductId, includeProperties: "Category,CoverType");
                ShoppingCart cartObj = new ShoppingCart
                {
                    Product = productFromDb,
                    ProductId = productFromDb.Id
                };

                return View(cartObj);
            }

            // We will add to the cart
            // Get User login Id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            cartFromUI.ApplicationUserId = claim.Value;

            // Get this User's shopping cart from DB
            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(
                i => i.ApplicationUserId == cartFromUI.ApplicationUserId && i.ProductId == cartFromUI.ProductId
                , includeProperties: "Product");

            if (cartFromDb == null)
            {
                // This User have no Shopping Cart now.
                _unitOfWork.ShoppingCart.Add(cartFromUI);
            }
            else
            {
                cartFromDb.Count += cartFromUI.Count;
            }

            _unitOfWork.Save();
            
            var count = _unitOfWork.ShoppingCart
                .GetAll(i => i.ApplicationUserId == cartFromUI.ApplicationUserId)
                .ToList().Count();

            //HttpContext.Session.SetObject(SD.ssShoppingCart, cartFromUI);
            HttpContext.Session.SetInt32(SD.ssShoppingCart, count);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
