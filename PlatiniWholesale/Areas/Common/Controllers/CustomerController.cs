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
using Intuit.Ipp.Security;
using Intuit.Ipp.Core;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Data;
using Intuit.Ipp.QueryFilter;
using System.IO;

namespace Platini.Areas.Common.Controllers
{
    public class CustomerController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = 50;
        private bool activeInactive = true;

        public ActionResult Index2(int? page, int? selectedSalesPersonId, string searchString, string sortOrder, string sortColumn = "BusinessName")
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
            ViewBag.searchStringParam = searchString;
            List<CustomerClass> list = new List<CustomerClass>();

            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
            {
                int userId = Convert.ToInt32(SiteIdentity.UserId);
                //var customerList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer && x.IsDelete == false).Include(c => c.Communications)
                //    .Include(c => c.Companies).Include(c => c.Addresses).Include(c => c.CustomerSalesPersons).Include(c => c.CustomerOptionalInfoes).ToList();
                var customerList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer && x.IsDelete == false)
                    .Include(c => c.Companies).Include(c => c.CustomerSalesPersons).Include(u => u.CustomerUsers).ToList();

                string nameCategory = searchString;
                if (!string.IsNullOrEmpty(nameCategory))
                    customerList = customerList.Where(e => e.Companies.FirstOrDefault(x => x.Name.ToLower().Contains(nameCategory.ToLower()) && x.IsActive == true && x.IsDelete == false) != null).ToList();


