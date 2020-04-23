using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BulkyBooks.DataAccess.Repository.IRepository;
using BulkyBooks.Models;
using BulkyBooks.Models.ViewModels;
using BulkyBooks.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace BulkyBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderDetailsVM detailsVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            detailsVM = new OrderDetailsVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(i => i.Id == id, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetails.GetAll(i => i.OrderId == id, includeProperties: "Product")
            };

            return View(detailsVM);
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(i => i.Id == id);
            orderHeader.OrderStatus = SD.StatusInProcess;
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(i => i.Id == detailsVM.OrderHeader.Id);
            orderHeader.TrackingNumber = detailsVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = detailsVM.OrderHeader.Carrier;
            orderHeader.ShippingDate = DateTime.Now;
            orderHeader.OrderStatus = SD.StatusShipped;
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(i => i.Id == id);
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal / 100),
                    Reason = RefundReasons.RequestedByCustomer,
                    Charge = orderHeader.TransactionId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                orderHeader.OrderStatus = SD.StatusRefunded;
                orderHeader.PaymentStatus = SD.StatusRefunded;
            }
            else
            {
                orderHeader.OrderStatus = SD.StatusCancelled;
                orderHeader.PaymentStatus = SD.StatusCancelled;
            }

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Details")]
        public IActionResult Details(string stripeToken)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(i => i.Id == detailsVM.OrderHeader.Id, includeProperties: "ApplicationUser");

            if (!string.IsNullOrEmpty(stripeToken))
            {
                // Process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
                    Currency = "usd",
                    Description = "Order Id: " + orderHeader.Id,
                    Source = stripeToken
                };

                var service = new ChargeService();
                Charge charge = service.Create(options);

                if (charge.Id == null)
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    orderHeader.TransactionId = charge.Id;
                }

                if (charge.Status.ToLower() == "succeeded")
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    orderHeader.PaymentDate = DateTime.Now;
                }

                _unitOfWork.Save();
            }
                        
            return RedirectToAction("Details", "Order", new { id = orderHeader.Id });
        }


        #region API CALLS
        [HttpGet]
        public IActionResult GetOrdersList(string status)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeaderList;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                orderHeaderList = _unitOfWork.OrderHeader.GetAll(i => i.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaderList = orderHeaderList.Where(i => i.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaderList = orderHeaderList.Where(i => i.OrderStatus == SD.StatusApproved ||
                                                                i.OrderStatus == SD.StatusInProcess ||
                                                                i.OrderStatus == SD.StatusPending);
                    break;
                case "completed":
                    orderHeaderList = orderHeaderList.Where(i => i.OrderStatus == SD.StatusShipped);
                    break;
                case "rejected":
                    orderHeaderList = orderHeaderList.Where(i => i.OrderStatus == SD.StatusCancelled ||
                                                                i.OrderStatus == SD.StatusRefunded ||
                                                                i.PaymentStatus == SD.PaymentStatusRejected);
                    break;
                default:
                    break;
            };

            return Json(new { data = orderHeaderList });
        }

        

        #endregion

    }
}