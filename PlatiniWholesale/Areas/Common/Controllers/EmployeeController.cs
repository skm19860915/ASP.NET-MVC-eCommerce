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
    public class EmployeeController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);

        public ActionResult Index(int? page, string searchString, string selectedRole, string sortOrder, string sortColumn = "FirstName")
        {
            if (sortOrder == null)
            {
                sortOrder = "asc";
                ViewBag.currentOrderParam = "asc";
                ViewBag.sortOrderParam = "desc";
            }
            ViewBag.currentOrderParam = sortOrder;
            ViewBag.sortOrderParam = (sortOrder == "desc") ? "asc" : "desc";

            ViewBag.sortColumnParam = sortColumn;        
            var employees = db.Accounts.Where(i => i.RoleId != (int)RolesEnum.Customer && i.RoleId != (int)RolesEnum.User && i.IsDelete == false).Include(x => x.Communications).ToList();
            string nameCategory = searchString;
            if (!ReferenceEquals(nameCategory, null))
                employees = employees.Where(e => e.FirstName.ToLower().Contains(nameCategory.ToLower()) || e.LastName.ToLower().Contains(nameCategory.ToLower()) || e.UserName.ToLower().Contains(nameCategory.ToLower())).ToList();
            ViewBag.searchStringParam = nameCategory;

            int selRole = 0;
            int.TryParse(selectedRole, out selRole);
            if (selRole > 0)
                employees = employees.Where(i => i.RoleId == selRole).ToList();
            ViewBag.selectedRoleParam = selRole.ToString();

            List<EmployeeClass> retList = new List<EmployeeClass>();
            foreach (var employee in employees)
            {
                var number = "";
                if(employee.Communications.Count() != 0)
                {
                    number = employee.Communications.First().Phone;
                }
                var item = new EmployeeClass
                {
                    EmployeeId = employee.AccountId,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    IsActive = employee.IsActive.Value,
                    RoleId = employee.RoleId,
                    Email = employee.Email,
                    Username = employee.UserName,
                    PhoneNo = number,
                    Role = ((RolesEnum)employee.RoleId).ToString()
                };
                retList.Add(item);             
            }

            Type sortByPropType = typeof(EmployeeClass).GetProperty(sortColumn).PropertyType;
            List<EmployeeClass> sortedList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(EmployeeClass), sortByPropType })
                                           .Invoke(retList, new object[] { retList, sortColumn, sortOrder }) as List<EmployeeClass>;

            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(sortedList.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult Create()
        {
            EmployeeClass employee = new EmployeeClass();
            employee.IsActive = true;
            return View("CreateOrEdit", employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeClass employee)
        {
            ModelState.Remove("EmployeeId");
            if (ModelState.IsValid)
            {
                var chkExist = db.Accounts.Where(x => x.UserName == employee.Username && x.AccountId != employee.EmployeeId && x.IsDelete == false && (employee.RoleId != (int)RolesEnum.Customer || employee.RoleId != (int)RolesEnum.User)).Any();
                if (!chkExist)
                {
                    Account dbAccount = new Account();
                    dbAccount.InjectClass(employee);
                    dbAccount.IsDelete = false;
                    dbAccount.DateCreated = DateTime.UtcNow;
                    dbAccount.DateUpdated = DateTime.UtcNow;
                    dbAccount.Password = Encoding.ASCII.GetBytes(employee.Password);
                    db.Accounts.Add(dbAccount);
                    db.SaveChanges();
                    Communication communication = new Communication { AccountId = dbAccount.AccountId, Phone = employee.PhoneNo };
                    db.Communications.Add(communication);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.PageMessage = "An employee with this name already exists.";
                }
            }
            return View("CreateOrEdit", employee);
        }

        public ActionResult Edit(int Id = 0)
        {
            Account dbAccount = db.Accounts.Where(x => x.AccountId == Id).Include(y => y.Communications).FirstOrDefault(); 
            if (dbAccount != null)
            {                
                var number = "";
                if (dbAccount.Communications != null && dbAccount.Communications.Count>0)
                {
                    number = dbAccount.Communications.FirstOrDefault().Phone;
                }
                EmployeeClass employee = new EmployeeClass
                {
                    EmployeeId = dbAccount.AccountId,
                    Username = dbAccount.UserName,
                    FirstName = dbAccount.FirstName,
                    LastName = dbAccount.LastName,
                    Email = dbAccount.Email,
                    Password = Encoding.ASCII.GetString(dbAccount.Password),
                    RoleId = dbAccount.RoleId,                   
                    PhoneNo = number,
                    IsActive = dbAccount.IsActive.Value
                };                
                return View("CreateOrEdit", employee);
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeClass employee)
        {
            if (ModelState.IsValid)
            {
                Account dbAccount = db.Accounts.Find(employee.EmployeeId);
                if (dbAccount != null)
                {
                    var chkExist = db.Accounts.Where(x => x.UserName == employee.Username && x.AccountId == employee.EmployeeId && x.IsDelete == false).Any();                 
                    if (chkExist)
                    {
                        dbAccount.InjectClass(employee);
                        dbAccount.IsDelete = false;
                        dbAccount.DateCreated = DateTime.UtcNow;
                        dbAccount.DateUpdated = DateTime.UtcNow;
                        dbAccount.Password = Encoding.ASCII.GetBytes(employee.Password);
                        db.SaveChanges();
                        Communication communication = db.Communications.Where(x => x.AccountId == employee.EmployeeId).FirstOrDefault();
                        if (communication != null)
                        {
                            communication.Phone = employee.PhoneNo;                           
                        }
                        else
                        {
                            communication = new Communication { AccountId = employee.EmployeeId, Phone = employee.PhoneNo };
                            db.Communications.Add(communication);
                        }
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "An employee with this name already exists.";
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Record not found";
                }
            }
            return View("CreateOrEdit", employee);
        }

        public ActionResult Delete(int Id = 0)
        {
            Account dbAccount = db.Accounts.Find(Id);
            if (dbAccount != null)
            {
                dbAccount.IsDelete = true;
                dbAccount.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
                return RedirectToAction("Index");
            }
            ViewBag.PageMessage = "Record not found";
            return View();
        }
    }
}