                foreach (var item in customerList)
                {
                    CustomerClass customer = new CustomerClass
                    {
                        AccountId = item.AccountId,
                        BusinessName = (item.Companies.Count(x => x.IsActive == true && x.IsDelete == false) > 0) ? item.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Name : "",
                        Username = item.UserName,
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Email = item.Email,
                        //PhoneNo = (item.Communications.Count(x=>x.IsActive == true && x.IsDelete == false) > 0) ? item.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Phone : "",
                        //BillingAddress = (item.Addresses.Count(x => x.IsActive == true && x.IsDelete == false) > 0) ? item.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false) : new Address(),
                        IsActive = item.IsActive,
                        SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson && x.IsActive == true && x.IsDelete == false).Select(y => new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName }).ToList(),
                        SelectedSalesPerson = (item.CustomerSalesPersons.Count() > 0) ? item.CustomerSalesPersons.FirstOrDefault().SalesPersonId : 0,
                        //UserCount = db.CustomerUsers.Include(x => x.Account).Where(x => x.CustomerId == item.AccountId && x.Account.IsDelete == false && x.Account.RoleId == (int)RolesEnum.User).Count()
                        UserCount = item.CustomerUsers.Count(x => x.CustomerId == item.AccountId && x.Account.IsDelete == false && x.Account.RoleId == (int)RolesEnum.User)
                    };
                    list.Add(customer);
                }

                list = list.Where(x => x.SelectedSalesPerson == userId).ToList();


                if (sortColumn != "City" && sortColumn != "State" && sortColumn != "LastVisitedDate" && sortColumn != "Visits")
                {
                    Type sortByPropType = typeof(CustomerClass).GetProperty(sortColumn).PropertyType;
                    list = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(CustomerClass), sortByPropType })
                                                .Invoke(list, new object[] { list, sortColumn, sortOrder }) as List<CustomerClass>;
                }

                if (sortColumn == "City" || sortColumn == "State")
                {
                    switch (sortColumn)
                    {
                        case "City":
                            list = list.OrderBy(x => x.BillingAddress.City).ToList();
                            break;
                        case "State":
                            list = list.OrderBy(x => x.BillingAddress.State).ToList();
                            break;
                    }
                }

            }
            else
            {
                //var customerList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer && x.IsDelete == false).Include(c => c.Communications)
                //    .Include(c => c.Companies).Include(c => c.Addresses).Include(c => c.CustomerSalesPersons).Include(c => c.CustomerOptionalInfoes).ToList();
                var customerList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer && x.IsDelete == false)
                    .Include(c => c.Companies).Include(c => c.CustomerSalesPersons).Include(u => u.CustomerUsers).ToList();

                string nameCategory = searchString;
                if (!string.IsNullOrEmpty(nameCategory))
                    customerList = customerList.Where(e => e.Companies.FirstOrDefault(x => x.Name.ToLower().Contains(nameCategory.ToLower())) != null).ToList();
                //List<CustomerClass> list = new List<CustomerClass>();

                foreach (var item in customerList)
                {
                    CustomerClass customer = new CustomerClass
                    {
                        AccountId = item.AccountId,
                        BusinessName = (item.Companies.Count(x => x.IsActive == true && x.IsDelete == false) > 0) ? item.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Name : "",
                        Username = item.UserName,
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Email = item.Email,
                        //PhoneNo = (item.Communications.Count(x => x.IsActive == true && x.IsDelete == false) > 0) ? item.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Phone : "",
                        //BillingAddress = (item.Addresses.Count(x => x.IsActive == true && x.IsDelete == false) > 0) ? item.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false) : new Address(),
                        IsActive = item.IsActive,
                        SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson && x.IsActive == true && x.IsDelete == false).Select(y => new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName }).ToList(),
                        SelectedSalesPerson = (item.CustomerSalesPersons.Count() > 0) ? item.CustomerSalesPersons.FirstOrDefault().SalesPersonId : 0,
                        //UserCount = db.CustomerUsers.Include(x => x.Account).Where(x => x.CustomerId == item.AccountId && x.Account.IsDelete == false && x.Account.RoleId == (int)RolesEnum.User).Count()
                        UserCount = item.CustomerUsers.Count(x => x.CustomerId == item.AccountId && x.Account.IsDelete == false && x.Account.RoleId == (int)RolesEnum.User)
                    };
                    list.Add(customer);
                }

                if (selectedSalesPersonId != null && selectedSalesPersonId != -1)
                {
                    list = list.Where(x => x.SelectedSalesPerson == selectedSalesPersonId).ToList();
                }

                if (sortColumn != "City" && sortColumn != "State" && sortColumn != "LastVisitedDate" && sortColumn != "Visits")
                {
                    Type sortByPropType = typeof(CustomerClass).GetProperty(sortColumn).PropertyType;
                    list = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(CustomerClass), sortByPropType })
                                                .Invoke(list, new object[] { list, sortColumn, sortOrder }) as List<CustomerClass>;
                }

                if (sortColumn == "City" || sortColumn == "State")
                {
                    switch (sortColumn)
                    {
                        case "City":
                            list = list.OrderBy(x => x.BillingAddress.City).ToList();
                            break;
                        case "State":
                            list = list.OrderBy(x => x.BillingAddress.State).ToList();
                            break;
                    }
                }

            }
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(list.ToPagedList(currentPageIndex, defaultpageSize));

        }

        public ActionResult Index(string Id, int? fType, int? page, int? selectedSalesPersonId, string searchString, string sortOrder, string sortColumn = "BusinessName")
        {
            if (sortOrder == null)
            {
                sortOrder = "asc";
                ViewBag.currentOrderParam = "asc";
                ViewBag.sortOrderParam = "desc";
            }
            ViewBag.currentOrderParam = sortOrder;
            ViewBag.sortOrderParam = (sortOrder == "desc") ? "asc" : "desc";
            ViewBag.userType = "1";
            if (!string.IsNullOrEmpty(Id))
                ViewBag.userType = Id;
            ViewBag.sortColumnParam = sortColumn;
            ViewBag.searchStringParam = searchString;
            ViewBag.fType = 0;
            ViewBag.SalesPersons = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson && x.IsActive == true && x.IsDelete == false).Select(y => new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName }).ToList();
            int userId = 0;
            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
                int.TryParse(SiteIdentity.UserId, out userId);
            else if (selectedSalesPersonId != null && selectedSalesPersonId != -1)
                userId = selectedSalesPersonId.Value;
            string SQL = @"select a.AccountId,FirstName,LastName,Email,IsActive,IsDelete,Username,LastLoginDate,
                           ISNULL(i.DisplayName,ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),a.FirstName+' '+a.LastName))as DisplayName,
                           ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),'')as BusinessName,
                           ISNULL((select COUNT(c.CustomerUserId) from CustomerUser c where  c.AccountId=a.AccountId),0) as UserCount,
                           ISNULL((select top 1 c.SalesPersonId from CustomerSalesPerson c where  c.AccountId=a.AccountId),0) as SelectedSalesPerson,
                           ISNULL((select top 1 p.Phone from Communication p where p.AccountId=a.AccountId and p.IsActive=1 and p.IsDelete=0),'') as PhoneNo,
                           ISNULL((select top 1 d.City from Address d where d.AccountId=a.AccountId and d.IsActive=1 and d.IsDelete=0),'') as City,
                           ISNULL((select top 1 d.State from Address d where d.AccountId=a.AccountId and d.IsActive=1 and d.IsDelete=0),'') as State
						from Account a 
                        left outer join CustomerOptionalInfo i on a.AccountId=i.AccountId 
                        where RoleId=5 and IsDelete=0 and ISNULL(i.CustomerType,2)=" + ViewBag.userType;
            var list = db.Database.SqlQuery<CustomerClass>(SQL);
            if (userId > 0)
                list = list.Where(x => x.SelectedSalesPerson == userId);
            string nameCategory = searchString;
            if (!string.IsNullOrEmpty(nameCategory))
            {
                nameCategory = nameCategory.ToLower();
                if (!fType.HasValue)
                    list = list.Where(e => e.BusinessName.ToLower().Contains(nameCategory) || e.Email.ToLower().Contains(nameCategory) || e.PhoneNo.ToLower().Contains(nameCategory) || e.FirstName.ToLower().Contains(nameCategory)
                        || e.LastName.ToLower().Contains(nameCategory) || e.City.ToLower().Contains(nameCategory) || e.State.ToLower().Contains(nameCategory));
                else
                {
                    if (fType == 1)
                        list = list.Where(e => e.BusinessName.ToLower().Contains(nameCategory));
                    else if (fType == 2)
                        list = list.Where(e => e.City.ToLower().Contains(nameCategory));
                    else if (fType == 3)
                        list = list.Where(e => e.State.ToLower().Contains(nameCategory));
                    else if (fType == 4)
                        list = list.Where(e => e.FirstName.ToLower().Contains(nameCategory) || e.LastName.ToLower().Contains(nameCategory));
                    else if (fType == 5)
                        list = list.Where(e => e.Email.ToLower().Contains(nameCategory));
                    else if (fType == 6)
                        list = list.Where(e => e.PhoneNo.ToLower().Contains(nameCategory));
                    ViewBag.fType = fType.Value;
                }
            }
            Type sortByPropType = typeof(CustomerClass).GetProperty(sortColumn).PropertyType;
            list = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(CustomerClass), sortByPropType })
                                        .Invoke(list, new object[] { list, sortColumn, sortOrder }) as List<CustomerClass>;
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(list.ToPagedList(currentPageIndex, defaultpageSize));

        }

        public ActionResult Create()
        {
            CustomerClass customer = new CustomerClass();
            customer.IsActive = true;
            customer.SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson).Select(y =>
                new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName, IsSelected = false }).ToList();
            customer.TermList = db.Terms.Where(x => x.IsActive == true && x.IsDelete == false)
                .Select(y => new SelectedListValues { Id = y.TermId, Value = y.Name, IsSelected = false }).ToList();
            customer.BillingAddress = new Address { AddressTypeId = (int)AddressTypeEnum.BillingAddress };
            customer.ShippingAddress = new Address { AddressTypeId = (int)AddressTypeEnum.ShippingAddress };
            customer.Wholesale = true;
            ViewBag.SameAsBillingValue = "true";

            return View("CreateOrEdit", customer);
        }

        [HttpPost]
        public ActionResult Create(CustomerClass model, HttpPostedFileBase bFileUp)
        {
            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
            {
                model.SelectedSalesPerson = Convert.ToInt32(SiteIdentity.UserId);
            }

            if (db.Accounts.Select(x => x.UserName).Contains(model.Username))
            {
                ModelState.AddModelError("Username", "Record with this User Name already exist.");
                return View("CreateOrEdit", model);
            }
            //ViewBag.Msg = CheckUserNameAvailability(model.Username);
            model.SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson).Select(y =>
                new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName, IsSelected = false }).ToList();
            model.TermList = db.Terms.Where(x => x.IsActive == true && x.IsDelete == false)
                .Select(y => new SelectedListValues { Id = y.TermId, Value = y.Name, IsSelected = false }).ToList();
            
            ModelState.Remove("AccountId");
            ModelState.Remove("SelectedSalesPerson");
            ModelState.Remove("Email");            
            model.PhoneNo = model.CountryCode;
            if (ModelState.IsValid)
            {
                try
                {
                    var dbAccount = new Platini.DB.Account();
                    dbAccount.InjectClass(model);
                    dbAccount.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : string.Empty;
                    dbAccount.IsActive = model.IsActive;
                    dbAccount.IsDelete = false;
                    dbAccount.RoleId = (int)RolesEnum.Customer;
                    dbAccount.DateCreated = DateTime.UtcNow;
                    dbAccount.DateUpdated = DateTime.UtcNow;
                    dbAccount.Password = Encoding.ASCII.GetBytes(model.Password);
                    db.Accounts.Add(dbAccount);
                    db.SaveChanges();
                    var id = db.Accounts.First(x => x.UserName == model.Username).AccountId;

                    if (model.SelectedSalesPerson > 0)
                    {
                        var person = new CustomerSalesPerson();
                        person.AccountId = dbAccount.AccountId;
                        person.SalesPersonId = model.SelectedSalesPerson;
                        person.IsSalesPersonContact = 1;
                        person.DateCreated = person.DateUpdated = DateTime.UtcNow;
                        db.CustomerSalesPersons.Add(person);
                    }

                    db.SaveChanges();
                    StringBuilder sb = new StringBuilder();
                    foreach (char c in model.PhoneNo)
                    {
                        if ((c >= '0' && c <= '9'))
                            sb.Append(c);
                    }
                    model.PhoneNo = sb.ToString();
                    if (model.CountryCode.Length > model.PhoneNo.Length)
                    {
                        int len = model.CountryCode.Length - model.PhoneNo.Length;
                        model.CountryCode = model.CountryCode.Insert(len + 1, "-");
                        model.PhoneNo = model.CountryCode;
                    }
                    var dbCommunication = new Communication
                    {    
                        Phone = model.PhoneNo,
                        //CountryCode= model.CountryCode,
                        AccountId = id,
                        IsActive = true,
                        IsDelete = false,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    };

                    db.Communications.Add(dbCommunication);
                    db.SaveChanges();
                    string cName = string.Empty;
                    if (model.BusinessName != null)
                    {
                        cName = SaveUniqueCompany(model.BusinessName, model.BillingAddress.City, id);
                    }
                    else if (model.BusinessName == null)
                    {
                        cName = SaveUniqueCompany(model.FirstName + " " + model.LastName, model.BillingAddress.City, id);
                    }

                    var dbcustomerOptionalInfo = new CustomerOptionalInfo
                    {
                        BusinessReseller = model.BusinessReseller,
                        AccountId = id,
                        TermId = model.SelectedTerm,
                        CustomerType = model.Wholesale ? (int)Platini.Models.CustomerType.Wholesale : (int)Platini.Models.CustomerType.Retail,
                        DisplayName = !string.IsNullOrEmpty(model.DisplayName) ? model.DisplayName : (!string.IsNullOrEmpty(cName) ? cName : model.FirstName + " " + model.LastName),
                        Note = model.Note,
                        Discount = model.Discount,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    };
                    if (model.Wholesale)
                    {
                        if (bFileUp != null && bFileUp.ContentLength != 0 && bFileUp.InputStream != null)
                        {
                            string fileName;
                            System.Drawing.Imaging.ImageFormat format;
                            if (VerifyImage(bFileUp.ContentType.ToLower(), out fileName, out format))
                            {
                                dbcustomerOptionalInfo.BusinessLicense = fileName;
                                System.Drawing.Image.FromStream(bFileUp.InputStream).Save(Server.MapPath("~/Library/Uploads/" + fileName), format);
                            }
                            else
                                TempData["PageMessage"] = "File does not appear to be a valid image type.";
                        }
                        dbcustomerOptionalInfo.DateUpdated = DateTime.UtcNow;
                    }
                    db.CustomerOptionalInfoes.Add(dbcustomerOptionalInfo);
                    db.SaveChanges();

                    if (model.BillingAddress.City != null)
                    {
                        var dbAddress = new Address();
                        dbAddress.InjectClass(model.BillingAddress);
                        dbAddress.AccountId = id;
                        dbAddress.IsActive = true;
                        dbAddress.IsDelete = false;
                        dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
                        db.Addresses.Add(dbAddress);
                        db.SaveChanges();
                    }

                    if (model.ShippingAddress.City != null && model.SameAsBillingCheckBox == false)
                    {
                        var dbAddress = new Address();
                        dbAddress.InjectClass(model.ShippingAddress);
                        dbAddress.AccountId = id;
                        dbAddress.IsActive = true;
                        dbAddress.IsDelete = false;
                        dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
                        db.Addresses.Add(dbAddress);
                        db.SaveChanges();
                    }

                    if (model.SameAsBillingCheckBox == true)
                    {
                        var dbAddress = new Address();
                        dbAddress.InjectClass(model.BillingAddress);
                        dbAddress.AccountId = id;
                        dbAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
                        dbAddress.IsActive = true;
                        dbAddress.IsDelete = false;
                        dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
                        db.Addresses.Add(dbAddress);
                        db.SaveChanges();
                    }
                    if (dbAccount.IsActive == true && QuickBookStrings.UseQuickBook())
                        AddCustomerToQuickBook(dbAccount.AccountId, string.Empty);
                    return RedirectToAction("Index");
                }
                catch 
                {                    
                }
            }
            return View("CreateOrEdit", model);
        }

        public ActionResult Export(string Id, int? fType, int? selectedSalesPersonId, string searchString, string sortOrder, string sortColumn = "BusinessName")
        {
            StringBuilder sb = new StringBuilder();
            string sFileName = "Customers.xls";
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
            ViewBag.userType = "1";
            if (!string.IsNullOrEmpty(Id))
                ViewBag.userType = Id;
            int userId = 0;
            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
                int.TryParse(SiteIdentity.UserId, out userId);
            else if (selectedSalesPersonId != null && selectedSalesPersonId != -1)
                userId = selectedSalesPersonId.Value;
            string SQL = @"select a.AccountId,FirstName,LastName,Email,IsActive,IsDelete,Username,LastLoginDate,
                        ISNULL(i.DisplayName,ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),a.FirstName+' '+a.LastName))as DisplayName,
                        ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),'')as BusinessName,
                        ISNULL((select COUNT(c.CustomerUserId) from CustomerUser c where  c.AccountId=a.AccountId),0) as UserCount,
                        ISNULL((select top 1 c.SalesPersonId from CustomerSalesPerson c where  c.AccountId=a.AccountId),0) as SelectedSalesPerson,
                        ISNULL((select top 1 p.Phone from Communication p where p.AccountId=a.AccountId and p.IsActive=1 and p.IsDelete=0),'') as PhoneNo,
                        ISNULL((select top 1 d.City from Address d where d.AccountId=a.AccountId and d.IsActive=1 and d.IsDelete=0),'') as City,
                        ISNULL((select top 1 d1.State from Address d1 where d1.AccountId=a.AccountId and d1.IsActive=1 and d1.IsDelete=0),'') as State
                        from Account a 
						left outer join CustomerOptionalInfo i on a.AccountId=i.AccountId 
						where RoleId=5 and IsDelete=0 and ISNULL(i.CustomerType,2)=" + ViewBag.userType;
            var list = db.Database.SqlQuery<CustomerClass>(SQL);
            if (userId > 0)
                list = list.Where(x => x.SelectedSalesPerson == userId);
            string nameCategory = searchString;
            if (!string.IsNullOrEmpty(nameCategory))
            {
                nameCategory = nameCategory.ToLower();
                if (!fType.HasValue)
                    list = list.Where(e => e.BusinessName.ToLower().Contains(nameCategory) || e.Email.ToLower().Contains(nameCategory) || e.PhoneNo.ToLower().Contains(nameCategory) || e.FirstName.ToLower().Contains(nameCategory)
                        || e.LastName.ToLower().Contains(nameCategory) || e.City.ToLower().Contains(nameCategory) || e.State.ToLower().Contains(nameCategory));
                else
                {
                    if (fType == 1)
                        list = list.Where(e => e.BusinessName.ToLower().Contains(nameCategory));
                    else if (fType == 2)
                        list = list.Where(e => e.City.ToLower().Contains(nameCategory));
                    else if (fType == 3)
                        list = list.Where(e => e.State.ToLower().Contains(nameCategory));
                    else if (fType == 4)
                        list = list.Where(e => e.FirstName.ToLower().Contains(nameCategory) || e.LastName.ToLower().Contains(nameCategory));
                    else if (fType == 5)
                        list = list.Where(e => e.Email.ToLower().Contains(nameCategory));
                    else if (fType == 6)
                        list = list.Where(e => e.PhoneNo.ToLower().Contains(nameCategory));

                }
            }
            Type sortByPropType = typeof(CustomerClass).GetProperty(sortColumn).PropertyType;
            list = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(CustomerClass), sortByPropType })
                                        .Invoke(list, new object[] { list, sortColumn, sortOrder }) as List<CustomerClass>;
            if (list != null && list.Any())
            {
                sb.Append("<table style='1px solid black; font-size:14px;'>");
                sb.Append("<tr>");
                //sb.Append("<td style='width:300px;font-size:16px'><b>Display Name</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Business Name</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>First Name</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Last Name</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Email</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Phone</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Billing City</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Billing State</b></td>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Sales Man</b></td>");
                sb.Append("</tr>");
                foreach (var result in list)
                {
                    sb.Append("<tr>");
                    //sb.Append("<td>" + result.DisplayName + "</td>");
                    sb.Append("<td>" + result.BusinessName + "</td>");
                    sb.Append("<td>" + result.FirstName + "</td>");
                    sb.Append("<td>" + result.LastName + "</td>");
                    sb.Append("<td>" + result.Email + "</td>");
                    sb.Append("<td>" + result.PhoneNo + "</td>");
                    sb.Append("<td>" + result.City + "</td>");
                    sb.Append("<td>" + result.State + "</td>"); ;
                    if (result.SelectedSalesPerson > 0)
                    {
                        var Account = db.Accounts.Find(result.SelectedSalesPerson).UserName;
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

        //public string CheckUserNameAvailability(string username)
        //{
        //    var isAvailable = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer).Select(y => y.Username).Contains(username);
        //    string msg = "";
        //    if (isAvailable == true)
        //    {
        //        msg = "Record with this User Name already exist.";
        //    }
        //    else if (isAvailable == false)
        //    {
        //        msg = "User Name is available.";
        //    }
        //    return msg;            
        //}

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

        public ActionResult Edit(string fromInactiveCustomerPage, int id = 0)
        {
            if (db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer).Select(x => x.AccountId).Contains(id))
            {
                var account = db.Accounts.Single(x => x.AccountId == id);
                var customerOptionalInfo = (db.CustomerOptionalInfoes.Where(x => x.AccountId == id).Count() > 0) ? db.CustomerOptionalInfoes.Single(x => x.AccountId == id) : new CustomerOptionalInfo();
                var company = (db.Companies.Where(x => x.AccountId == id).Count() > 0) ? db.Companies.FirstOrDefault(x => x.AccountId == id) : new Platini.DB.Company();
                var communication = (db.Communications.Where(x => x.AccountId == id && x.IsActive == true && x.IsDelete == false).Count() > 0) ? db.Communications.FirstOrDefault(x => x.AccountId == id && x.IsActive == true && x.IsDelete == false) : new Communication();
                var addresses = db.Addresses.Where(x => x.AccountId == id).ToList();

                var model = new CustomerClass();
                model.SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson).Select(y =>
                new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName, IsSelected = false }).ToList();
                model.TermList = db.Terms.Where(x => x.IsActive == true && x.IsDelete == false)
                    .Select(y => new SelectedListValues { Id = y.TermId, Value = y.Name, IsSelected = false }).ToList();
                model.SelectedSalesPerson = db.CustomerSalesPersons.Select(x => x.AccountId).Contains(id) ? db.CustomerSalesPersons.Single(x => x.AccountId == id && x.IsSalesPersonContact == 1).SalesPersonId : 0;
                model.BusinessName = (company != null) ? company.Name : "";
                model.PhoneNo = (communication.Phone != null) ? communication.Phone : "";
                //model.PhoneNo = (communication.Phone != null) ? (communication.CountryCode != null ? communication.CountryCode : "") : "";
                model.BillingAddress = addresses.Select(x => x.AddressTypeId).Contains((int)AddressTypeEnum.BillingAddress) ? addresses.First(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress) : new Address();
                model.ShippingAddress = addresses.Select(x => x.AddressTypeId).Contains((int)AddressTypeEnum.ShippingAddress) ? addresses.First(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress) : new Address();
                model.InjectClass(customerOptionalInfo);
                model.SelectedTerm = (customerOptionalInfo != null && customerOptionalInfo.TermId.HasValue) ? customerOptionalInfo.TermId.Value : 0;
                model.InjectClass(account);
                model.Password = Encoding.ASCII.GetString(account.Password);
                model.BusinessReseller = customerOptionalInfo.BusinessReseller;
                if (customerOptionalInfo != null && customerOptionalInfo.CustomerType == (int)Platini.Models.CustomerType.Wholesale)
                {
                    model.Wholesale = true;
                }
                else
                {
                    model.Wholesale = false;
                }
                model.DisplayName = customerOptionalInfo.AccountId > 0 ? (!string.IsNullOrEmpty(customerOptionalInfo.DisplayName) ? customerOptionalInfo.DisplayName : (company != null ? company.Name : account.FirstName + " " + account.LastName)) : (company != null ? company.Name : account.FirstName + " " + account.LastName);
                model.Note = customerOptionalInfo.Note;
                model.Discount = customerOptionalInfo.Discount.HasValue ? customerOptionalInfo.Discount.Value : 0;
                model.FromInActiveCustomerPage = (fromInactiveCustomerPage == null) ? "false" : fromInactiveCustomerPage;
                model.IsActive = model.IsActive.HasValue ? model.IsActive.Value : false;
                ViewBag.SameAsBillingValue = ShowShippingDiv(model.BillingAddress, model.ShippingAddress);
                ViewBag.fromInactive = model.FromInActiveCustomerPage;

                return View("CreateOrEdit", model);
            }
            TempData["PageMessage"] = "Record not found";
            return RedirectToAction("Index", "Customer");
            //return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CustomerClass model, HttpPostedFileBase bFileUp)
        {
            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
            {
                model.SelectedSalesPerson = Convert.ToInt32(SiteIdentity.UserId);
            }
            model.SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson).Select(y =>
                new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName, IsSelected = false }).ToList();
            model.TermList = db.Terms.Where(x => x.IsActive == true && x.IsDelete == false)
                .Select(y => new SelectedListValues { Id = y.TermId, Value = y.Name, IsSelected = false }).ToList();
            string CompanyName = string.Empty;
            ModelState.Remove("Id");
            ModelState.Remove("SelectedSalesPerson");
            ModelState.Remove("Email");                
            if (ModelState.IsValid)
            {                
                try
                {                    
                    var dbAccount = db.Accounts.Find(model.AccountId);                    
                    dbAccount.InjectClass(model);
                    dbAccount.Email = !string.IsNullOrEmpty(model.Email) ? model.Email : " ";
                    dbAccount.IsActive = model.IsActive;
                    dbAccount.IsDelete = false;
                    dbAccount.DateUpdated = DateTime.UtcNow;
                    dbAccount.Password = Encoding.ASCII.GetBytes(model.Password);
                    db.SaveChanges();
                    activeInactive = model.IsActive.HasValue ? model.IsActive.Value : false;
                    var dbCommunication = db.Communications.FirstOrDefault(x => x.AccountId == model.AccountId);
                    if (dbCommunication != null)
                    {
                        //model.PhoneNo = model.CountryCode;
                        StringBuilder sb = new StringBuilder();
                        foreach (char c in model.PhoneNo)
                        {
                            if ((c >= '0' && c <= '9'))                            
                                sb.Append(c);                            
                        }
                        model.PhoneNo = sb.ToString();
                        if (model.CountryCode != null)
                        {
                            if (model.CountryCode.Length > model.PhoneNo.Length)
                            {
                                int len = model.CountryCode.Length - model.PhoneNo.Length;
                                model.CountryCode = model.CountryCode.Insert(len, "-");
                                model.PhoneNo = model.CountryCode;
                            }
                        }
                        dbCommunication.Phone = model.PhoneNo;
                        //dbCommunication.CountryCode = model.CountryCode;
                        dbCommunication.DateUpdated = DateTime.UtcNow;
                    }
                    else
                    {
                        dbCommunication = new Communication();
                        dbCommunication.AccountId = model.AccountId;
                        dbCommunication.Phone = model.PhoneNo;
                        //dbCommunication.CountryCode = model.CountryCode;
                        dbCommunication.IsActive = true;
                        dbCommunication.IsDelete = false;
                        dbCommunication.DateCreated = dbCommunication.DateUpdated = DateTime.UtcNow;
                        db.Communications.Add(dbCommunication);
                    }
                    db.SaveChanges();
                    var CompanyDB = db.Companies.Where(x => x.AccountId == model.AccountId).FirstOrDefault();
                    CompanyName = CompanyDB != null ? CompanyDB.Name : string.Empty;
                    string cName = "";
                    if (model.BusinessName != null)
                    {
                        cName = SaveUniqueCompany(model.BusinessName, model.BillingAddress.City, dbAccount.AccountId);
                    }
                    else if (model.BusinessName == null)
                    {
                        cName = SaveUniqueCompany(model.FirstName + " " + model.LastName, model.BillingAddress.City, dbAccount.AccountId);
                    }
                    var dbcustomerOptionalInfo = db.CustomerOptionalInfoes.Where(x => x.AccountId == model.AccountId).FirstOrDefault();
                    if (dbcustomerOptionalInfo == null)
                    {
                        dbcustomerOptionalInfo = new CustomerOptionalInfo();
                        dbcustomerOptionalInfo.DateCreated = DateTime.UtcNow;
                    }
                    dbcustomerOptionalInfo.BusinessReseller = model.BusinessReseller;
                    dbcustomerOptionalInfo.TermId = model.SelectedTerm;
                    dbcustomerOptionalInfo.AccountId = model.AccountId;
                    dbcustomerOptionalInfo.Note = model.Note;
                    dbcustomerOptionalInfo.DisplayName = !string.IsNullOrEmpty(model.DisplayName) ? model.DisplayName : (!string.IsNullOrEmpty(cName) ? cName : model.FirstName + " " + model.LastName);
                    dbcustomerOptionalInfo.Discount = model.Discount;
                    if (model.Wholesale)
                    {
                        dbcustomerOptionalInfo.CustomerType = (int)Platini.Models.CustomerType.Wholesale;
                        if (bFileUp != null && bFileUp.ContentLength != 0 && bFileUp.InputStream != null)
                        {
                            string fileName;
                            System.Drawing.Imaging.ImageFormat format;
                            if (VerifyImage(bFileUp.ContentType.ToLower(), out fileName, out format))
                            {
                                if (!string.IsNullOrEmpty(dbcustomerOptionalInfo.BusinessLicense))
                                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + dbcustomerOptionalInfo.BusinessLicense));
                                dbcustomerOptionalInfo.BusinessLicense = fileName;
                                System.Drawing.Image.FromStream(bFileUp.InputStream).Save(Server.MapPath("~/Library/Uploads/" + fileName), format);
                            }
                            else
                                TempData["PageMessage"] = "File does not appear to be a valid image type.";
                        }
                    }
                    else
                    {
                        dbcustomerOptionalInfo.CustomerType = (int)Platini.Models.CustomerType.Retail;
                    }
                    dbcustomerOptionalInfo.DateUpdated = DateTime.UtcNow;
                    if (dbcustomerOptionalInfo.CustomerOptionalInfoId == 0)
                        db.CustomerOptionalInfoes.Add(dbcustomerOptionalInfo);
                    db.SaveChanges();

                    var person = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == model.AccountId);
                    if (person != null)
                    {
                        if (model.SelectedSalesPerson == 0)
                            db.CustomerSalesPersons.Remove(person);
                        else
                            person.SalesPersonId = model.SelectedSalesPerson;

                    }
                    else if (model.SelectedSalesPerson > 0)
                    {
                        person = new CustomerSalesPerson();
                        person.AccountId = model.AccountId;
                        person.SalesPersonId = model.SelectedSalesPerson;
                        db.CustomerSalesPersons.Add(person);
                    }


                    db.SaveChanges();

                    if (model.BillingAddress.City != null)
                    {
                        var dbAddress = (db.Addresses.Where(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.AccountId == model.AccountId && x.IsActive == true && x.IsDelete == false).Count() > 0) ? db.Addresses.Where(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.AccountId == model.AccountId && x.IsActive == true && x.IsDelete == false).Include(y => y.Account).FirstOrDefault() : new Address();

                        dbAddress.Street = model.BillingAddress.Street;
                        dbAddress.City = model.BillingAddress.City;
                        dbAddress.State = model.BillingAddress.State;
                        dbAddress.Pincode = model.BillingAddress.Pincode;
                        dbAddress.Country = model.BillingAddress.Country;
                        dbAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
                        dbAddress.IsActive = true;
                        dbAddress.IsDelete = false;
                        if (dbAddress.AccountId == 0)
                        {
                            dbAddress.AccountId = model.AccountId;
                            dbAddress.DateCreated = DateTime.UtcNow;
                            db.Addresses.Add(dbAddress);
                        }
                        dbAddress.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }

                    if (model.ShippingAddress.City != null && model.SameAsBillingCheckBox == false)
                    {
                        var dbAddress = (db.Addresses.Where(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.AccountId == model.AccountId && x.IsActive == true && x.IsDelete == false).Count() > 0) ? db.Addresses.Where(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.AccountId == model.AccountId && x.IsActive == true && x.IsDelete == false).Include(y => y.Account).FirstOrDefault() : new Address();

                        dbAddress.Street = model.ShippingAddress.Street;
                        dbAddress.City = model.ShippingAddress.City;
                        dbAddress.State = model.ShippingAddress.State;
                        dbAddress.Pincode = model.ShippingAddress.Pincode;
                        dbAddress.Country = model.ShippingAddress.Country;
                        dbAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
                        dbAddress.IsActive = true;
                        dbAddress.IsDelete = false;
                        if (dbAddress.AccountId == 0)
                        {
                            dbAddress.AccountId = model.AccountId;
                            dbAddress.DateCreated = DateTime.UtcNow;
                            db.Addresses.Add(dbAddress);
                        }
                        dbAddress.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }

                    if (model.SameAsBillingCheckBox == true)
                    {
                        var dbAddress = (db.Addresses.Where(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.AccountId == model.AccountId && x.IsActive == true && x.IsDelete == false).Count() > 0) ? db.Addresses.Where(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.AccountId == model.AccountId && x.IsActive == true && x.IsDelete == false).Include(y => y.Account).FirstOrDefault() : new Address();
                        if (dbAddress.AccountId != 0)
                        {
                            db.Addresses.Remove(dbAddress);
                            db.SaveChanges();
                        }

                        Address shippingAddress = new Address();
                        shippingAddress.InjectClass(model.BillingAddress);
                        shippingAddress.AccountId = 0;
                        shippingAddress.AccountId = model.AccountId;
                        shippingAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
                        shippingAddress.IsActive = true;
                        shippingAddress.IsDelete = false;
                        shippingAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
                        db.Addresses.Add(shippingAddress);
                        db.SaveChanges();
                    }
                    if (model.FromInActiveCustomerPage == "true")
                    {
                        return RedirectToAction("InactiveCustomer");
                    }
                    if (QuickBookStrings.UseQuickBook())
                        AddCustomerToQuickBook(dbAccount.AccountId, CompanyName);
                    return RedirectToAction("Index");
                }
                catch 
                {                  
                }
            }
            return View("CreateOrEdit", model);
        }

        public ActionResult Delete(string tId, int? fType, string sortOrder, string sortColumn, string searchString, int Id = 0)
        {
            Platini.DB.Account dbaccount = db.Accounts.Find(Id);
            if (dbaccount != null)
            {
                dbaccount.IsDelete = true;
                dbaccount.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                TempData["PageMessage"] = "This was successfully deleted  ";
            }
            else
            {
                TempData["PageMessage"] = "Record not found";
            }
            return RedirectToAction("Index", new { Id = tId, @fType = fType, @sortOrder = sortOrder, @sortColumn = sortColumn, searchString = searchString });

        }

        public string ShowShippingDiv(Address billing, Address shipping)
        {
            string value = "true";

            if (shipping.City == null || billing.Street == shipping.Street && billing.City == shipping.City && billing.State == shipping.State && billing.Pincode == shipping.Pincode)
            {
                value = "true";
            }
            else { value = "false"; }

            return value;
        }

        [HttpPost]
        public void AssociateSelectedSalesPerson(int selectedAssociateSalesPersonId, List<int> selectedCustomerIds)
        {
            //var list = (from r in db.Customer_SalesPerson
            //            join r1 in selectedCustomerIds on r.CustomerId equals r1
            //            select r).ToList();

            //foreach (var item in list)
            //{
            //    item.SalesPersonId = selectedAssociateSalesPersonId;
            //    db.SaveChanges();
            //}


            if (selectedAssociateSalesPersonId > 0 && selectedCustomerIds.Count > 0)
            {
                foreach (var item in selectedCustomerIds)
                {
                    var dbRel = db.CustomerSalesPersons.Where(x => x.AccountId == item).FirstOrDefault();
                    if (dbRel == null)
                    {
                        dbRel = new CustomerSalesPerson()
                        {
                            AccountId = item,
                            SalesPersonId = selectedAssociateSalesPersonId,
                            IsSalesPersonContact = 1,
                        };
                        db.CustomerSalesPersons.Add(dbRel);
                    }
                    else
                        dbRel.SalesPersonId = selectedAssociateSalesPersonId;
                    db.SaveChanges();
                }
            }
        }

        public ActionResult InactiveCustomer(int? page, string searchString)
        {
            int userId = 0;
            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
                int.TryParse(SiteIdentity.UserId, out userId);

            string SQL = @"select AccountId,FirstName,LastName,Email,IsActive,IsDelete,Username,LastLoginDate,
                           ISNULL((select top 1 DisplayName from CustomerOptionalInfo i where i.AccountId=a.AccountId),ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),a.FirstName+' '+a.LastName))as DisplayName,
                           ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),'')as BusinessName,
                           ISNULL((select COUNT(c.CustomerUserId) from CustomerUser c where  c.AccountId=a.AccountId),0) as UserCount,
                           ISNULL((select top 1 c.SalesPersonId from CustomerSalesPerson c where  c.AccountId=a.AccountId),0) as SelectedSalesPerson,
                           ISNULL((select top 1 p.Phone from Communication p where p.AccountId=a.AccountId and p.IsActive=1 and p.IsDelete=0),'') as PhoneNo,
                           ISNULL((select top 1 d.City from Address d where d.AccountId=a.AccountId and d.IsActive=1 and d.IsDelete=0),'') as City,
                           ISNULL((select top 1 d1.State from Address d1 where d1.AccountId=a.AccountId and d1.IsActive=1 and d1.IsDelete=0),'') as State
                           from Account a where RoleId=5 and IsActive=0 and IsDelete=0";
            var list = db.Database.SqlQuery<CustomerClass>(SQL);
            if (userId > 0)
                list = list.Where(x => x.SelectedSalesPerson == userId);
            string nameCategory = searchString;
            if (!string.IsNullOrEmpty(nameCategory))
            {
                nameCategory = nameCategory.ToLower();
                list = list.Where(e => e.BusinessName.ToLower().Contains(nameCategory) || e.Email.ToLower().Contains(nameCategory) || e.PhoneNo.ToLower().Contains(nameCategory) || e.FirstName.ToLower().Contains(nameCategory)
                    || e.LastName.ToLower().Contains(nameCategory) || e.City.ToLower().Contains(nameCategory));
            }
            ViewBag.searchStringParam = searchString;
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.isWH = false;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(list.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult InactiveWholeSeller(int? page, string searchString)
        {
            int userId = 0;
            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
                int.TryParse(SiteIdentity.UserId, out userId);

            string SQL = @"select a.AccountId,FirstName,LastName,Email,IsActive,IsDelete,Username,LastLoginDate,
                           ISNULL(i.DisplayName,ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),a.FirstName+' '+a.LastName))as DisplayName,
                           ISNULL((select top 1 Name from Company c where c.IsActive=1 and c.IsDelete=0 and c.AccountId=a.AccountId),'')as BusinessName,
                           ISNULL((select COUNT(c.CustomerUserId) from CustomerUser c where  c.AccountId=a.AccountId),0) as UserCount,
                           ISNULL((select top 1 c.SalesPersonId from CustomerSalesPerson c where  c.AccountId=a.AccountId),0) as SelectedSalesPerson,
                           ISNULL((select top 1 p.Phone from Communication p where p.AccountId=a.AccountId and p.IsActive=1 and p.IsDelete=0),'') as PhoneNo,
                           ISNULL((select top 1 d.City from Address d where d.AccountId=a.AccountId and d.IsActive=1 and d.IsDelete=0),'') as City,
                           ISNULL((select top 1 d1.State from Address d1 where d1.AccountId=a.AccountId and d1.IsActive=1 and d1.IsDelete=0),'') as State
                           from Account a 
                           left outer join CustomerOptionalInfo i on a.AccountId=i.AccountId
                           where RoleId=5 and IsActive=0 and IsDelete=0 and i.CustomerType=1";
            var list = db.Database.SqlQuery<CustomerClass>(SQL);
            if (userId > 0)
                list = list.Where(x => x.SelectedSalesPerson == userId);
            string nameCategory = searchString;
            if (!string.IsNullOrEmpty(nameCategory))
            {
                nameCategory = nameCategory.ToLower();
                list = list.Where(e => e.BusinessName.ToLower().Contains(nameCategory) || e.Email.ToLower().Contains(nameCategory) || e.PhoneNo.ToLower().Contains(nameCategory) || e.FirstName.ToLower().Contains(nameCategory)
                    || e.LastName.ToLower().Contains(nameCategory) || e.City.ToLower().Contains(nameCategory));
            }
            ViewBag.isWH = true;
            ViewBag.searchStringParam = searchString;
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View("InactiveCustomer", list.ToPagedList(currentPageIndex, defaultpageSize));
        }

        [HttpPost]
        public void Activate(List<int> selectedCustomerIds)
        {
            var list = (from r in db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer && x.IsActive == false && x.IsDelete == false)
                        join r1 in selectedCustomerIds on r.AccountId equals r1
                        select r).ToList();

            foreach (var item in list)
            {
                item.IsActive = true;
                item.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                string email = string.Empty;
                if (item.Companies.Any(x => x.IsActive == true && x.IsDelete == false))
                    email = item.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Name;
                EmailManager.SendActivationEmail(item.Email, (email == string.Empty ? item.FirstName + " " + item.LastName : email));
            }
            if (QuickBookStrings.UseQuickBook())
            {
                foreach (var item in list)
                    AddCustomerToQuickBook(item.AccountId, string.Empty);
            }
        }

        [HttpGet]
        public ActionResult SendPassword(string tId, int? fType, string sortOrder, string sortColumn, string searchString, int Id = 0)
        {
            var account = db.Accounts.Find(Id);
            if (account != null)
            {
                string password = Encoding.ASCII.GetString(account.Password);
                var value = EmailManager.SendForgotPasswordEmail(account.Email, account.UserName, password);
                TempData["PageMessage"] = "Password sent successfully";
            }
            return RedirectToAction("Index", new { Id = tId, @fType = fType, @sortOrder = sortOrder, @sortColumn = sortColumn, searchString = searchString });
        }

        public string SaveUniqueCompany(string companyName, string city, int accountId)
        {
            string cName = companyName;
            bool check = db.Companies.Where(x => x.Name == cName && x.AccountId != accountId).Any();
            if (check)
            {
                cName = companyName + "-" + city;
                check = db.Companies.Where(x => x.Name == cName && x.AccountId != accountId).Any();
                if (check)
                    cName = companyName + "-" + city + "-" + accountId;
            }
            var dbCompany = db.Companies.Where(x => x.AccountId == accountId).FirstOrDefault();
            if (dbCompany != null)
            {
                dbCompany.Name = cName;
                dbCompany.AccountId = accountId;
            }
            else
            {
                dbCompany = new Platini.DB.Company();
                dbCompany.Name = cName;
                dbCompany.AccountId = accountId;
                dbCompany.IsActive = true;
                dbCompany.IsDelete = false;
                dbCompany.DateCreated = dbCompany.DateUpdated = DateTime.UtcNow;
                db.Companies.Add(dbCompany);
            }
            db.SaveChanges();
            return cName;
        }

        [NonAction]
        public bool AddCustomerToQuickBook(int CustomerId, string CompName)
        {
            QuickBookStrings.LoadQuickBookStrings(FailureFrom.Customer.ToString());
            string CompanyName = string.Empty;
            string Type = FailureFrom.Customer.ToString();
            var oauthValidator = new OAuthRequestValidator(QuickBookStrings.AccessToken, QuickBookStrings.AccessTokenSecret, QuickBookStrings.ConsumerKey, QuickBookStrings.ConsumerSecret);
            QuickBookFailureRecord existFailure = null;
            if (CustomerId > 0)
                existFailure = db.QuickBookFailureRecords.FirstOrDefault(x => x.FailureFrom.ToLower() == Type.ToLower() && x.FailureFromId == CustomerId);
            try
            {
                var context = new ServiceContext(QuickBookStrings.AppToken, QuickBookStrings.CompanyId, IntuitServicesType.QBO, oauthValidator);
                context.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = GetLoc(Type, CustomerId.ToString());
                context.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl; 
                var service = new DataService(context);
                var customer = new Customer();
                var objCustomer = new Customer();
                var term = new Intuit.Ipp.Data.Term();
                var account = new QueryService<Customer>(context);
                var regstAccount = db.Accounts.Find(CustomerId);
                if (existFailure != null)
                {
                    CompanyName = existFailure.FailureOriginalValue;
                }
                else if (regstAccount != null)
                {
                    if (regstAccount.IsActive==true && regstAccount.IsDelete == false && regstAccount.Companies.FirstOrDefault() != null)
                        CompanyName = regstAccount.Companies.FirstOrDefault().Name;
                }
                CompanyName = CompanyName.Replace("'", "");
                CompanyName = CompanyName.Replace(":", " ");
                if (!string.IsNullOrEmpty(CompanyName))
                {
                    var billingAddress = regstAccount.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);
                    var shippingAddress = regstAccount.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
                    if (!string.IsNullOrEmpty(CompName))
                        objCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompName.Trim() + "'").FirstOrDefault();
                    else
                        objCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompanyName.Trim() + "'").FirstOrDefault();

                    var dbcustomerOptionalInfo = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == regstAccount.AccountId);
                    if (regstAccount.Communications.FirstOrDefault() != null)
                    {
                        string Number = string.Empty;
                        string[] split = regstAccount.Communications.FirstOrDefault().Phone.Split('-');
                        if (split.Count() > 1)
                            Number = !string.IsNullOrEmpty(split[1]) ? split[1] : string.Empty;
                        else
                            Number = !string.IsNullOrEmpty(split[0]) ? split[0] : string.Empty;

                        regstAccount.Communications.FirstOrDefault().Phone = Number;
                    }
                    var dbterms = db.Terms.Where(x => x.TermId == dbcustomerOptionalInfo.TermId && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                    var terms = new QueryService<Intuit.Ipp.Data.Term>(context);
                    string termName = dbterms.Name != null ? dbterms.Name : string.Empty;
                    term = terms.ExecuteIdsQuery("Select * From Term Where Name='" + termName + "'").FirstOrDefault();
                    if (objCustomer != null)
                    {
                        objCustomer.DisplayName = CompanyName.Trim();
                        objCustomer.CompanyName = CompanyName.Trim();
                        objCustomer.GivenName = regstAccount.FirstName;
                        objCustomer.FamilyName = regstAccount.LastName;


                        objCustomer.PrimaryPhone = new TelephoneNumber() { FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Phone : "" };
                        objCustomer.PrimaryEmailAddr = new EmailAddress() { Address = regstAccount.Email };
                        if (billingAddress != null)
                            objCustomer.BillAddr = new PhysicalAddress() { City = billingAddress.City, Line1 = billingAddress.Street, PostalCode = billingAddress.Pincode, CountrySubDivisionCode = billingAddress.State, Country = billingAddress.Country };
                        if (shippingAddress != null)
                            objCustomer.ShipAddr = new PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State, Country= shippingAddress.Country };
                        objCustomer.Fax = new TelephoneNumber() { AreaCode = "01", FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Fax : "" };
                        objCustomer.Active = activeInactive;
                        if (term != null)
                            objCustomer.SalesTermRef = new ReferenceType() { name = term.Name, Value = term.Id };
                        objCustomer.ResaleNum = dbcustomerOptionalInfo.BusinessReseller;
                        service.Update(objCustomer);
                    }
                    else
                    {
                        objCustomer = new Customer();
                        objCustomer.DisplayName = CompanyName.Trim();
                        objCustomer.CompanyName = CompanyName.Trim();
                        objCustomer.GivenName = regstAccount.FirstName;
                        objCustomer.FamilyName = regstAccount.LastName;
                        objCustomer.PrimaryPhone = new TelephoneNumber() { FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Phone : "" };
                        objCustomer.PrimaryEmailAddr = new EmailAddress() { Address = regstAccount.Email };
                        if (billingAddress != null)
                            objCustomer.BillAddr = new PhysicalAddress() { City = billingAddress.City, Line1 = billingAddress.Street, PostalCode = billingAddress.Pincode, CountrySubDivisionCode = billingAddress.State, Country = billingAddress.Country };
                        if (shippingAddress != null)
                            objCustomer.ShipAddr = new PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State, Country = shippingAddress.Country };
                        objCustomer.Fax = new TelephoneNumber() { AreaCode = "01", FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Fax : "" };
                        objCustomer.Active = activeInactive;
                        if (term != null)
                            objCustomer.SalesTermRef = new ReferenceType() { name = term.Name, Value = term.Id };                        
                        objCustomer.ResaleNum = dbcustomerOptionalInfo.BusinessReseller;
                        service.Add(objCustomer);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                var newFailure = new QuickBookFailureRecord();
                newFailure.FailureFrom = FailureFrom.Customer.ToString();
                newFailure.FailureFromId = CustomerId;
                newFailure.FailureOriginalValue = CompanyName;
                newFailure.ErrorDetails = FailureText(ex, Type, CustomerId.ToString());
                newFailure.FailureOriginalValue = CompanyName;
                newFailure.FailureDate = DateTime.UtcNow;
                db.QuickBookFailureRecords.Add(newFailure);
                db.SaveChanges();
            }
            return false;
        }

        [NonAction]
        public string GetLoc(string type, string Id)
        {
            var di = new System.IO.DirectoryInfo(Server.MapPath("~/Library/Uploads/" + type + "/" + Id));
            di.Create();
            return di.FullName;
        }
        [NonAction]
        public string FailureText(Exception ex, string type, string Id)
        {
            var di = new System.IO.DirectoryInfo(GetLoc(type, Id));
            int id = 0;
            int.TryParse(SiteIdentity.UserId, out id);
            var dbuser = db.Accounts.Find(id);
            string userName = dbuser != null ? dbuser.UserName : string.Empty;
            HttpContext con = System.Web.HttpContext.Current;

            // Exception 
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("User :           " + userName);
            sb.AppendLine("Attempt :        " + type);
            sb.AppendLine("Page :           " + con.Request.Url.ToString());
            sb.AppendLine("Error Message :  " + ex.Message);
            sb.AppendLine("Inner Message :  " + ex.InnerException == null ? " " : ex.InnerException.ToString());

            // Here save text file containing this error details
            string fileName = Path.Combine(di.FullName + "\\" + Id + ".txt");
            System.IO.File.WriteAllText(fileName, sb.ToString());

            string retSting = string.Empty;
            var file = di.GetFiles("*", System.IO.SearchOption.AllDirectories).OrderByDescending(x => x.LastWriteTime).FirstOrDefault();
            if (file != null)
            {
                var reader = new StreamReader(file.FullName);
                retSting = reader.ReadToEnd();
                reader.Dispose();
            }
            return retSting;
        }

        public ActionResult ShowRoomImages(int Id, string from)
        {
            var account = db.Accounts.Find(Id);
            if (account != null)
            {
                var model = new ShowRoomImages();
                model.AccountId = account.AccountId;
                model.Images = account.ShowRoomImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                ViewBag.From = from;
                return View(model);
            }
            TempData["PageMessage"] = "No account found.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult mFileUpload(IEnumerable<HttpPostedFileBase> files, int AccountId)
        {
            var account = db.Accounts.Find(AccountId);
            if (account != null && files != null)
            {
                int so = 1;
                var imageOrder = account.ShowRoomImages.Where(x => x.AccountId == AccountId && x.IsActive && !x.IsDelete).ToList().OrderBy(x => x.SortOrder).LastOrDefault();
                if (imageOrder != null)
                    so = (imageOrder.SortOrder.HasValue ? imageOrder.SortOrder.Value : 0) + 1;
                foreach (HttpPostedFileBase file in files)
                {
                    var image = SaveUpdateClothesImage(file, so, AccountId);
                    if (image != null)
                        return Json(new { @Path = image.ImagePath, Name = @image.ImageName, Id = image.ShowRoomImageId, SO = image.SortOrder }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json("error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteImage(int Id)
        {

            var image = db.ShowRoomImages.Find(Id);
            if (image != null)
            {
                if (!string.IsNullOrEmpty(image.ImagePath))
                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + image.ImagePath));
                image.IsDelete = true;
                image.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SaveSort(string NewOrders)
        {
            if (!string.IsNullOrEmpty(NewOrders))
            {
                var collection = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SortOrders>>(NewOrders);
                foreach (var item in collection)
                {
                    int ShowroomImageId = 0;
                    int.TryParse(item.id, out ShowroomImageId);
                    if (ShowroomImageId > 0)
                    {
                        var image = db.ShowRoomImages.Find(ShowroomImageId);
                        if (image != null && !image.IsDelete && image.IsActive && image.SortOrder != item.so)
                        {
                            image.SortOrder = item.so;
                            image.DateUpdated = DateTime.UtcNow;
                        }
                        db.SaveChanges();
                    }
                }
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        public ShowRoomImage SaveUpdateClothesImage(HttpPostedFileBase upPic, int SortOrder, int AccountId)
        {
            string fileName;
            System.Drawing.Imaging.ImageFormat format;
            if (VerifyImage(upPic.ContentType.ToLower(), out fileName, out format))
            {
                var roomImage = db.ShowRoomImages.Where(i => i.AccountId == AccountId && i.SortOrder == SortOrder).FirstOrDefault();
                if (roomImage != null)
                {
                    string imagePath = roomImage.ImagePath;
                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + imagePath));
                }
                else
                {
                    roomImage = new ShowRoomImage();
                    roomImage.DateCreated = DateTime.UtcNow;
                }
                roomImage.ImageName = upPic.FileName;
                roomImage.ImagePath = fileName;
                roomImage.AccountId = AccountId;
                roomImage.SortOrder = SortOrder;
                roomImage.IsActive = true;
                roomImage.IsDelete = false;
                roomImage.DateUpdated = DateTime.UtcNow;
                db.ShowRoomImages.Add(roomImage);
                db.SaveChanges();
                System.Drawing.Image.FromStream(upPic.InputStream).Save(Server.MapPath("~/Library/Uploads/" + fileName), format);
                return roomImage;
            }
            return null;
        }

        [NonAction]
        protected bool VerifyImage(string contentType, out string fileName, out System.Drawing.Imaging.ImageFormat format)
        {
            bool proceed = true;
            fileName = Guid.NewGuid().ToString().GetHashCode().ToString("x");
            format = System.Drawing.Imaging.ImageFormat.Jpeg;
            switch (contentType)
            {
                case "image/jpg":
                    fileName += ".jpg";
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case "image/jpeg":
                    fileName += ".jpeg";
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case "image/png":
                    fileName += ".png";
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    break;
                case "image/gif":
                    fileName += ".gif";
                    format = System.Drawing.Imaging.ImageFormat.Gif;
                    break;
                case "image/bmp":
                    fileName += ".bmp";
                    format = System.Drawing.Imaging.ImageFormat.Bmp;
                    break;
                default:
                    fileName = "";
                    format = null;
                    proceed = false;
                    break;
            }
            return proceed;
        }
    }
}
