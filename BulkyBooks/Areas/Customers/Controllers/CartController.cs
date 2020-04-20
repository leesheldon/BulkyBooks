using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BulkyBooks.DataAccess.Repository.IRepository;
using BulkyBooks.Models;
using BulkyBooks.Models.ViewModels;
using BulkyBooks.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BulkyBooks.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;

        public ShoppingCartVM cartVM { get; set; }
        
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            cartVM = new ShoppingCartVM
            {
                OrderHeader = new OrderHeader()
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {                
                cartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == claim.Value, includeProperties: "Product");
                cartVM.OrderHeader.OrderTotal = 0;
                cartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(i => i.Id == claim.Value, includeProperties: "Company");

                foreach (var item in cartVM.ListCart)
                {
                    item.Price = SD.GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);
                    cartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
                    item.Product.Description = SD.ConvertToRawHtml(item.Product.Description);
                    if (item.Product.Description.Length > 100)
                    {
                        item.Product.Description = item.Product.Description.Substring(0, 99) + "...";
                    }
                }
            }
                        
            return View(cartVM);
        }
    
        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.ApplicationUser.GetFirstOrDefault(i => i.Id == claim.Value);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Verification email is empty!");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            ModelState.AddModelError(string.Empty, "Verification email is sent. Please check your email!");
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(i => i.Id == cartId, includeProperties: "Product");
            cartFromDb.Count += 1;
            cartFromDb.Price = SD.GetPriceBasedOnQuantity(cartFromDb.Count, cartFromDb.Product.Price, cartFromDb.Product.Price50, cartFromDb.Product.Price100);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(i => i.Id == cartId, includeProperties: "Product");
            if (cartFromDb.Count == 1)
            {
                var countOfCarts = _unitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == cartFromDb.ApplicationUserId).ToList().Count();
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.ssShoppingCart, countOfCarts - 1);
            }
            else
            {
                cartFromDb.Count -= 1;
                cartFromDb.Price = SD.GetPriceBasedOnQuantity(cartFromDb.Count, cartFromDb.Product.Price, cartFromDb.Product.Price50, cartFromDb.Product.Price100);
                _unitOfWork.Save();
            }
            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(i => i.Id == cartId, includeProperties: "Product");
            var countOfCarts = _unitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == cartFromDb.ApplicationUserId).ToList().Count();
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.ssShoppingCart, countOfCarts - 1);

            return RedirectToAction(nameof(Index));
        }

    }
}