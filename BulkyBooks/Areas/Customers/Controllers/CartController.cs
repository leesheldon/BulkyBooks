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
using Stripe;

namespace BulkyBooks.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
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

        public IActionResult Summary()
        {
            cartVM = new ShoppingCartVM
            {
                OrderHeader = new OrderHeader()
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                cartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(i => i.Id == claim.Value, includeProperties: "Company");
                cartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == claim.Value, includeProperties: "Product");

                foreach (var item in cartVM.ListCart)
                {
                    item.Price = SD.GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);
                    cartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
                }

                cartVM.OrderHeader.Name = cartVM.OrderHeader.ApplicationUser.Name;
                cartVM.OrderHeader.PhoneNumber = cartVM.OrderHeader.ApplicationUser.PhoneNumber;
                cartVM.OrderHeader.StreetAddress = cartVM.OrderHeader.ApplicationUser.StreetAddress;
                cartVM.OrderHeader.City = cartVM.OrderHeader.ApplicationUser.City;
                cartVM.OrderHeader.State = cartVM.OrderHeader.ApplicationUser.State;
                cartVM.OrderHeader.PostalCode = cartVM.OrderHeader.ApplicationUser.PostalCode;
            }

            return View(cartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                cartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(i => i.Id == claim.Value, includeProperties: "Company");
                cartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == claim.Value, includeProperties: "Product");
                foreach (var item in cartVM.ListCart)
                {
                    item.Price = SD.GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);
                    cartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
                }

                cartVM.OrderHeader.Name = cartVM.OrderHeader.ApplicationUser.Name;
                cartVM.OrderHeader.PhoneNumber = cartVM.OrderHeader.ApplicationUser.PhoneNumber;
                cartVM.OrderHeader.StreetAddress = cartVM.OrderHeader.ApplicationUser.StreetAddress;
                cartVM.OrderHeader.City = cartVM.OrderHeader.ApplicationUser.City;
                cartVM.OrderHeader.State = cartVM.OrderHeader.ApplicationUser.State;
                cartVM.OrderHeader.PostalCode = cartVM.OrderHeader.ApplicationUser.PostalCode;

                return View(cartVM);
            }                                             

            // Add new Order Header into Database
            if (claim != null)
            {
                cartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(i => i.Id == claim.Value, includeProperties: "Company");

                cartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                cartVM.OrderHeader.OrderStatus = SD.StatusPending;
                cartVM.OrderHeader.ApplicationUserId = claim.Value;
                cartVM.OrderHeader.OrderDate = DateTime.Now;
                _unitOfWork.OrderHeader.Add(cartVM.OrderHeader);
                _unitOfWork.Save();
            }

            // Add new Order Details into Database
            cartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == claim.Value, includeProperties: "Product");
            List<OrderDetails> detailsList = new List<OrderDetails>();
            foreach (var item in cartVM.ListCart)
            {
                item.Price = SD.GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);

                OrderDetails orderDetails = new OrderDetails()
                {
                    ProductId = item.ProductId,
                    OrderId = cartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };

                cartVM.OrderHeader.OrderTotal += orderDetails.Count * orderDetails.Price;
                _unitOfWork.OrderDetails.Add(orderDetails);                
            }

            // Remove Shopping Cart
            _unitOfWork.ShoppingCart.RemoveRange(cartVM.ListCart);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.ssShoppingCart, 0);

            if (string.IsNullOrEmpty(stripeToken))
            {
                // Order will be created for delayed payment for authorized company.
                cartVM.OrderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
                cartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                cartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            else
            {
                // Process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(cartVM.OrderHeader.OrderTotal * 100),
                    Currency = "usd",
                    Description = "Order Id: " + cartVM.OrderHeader.Id,
                    Source = stripeToken
                };

                var service = new ChargeService();
                Charge charge = service.Create(options);

                if (charge.BalanceTransactionId == null)
                {
                    cartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    cartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
                }

                if (charge.Status.ToLower() == "succeeded")
                {
                    cartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    cartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                    cartVM.OrderHeader.PaymentDate = DateTime.Now;
                }
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(OrderConfirmation), "Cart", new { id = cartVM.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

    }
}