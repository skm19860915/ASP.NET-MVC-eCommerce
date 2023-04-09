using Platini.DB;
using Platini.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using MvcPaging;

namespace Platini.Areas.Common.Controllers
{
    public class HomeController : Controller
    {
        Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);

        public ActionResult Index()
        {
            ViewBag.PageMessage = TempData["PageMessage"];
            if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower())
                return View("Index", "~/Areas/Common/Views/Shared/_Layout2.cshtml");
            return View();
        }

        public ActionResult Account()
        {
            int id = 0;
            int.TryParse(SiteIdentity.UserId, out id);
            var dbuser = db.Accounts.Find(id);
            if (dbuser != null)
            {
                UserClass model = new UserClass();
                model.InjectClass(dbuser);
                model.Password = Encoding.ASCII.GetString(dbuser.Password);

                var communication = dbuser.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false);
                if (communication != null)
                {
                    model.PhoneNo = communication.Phone;
                }
                var company = dbuser.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false);
                if (company != null)
                {
                    model.BusinessName = company.Name;
                    model.CustomerId = company.AccountId;
                }
                var salesPerson = dbuser.CustomerSalesPersons.FirstOrDefault();
                if (salesPerson != null)
                {
                    var salesAccount = db.Accounts.Single(x => x.AccountId == salesPerson.SalesPersonId && x.RoleId == (int)RolesEnum.SalesPerson);
                    model.SalesPerson = salesAccount.FirstName + " " + salesAccount.LastName;
                }

                var cutomerInfo = dbuser.CustomerOptionalInfoes.FirstOrDefault();
                if (cutomerInfo != null && cutomerInfo.CustomerType == (int)CustomerType.Wholesale)
                    model.BusinessLicense = cutomerInfo.BusinessLicense;

                if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower())
                    return View("Account", "~/Areas/Common/Views/Shared/_Layout2.cshtml", model);
                return View("Account", model);
            }

            TempData["PageMessage"] = "Record not found";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Account(UserClass user, HttpPostedFileBase bFileUp)
        {
            if (ModelState.IsValid)
            {
                var dbAccount = db.Accounts.Find(user.AccountId);
                if (dbAccount != null)
                {
                    int RoleId = dbAccount.RoleId;
                    bool IsActive = dbAccount.IsActive.HasValue ? dbAccount.IsActive.Value : false;
                    dbAccount.InjectClass(user);
                    dbAccount.RoleId = RoleId;
                    dbAccount.Password = Encoding.ASCII.GetBytes(user.Password);
                    dbAccount.IsActive = IsActive;
                    dbAccount.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();

                    var dbCommunication = db.Communications.FirstOrDefault(x => x.AccountId == dbAccount.AccountId);
                    if (dbCommunication != null)
                    {
                        dbCommunication.Phone = user.PhoneNo;
                    }
                    else
                    {
                        dbCommunication = new Communication();
                        dbCommunication.AccountId = dbAccount.AccountId;
                        dbCommunication.Phone = user.PhoneNo;
                        db.Communications.Add(dbCommunication);
                    }
                    dbCommunication.DateUpdated = DateTime.UtcNow;
                    dbCommunication.IsActive = true;
                    dbCommunication.IsDelete = false;
                    db.SaveChanges();
                    var cutomerInfo = dbAccount.CustomerOptionalInfoes.FirstOrDefault();
                    if (bFileUp != null && cutomerInfo != null && cutomerInfo.CustomerType == (int)CustomerType.Wholesale && bFileUp.ContentLength != 0 && bFileUp.InputStream != null)
                    {
                        string fileName;
                        System.Drawing.Imaging.ImageFormat format;
                        if (VerifyImage(bFileUp.ContentType.ToLower(), out fileName, out format))
                        {
                            if (!string.IsNullOrEmpty(cutomerInfo.BusinessLicense))
                                System.IO.File.Delete(Server.MapPath("~/Library/Uploads/" + cutomerInfo.BusinessLicense));
                            cutomerInfo.BusinessLicense = fileName;
                            cutomerInfo.DateUpdated = DateTime.UtcNow;
                            System.Drawing.Image.FromStream(bFileUp.InputStream).Save(Server.MapPath("~/Library/Uploads/" + fileName), format);
                            db.SaveChanges();
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            ViewBag.PageMessage = "File does not appear to be a valid image type.";
                            if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower())
                                return View("Account", "~/Areas/Common/Views/Shared/_Layout2.cshtml", user);
                            return View("Account", user);
                        }
                    }
                    else
                        return RedirectToAction("Index");
                }
                ViewBag.PageMessage = "Record not found";
            }
            if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower())
                return View("Account", "~/Areas/Common/Views/Shared/_Layout2.cshtml", user);
            return View("Account", user);
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


        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSubCategoryByCategoryId(string categoryId)
        {
            if (String.IsNullOrEmpty(categoryId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = int.TryParse(categoryId, out id);
            if (isValid == true)
            {
                var result = db.Categories.Where(x => x.ParentId == id && x.IsActive == true && x.IsDelete == false).OrderBy(y => y.SortOrder).ToList().Select(z => new SelectListItem { Text = z.Name, Value = z.CategoryId.ToString() });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSubCategoryTypeBySubCategoryId(string subCategoryId)
        {
            if (String.IsNullOrEmpty(subCategoryId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(subCategoryId, out id);
            if (isValid == true)
            {
                var result = db.Categories.Where(x => x.ParentId == id && x.IsActive == true && x.IsDelete == false).OrderBy(y => y.SortOrder).ToList().Select(z => new SelectListItem { Text = z.Name, Value = z.CategoryId.ToString() });
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSizeGroupBySubCategoryTypeId(string subCategoryTypeId)
        {
            if (String.IsNullOrEmpty(subCategoryTypeId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(subCategoryTypeId, out id);
            if (isValid == true)
            {
                var result = db.SizeGroups.Where(x => x.CategoryId == id && x.IsActive == true && x.IsDelete == false).ToList().Select(z => new SelectListItem { Text = z.Name, Value = z.SizeGroupId.ToString() }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetSizesBySizeGroupId(string sizeGroupId)
        {
            if (String.IsNullOrEmpty(sizeGroupId))
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            int id = 0;
            bool isValid = Int32.TryParse(sizeGroupId, out id);
            if (isValid == true)
            {
                var result = db.Sizes.Where(x => x.SizeGroupId == id && x.IsActive == true).Select(y => new { Id = y.SizeId, Name = y.Name, SubCategoryTypesId = y.CategoryId, SizeGroupId = y.SizeGroupId }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult FutureDeliveries(int? page, string searchString, string sortOrder, string sortColumn = "FutureDeliveryDate")
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

            var tempDate = new DateTime(1900, 1, 1);
            var clothes = db.Clothes.Where(i => i.IsActive == true && i.IsDelete == false && i.FutureDeliveryDate.HasValue && i.FutureDeliveryDate > tempDate).OrderBy(x => x.FutureDeliveryDate).ToList();
            string nameCategory = searchString;
            if (!ReferenceEquals(nameCategory, null))
                clothes = clothes.Where(e => e.StyleNumber.ToLower().Contains(nameCategory.ToLower())).ToList();

            List<ClothListItem> retList = new List<ClothListItem>().InjectFrom(clothes);
            retList.ForEach(x => { x.ImagePath = clothes.Where(y => y.ClothesId == x.ClothesId).FirstOrDefault().ClothesImages.Count(z => z.IsActive && !z.IsDelete && !string.IsNullOrEmpty(z.ImagePath)) > 0 ? clothes.Where(y => y.ClothesId == x.ClothesId).FirstOrDefault().ClothesImages.OrderBy(z => z.SortOrder).FirstOrDefault(a => a.IsActive && !a.IsDelete && !string.IsNullOrEmpty(a.ImagePath)).ImagePath : "NO_IMAGE.jpg"; });
            Type sortByPropType = typeof(ClothListItem).GetProperty(sortColumn).PropertyType;
            retList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(ClothListItem), sortByPropType })
                                        .Invoke(retList, new object[] { retList, sortColumn, sortOrder }) as List<ClothListItem>;

            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(retList.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult DeactivatedProducts(int? page, string searchString, string sortOrder, string sortColumn = "StyleNumber")
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

            var clothes = db.Clothes.Where(i => i.IsActive == false && i.IsDelete == false).OrderBy(x => x.StyleNumber).ToList();
            string nameCategory = searchString;
            if (!ReferenceEquals(nameCategory, null))
                clothes = clothes.Where(e => e.StyleNumber.ToLower().Contains(nameCategory.ToLower())).ToList();

            List<ClothListItem> retList = new List<ClothListItem>().InjectFrom(clothes);
            Type sortByPropType = typeof(ClothListItem).GetProperty(sortColumn).PropertyType;
            retList = typeof(MyExtensions).GetMethod("CustomSort").MakeGenericMethod(new Type[] { typeof(ClothListItem), sortByPropType })
                                        .Invoke(retList, new object[] { retList, sortColumn, sortOrder }) as List<ClothListItem>;

            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(retList.ToPagedList(currentPageIndex, defaultpageSize));
        }
      
        [HttpPost]
        public void ActivateClothes(List<int> selectedClothIds)
        {
            var list = (from r in db.Clothes.Where(x => x.IsActive == false)
                        join r1 in selectedClothIds on r.ClothesId equals r1
                        select r).ToList();

            foreach (var item in list)
            {
                item.IsActive = true;
                item.SortOrder = 0;
                item.DateChanged = item.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

        [HttpPost]
        public void ActivateFutureClothes(List<int> selectedClothIds)
        {
            var list = db.Clothes.Where(x => selectedClothIds.Contains(x.ClothesId)).ToList();

            foreach (var item in list)
            {
                item.IsActive = true;
                item.FutureDeliveryDate = null;
                item.SortOrder = 0;
                item.DateChanged = item.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

        public ActionResult Detail()
        {
            return View();
        }


        public ActionResult NewsLetterSignUps(int? page, string searchString)
        {
            ViewBag.searchStringParam = searchString;
            var list = db.NewsLetterSignUps.Where(x => !x.IsDelete && (!string.IsNullOrEmpty(searchString) ? x.Email.Contains(searchString) : true)).ToList().Select(x => new SelectedListValues() { Id = x.NewsEmailId, Value = x.Email });
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            return View(list.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult Export()
        {
            StringBuilder sb = new StringBuilder();
            string sFileName = "Emails.xls";
            var Data = db.NewsLetterSignUps.Where(i => !i.IsDelete).ToList();
            if (Data != null && Data.Any())
            {
                sb.Append("<table style='1px solid black; font-size:14px;'>");
                sb.Append("<tr>");
                sb.Append("<td style='width:300px;font-size:16px'><b>Email</b></td>");
                sb.Append("</tr>");
                foreach (var result in Data)
                {
                    sb.Append("<tr>");
                    sb.Append("<td>" + (result.Email) + "</td>");
                    sb.Append("</tr>");
                }
            }
            HttpContext.Response.AddHeader("content-disposition", "attachment; filename=" + sFileName);
            this.Response.ContentType = "application/vnd.ms-excel";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(buffer, "application/vnd.ms-excel");
        }

        public ActionResult OnlineNow(int? page)
        {
            var list = SiteConfiguration.GetApp(HttpContext.ApplicationInstance.Application).SelectMany(x => db.Accounts.Where(y => y.AccountId == x.Id).DefaultIfEmpty(), (x, y) => new OnlineCustomers()
            {
                AccountId = y != null ? y.AccountId : 0,
                Name = y != null ? (y.FirstName + " " + y.LastName) : "Visitor",
                SessionId = x.Value,
                Email = y != null ? y.Email : "",
            }).ToList();
            foreach (var item in list)
            {
                if (item.AccountId > 0)
                {
                    var phone = db.Communications.FirstOrDefault(x => x.AccountId == item.AccountId && x.IsActive == true && x.IsDelete == false);
                    if (phone != null)
                        item.Phone = phone.Phone;
                    var address = db.Addresses.FirstOrDefault(x => x.AccountId == item.AccountId && x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);
                    if(address!=null)
                    {
                        item.City = address.City;
                        item.State = address.State;
                    }
                }
            }
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            ViewBag.isVisitor = false;
            return View(list.ToPagedList(currentPageIndex, defaultpageSize));
        }

        public ActionResult Visitors(int? page)
        {
            DateTime toDate = DateTime.UtcNow.AddHours(-24);
            DateTime fromDate = DateTime.UtcNow;
            //var list = db.VisitorLogs.Where(x => (x.DateUpdated.HasValue ? (x.DateUpdated.Value <= fromDate && x.DateUpdated.Value >= toDate) : false) && !x.AccountId.HasValue).ToList().Select(x => new OnlineCustomers()
            //{
            //    AccountId = 0,
            //    Name = "Visitor" + ((!string.IsNullOrEmpty(x.CountryName) || !string.IsNullOrEmpty(x.StateName) || !string.IsNullOrEmpty(x.CityName)) ? " - " + (!string.IsNullOrEmpty(x.CityName) ? x.CityName + " " : "") + (!string.IsNullOrEmpty(x.StateName) ? x.StateName + " " : "") + x.CountryName : ""),
            //    SessionId = x.DateUpdated.Value.ToString("MM/dd/yyyy hh:mm:ss")
            //});

            var list = db.VisitorLogs.Where(x => (x.DateUpdated.HasValue ? (x.DateUpdated.Value <= fromDate && x.DateUpdated.Value >= toDate) : false) && !x.AccountId.HasValue).OrderByDescending(x => x.DateUpdated).ToList().Select(x => new SiteVisitors()
            {
                Name = "Visitor-" + x.IPAddress,
                City = x.CityName,
                State = x.StateName,
                Country = x.CountryName,
                Date = x.DateUpdated.Value.ToString("MM/dd/yyyy hh:mm:ss")
            });
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            ViewBag.PageMessage = TempData["PageMessage"];
            ViewBag.isVisitor = true;
            return View(list.ToPagedList(currentPageIndex, 20));
        }
    }
}
