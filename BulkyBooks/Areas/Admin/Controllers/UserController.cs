using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBooks.DataAccess.Data;
using BulkyBooks.Models;
using BulkyBooks.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var usersList = _db.ApplicationUsers.Include(i => i.Company).ToList();
            var userRole = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (var user in usersList)
            {
                var roleId = userRole.FirstOrDefault(i => i.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(i => i.Id == roleId).Name;
                if (user.Company == null)
                {
                    user.Company = new Company
                    {
                        Name = ""
                    };
                }
            }

            return Json(new { data = usersList });
        }

        [HttpPost]
        public IActionResult LockOrUnlock([FromBody] string id)
        {
            var userFromDb = _db.ApplicationUsers.FirstOrDefault(i => i.Id == id);
            if (userFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking!" });
            }

            if (userFromDb.LockoutEnd != null && userFromDb.LockoutEnd > DateTime.Now)
            {
                // User is currently locked, we will unlock user.
                userFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                userFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }

            _db.SaveChanges();
            return Json(new { success = true, message = "Locking/Unlocking is successful!" });
        }

        #endregion

    }
}