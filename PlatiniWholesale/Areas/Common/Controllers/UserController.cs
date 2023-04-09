using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Platini.DB;
using Platini.Models;
using MvcPaging;
using System.Text;

namespace Platini.Areas.Common.Controllers
{
    public class UserController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);

        public ActionResult Index(int customerId, int? page, string searchString, string sortOrder, string sortColumn = "FirstName")
        {
            ViewBag.CustomerId = customerId;
            ViewBag.CompanyName = db.Accounts.Single(x => x.AccountId == customerId).UserName;
            if (sortOrder == null)
            {
                sortOrder = "asc";
                ViewBag.currentOrderParam = "asc";
                ViewBag.sortOrderParam = "desc";
            }
            ViewBag.currentOrderParam = sortOrder;
            ViewBag.sortOrderParam = (sortOrder == "desc") ? "asc" : "desc";

            ViewBag.sortColumnParam = sortColumn;
            ViewBag.searchStringParam = searchString;

            var userIds = db.CustomerUsers.Where(x => x.CustomerId == customerId).Select(y => y.AccountId).ToList();
            //var userList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.User && x.IsDeleted == false).Include(c => c.Communications).Include(x=>x.Customer_Company).ToList();

            var userList = (from r in db.Accounts
                            join r1 in userIds on r.AccountId equals r1
                            where r.RoleId == (int)RolesEnum.User
                            where r.IsDelete == false
                            select r).ToList();

            string nameCategory = searchString;
            if (!ReferenceEquals(nameCategory, null))
                userList = userList.Where(e => e.FirstName.ToLower().Contains(nameCategory.ToLower()) || e.LastName.ToLower().Contains(nameCategory.ToLower())).ToList();

            List<UserClass> list = new List<UserClass>();

            foreach (var item in userList)
            {
                UserClass user = new UserClass
                {
                    AccountId = item.AccountId,
                    Username = item.UserName,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    Email = item.Email,
                    PhoneNo = (item.Communications.Count() > 0) ? item.Communications.First().Phone : "",
                };
                list.Add(user);
            }

            if (sortColumn != "LastVisitedDate" && sortColumn != "Visits")
            {
                Type sortByPropType = typeof(UserClass).GetProperty(sortColumn).PropertyType;
                list = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(UserClass), sortByPropType })
                                            .Invoke(list, new object[] { list, sortColumn, sortOrder }) as List<UserClass>;
            }

            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(list.ToPagedList(currentPageIndex, defaultpageSize));
            
        }

        public ActionResult Create(int? customerId)
        {
            UserClass model = new UserClass();
            if (customerId != null)
            {
                model.BusinessName = (db.Companies.Where(x => x.AccountId == customerId.Value).Count() > 0) ? db.Companies.First(x => x.AccountId == customerId.Value).Name : null;
                //model.PhoneNo = (db.Communications.Where(x => x.AccountId == companyId.Value).Count() > 0) ? db.Communications.First(x => x.AccountId == companyId.Value).Phone : null;
                model.CustomerId = customerId.Value;
                var salesperson = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == customerId.Value && x.IsSalesPersonContact == 1);
                var salespersonid = salesperson != null ? salesperson.SalesPersonId : 0;
                var salesaccount = (db.Accounts.Where(x => x.AccountId == salespersonid && x.RoleId == (int)RolesEnum.SalesPerson).Count() > 0) ? db.Accounts.Single(x => x.AccountId == salespersonid && x.RoleId == (int)RolesEnum.SalesPerson) : null;
                if (salesaccount == null)
                {
                    model.SalesPerson = "Default";
                }
                else
                {
                    model.SalesPerson = salesaccount.FirstName + " " + salesaccount.LastName;
                }
                model.IsActive = true;
            }

            return View("CreateOrEdit",model);
        }

        public ActionResult Export(int? customerId)
        {
            StringBuilder sb = new StringBuilder();
            string sFileName = "User.xls";
            var userIds = db.CustomerUsers.Where(x => x.CustomerId == customerId).Select(y => y.AccountId).ToList();
            var userList = (from r in db.Accounts
                            join r1 in userIds on r.AccountId equals r1
                            where r.RoleId == (int)RolesEnum.User
                            where r.IsDelete == false
                            select r).ToList();
            
            if (userList != null && userList.Any())
            {
                sb.Append("<table style='1px solid black; font-size:14px;'>");
                sb.Append("<tr>");
                sb.Append("<td style='width:300px;font-size:16px'><b>CompanyName</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>FirstName</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>LastName</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Email</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Phone</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>BillingCity</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>BillingState</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>SalesMan</b></td>");
                sb.Append("</tr>");
                foreach (var result in userList)
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + (result.Companies.Count() > 0 ? result.Companies.FirstOrDefault().Name : string.Empty) + "</td>");
                    sb.Append("<td>" + result.FirstName + "</td>");
                    sb.Append("<td>" + result.LastName + "</td>");
                    sb.Append("<td>" + result.Email + "</td>");
                    sb.Append("<td>" + (result.Communications.Count() > 0 ? result.Communications.FirstOrDefault().Phone : string.Empty) + "</td>");
                    sb.Append("<td>" + (result.CustomerOptionalInfoes.Count() > 0 ? result.Addresses.FirstOrDefault().City : string.Empty) + "</td>");
                    sb.Append("<td>" + (result.CustomerOptionalInfoes.Count() > 0 ? result.Addresses.FirstOrDefault().State : string.Empty) + "</td>");

                    var salesPersonId = result.CustomerSalesPersons.Count() > 0 ? result.CustomerSalesPersons.FirstOrDefault().SalesPersonId : 0;
                    if (salesPersonId > 0)
                    {
                        var Account = db.Accounts.Find(salesPersonId).UserName;
                        sb.Append("<td>" + (string.Empty) + "</td>");
                    }
                    else
                    {
                        sb.Append("<td>" + (string.Empty) + "</td>");
                    }
                    sb.Append("</tr>");
                }
            }
            HttpContext.Response.AddHeader("content-disposition", "attachment; filename=" + sFileName);
            this.Response.ContentType = "application/vnd.ms-excel";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(buffer, "application/vnd.ms-excel");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserClass user)
        {
            if (db.Accounts.Select(x => x.UserName).Contains(user.Username))
            {
                ModelState.AddModelError("Username", "Record with this User Name already exist.");
                return View("CreateOrEdit", user);
            }
            ModelState.Remove("AccountId");
            if (ModelState.IsValid)
            {
                try
                {
                    var dbaccount = new Account();
                    dbaccount.InjectClass(user);
                    dbaccount.Password = Encoding.ASCII.GetBytes(user.Password);
                    dbaccount.RoleId = (int)RolesEnum.User;                    
                    dbaccount.IsActive = user.IsActive;
                    dbaccount.IsDelete = false;
                    db.Accounts.Add(dbaccount);
                    db.SaveChanges();
                    var id = db.Accounts.Single(x=>x.UserName == user.Username).AccountId;

                    var customerUser= new CustomerUser
                    {
                        AccountId = id,
                        CustomerId = user.CustomerId
                    };
                    db.CustomerUsers.Add(customerUser);
                    db.SaveChanges();

                    var communication = new Communication
                    {
                        Phone = user.PhoneNo,
                        AccountId = id
                    };
                    db.Communications.Add(communication);
                    db.SaveChanges();

                    return RedirectToAction("Index", new { customerId = user.CustomerId });
                }
                catch { }
            }
            return View("CreateOrEdit", user);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult checkUsernameAvailablity(string username)
        {
            if (String.IsNullOrEmpty(username))
            {
                return Json("Username is required.", JsonRequestBehavior.AllowGet);
            }
            else
            {
                if (db.Accounts.Select(x => x.UserName).Contains(username))
                {
                    return Json("Record with this User Name already exist.", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json("User Name is available.", JsonRequestBehavior.AllowGet);
                }
            }
        }

        public ActionResult Edit(int? customerId, int id)
        {
            var dbuser = db.Accounts.Find(id);
            if (dbuser != null)
            {
                UserClass model = new UserClass();
                model.InjectClass(dbuser);
                model.PhoneNo = (db.Communications.Where(x => x.AccountId == id).Count() > 0) ? db.Communications.First(x => x.AccountId == id).Phone : "";
                model.Password = Encoding.ASCII.GetString(dbuser.Password);
                if (customerId != null)
                {
                    model.BusinessName = db.Companies.Single(x => x.AccountId == customerId.Value).Name;
                    var CustomerSales = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == customerId.Value && x.IsSalesPersonContact == 1);
                    if (CustomerSales != null)
                    {
                        var salesaccount = db.Accounts.FirstOrDefault(x => x.AccountId == CustomerSales.SalesPersonId && x.RoleId == (int)RolesEnum.SalesPerson);
                        model.SalesPerson = salesaccount.FirstName + " " + salesaccount.LastName;
                    }
                    model.CustomerId = customerId.Value;
                }
                model.IsActive = model.IsActive.HasValue ? model.IsActive.Value : false;
                return View("CreateOrEdit",model);
            }

            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserClass user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dbaccount = db.Accounts.Find(user.AccountId);
                    dbaccount.InjectClass(user);
                    dbaccount.RoleId = (int)RolesEnum.User;
                    dbaccount.Password = Encoding.ASCII.GetBytes(user.Password);
                    dbaccount.IsActive = user.IsActive;
                    db.SaveChanges();

                    var dbcommunication = db.Communications.First(x => x.AccountId == user.AccountId);
                    dbcommunication.Phone = user.PhoneNo;
                    db.SaveChanges();

                    return RedirectToAction("Index", new { customerId = user.CustomerId });
                }
                catch { }
            }
            return View("CreateOrEdit", user);
        }

        public ActionResult Delete(int customerId, int Id = 0)
        {
            Account dbaccount = db.Accounts.Find(Id);
            if (dbaccount != null)
            {
                dbaccount.IsDelete = true;
                dbaccount.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index", new { @customerId = customerId });
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpGet]
        public void SendPassword(int id)
        {
            var account = db.Accounts.Single(x => x.AccountId == id);
            string password = Encoding.ASCII.GetString(account.Password);
            //var value = EmailManager.SendForgotPasswordEmail(account.Email, account.Username, password);
            //if (value == false) { throw new System.ArgumentException("Some Error Occured."); }
        }
    }
}
