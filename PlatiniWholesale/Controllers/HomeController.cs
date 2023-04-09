
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Threading;
using Newtonsoft.Json.Linq;
using Platini.DB;
using System.Xml;
using System.Configuration;
using MvcPaging;
using Platini.Models;
using System.Web.Script.Serialization;
using Intuit.Ipp.Security;
using Intuit.Ipp.Core;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Data;
using System.Data.SqlClient;
using System;
using Intuit.Ipp.QueryFilter;
using HiQPdf;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PayPal;
using PayPal.Api;
using Platini.Models.PasswordGenerator;
using System.Text.RegularExpressions;

namespace Platini.Controllers
{
    public class HomeController : Controller
    {
        Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);
        public ActionResult Index(string SuccMsg, string mode, int? id, int? ActivateAccountId, int? DeActivateAccountId, Guid? OrdId, string returnUrl)
        {
            SaveIp();
            if (!string.IsNullOrEmpty(returnUrl))
            {
                string[] Arr = returnUrl.Split('=');
                returnUrl = Arr[1];            }
            if (string.IsNullOrEmpty(SiteIdentity.UserId))
                SiteConfiguration.AddToApp(HttpContext.ApplicationInstance.Application, new SelectedListValues() { Id = 0, Value = Session.SessionID });
            if (OrdId.HasValue)
                TempData["CartSuccess"] = "jkl";
            ViewBag.CartHidden = TempData["HCartValue"];
            if (ActivateAccountId.HasValue)
            {
                var account = db.Accounts.Find(ActivateAccountId.Value);
                if (account != null)
                {
                    account.IsActive = true;
                    account.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                    string dbPassword = Encoding.ASCII.GetString(account.Password);
                    EmailManager.SendWelcomeEmailCustomer(account.Email, account.AccountId.ToString(), account.UserName, dbPassword);
                    TempData["PageMessage"] = "Customer account has been activitated successfully!";
                    if (QuickBookStrings.UseQuickBook())
                        AddCustomerToQuickBook(account.AccountId);
                }
            }
            if (DeActivateAccountId.HasValue)
            {
                var account = db.Accounts.Find(DeActivateAccountId.Value);
                if (account != null)
                {
                    account.IsActive = false;
                    account.IsDelete = true;
                    account.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                    TempData["PageMessage"] = "Customer account has been deleted successfully!";
                }
            }
            ViewBag.PageMessage = TempData["PageMessage"];
            if (mode == null)
            {
                //if (string.IsNullOrEmpty(SiteIdentity.UserId))
                //    return View();//RedirectToAction("Login");
                if (id == null)
                {
                    if (!string.IsNullOrEmpty(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return View();
                }
                else
                {
                    var menu = new Menu();
                    var category = db.Categories.FirstOrDefault(i => i.CategoryId == id.Value && i.IsActive == true && i.IsDelete == false);
                    if (category != null)
                    {
                        menu.Id = id.Value;
                        menu.Value = category.Name;
                    }
                    if (!string.IsNullOrEmpty(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return View("SeeAll", menu);
                }
            }
            else
            {
                var men = new Menu();
                if (mode == "d" || mode == "f" || mode == "n" || mode == "c")
                {
                    men.Value = mode;
                }
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);
                else
                    return View("SeeAll", men);
            }


        }


        [HttpGet]
        public string ServeImage(string path, int width, int height)
        {
            string FullPath = System.Web.Hosting.HostingEnvironment.MapPath(path);
            byte[] ret = null;
            if (System.IO.File.Exists(FullPath))
                ret = ImgHelper.ResizeImage(FullPath, width, height, "white");
            else
            {
                FullPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Library/Uploads/WebThumb/NO_IMAGE.jpg");
                ret = ImgHelper.ResizeImage(FullPath, width, height, "white");
            }
            return Convert.ToBase64String(ret);
        }

        [NonAction]
        public void SaveIp()
        {
            string IP = Request.UserHostAddress;
            if (!string.IsNullOrEmpty(IP))
            {
                IP = IP.Trim();
                int id = 0;
                int.TryParse(SiteIdentity.UserId, out id);
                var visitor = db.VisitorLogs.FirstOrDefault(x => x.IPAddress == IP);
                if (visitor == null)
                {
                    visitor = new VisitorLog();
                    visitor.IPAddress = IP;
                    visitor.DateCreated = DateTime.UtcNow;
                }
                if (string.IsNullOrEmpty(visitor.CountryName) || string.IsNullOrEmpty(visitor.StateName) || string.IsNullOrEmpty(visitor.CityName))
                {
                    try
                    {
                        string url = "http://api.ipinfodb.com/v3/ip-city/?key={0}&ip={1}&format=json";
                        url = string.Format(url, ConfigurationManager.AppSettings["IPInfoKey"], IP);
                        using (var client = new WebClient())
                        {
                            var response = client.DownloadString(url);
                            if (!string.IsNullOrEmpty(response))
                            {
                                var data = JsonConvert.DeserializeObject<IPData>(response);
                                visitor.CountryName = data.countryName;
                                visitor.StateName = data.regionName;
                                visitor.CityName = data.cityName;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                if (visitor.VisitorId == 0)
                    db.VisitorLogs.Add(visitor);
                visitor.AccountId = id > 0 ? id : (int?)null;
                visitor.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

        [HttpGet]
        public ActionResult GetClothesByCategoryId(int id)
        {
            var list = db.Clothes.Where(i => (i.CategoryId == id || i.Category.ParentId == id) && i.IsActive == true && i.IsDelete == false).OrderBy(x => x.Clearance).
                ThenByDescending(x => x.DateChanged).ToList();
            ClothList retlist = new ClothList();
            bool loadMSRP = true;
            if (!string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                {
                    int Type = 0;
                    string type = SiteIdentity.Type;
                    int.TryParse(type, out Type);
                    loadMSRP = Type == (int)Platini.Models.CustomerType.Retail;
                }
                else
                    loadMSRP = false;
            }
            if (loadMSRP)
                retlist.List = list.Select(x => new ClothListItem { ClothesId = x.ClothesId, FutureDeliveryDate = x.FutureDeliveryDate, Price = x.MSRP, StyleNumber = x.StyleNumber, Clearance = x.Clearance.HasValue ? x.Clearance.Value : 2 }).ToList();
            else
                retlist.List.InjectFrom(list);
            foreach (var item in retlist.List)
            {
                var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
            }
            var category = db.Categories.Find(id);
            if (category.CategoryLevel == 2)
            {
                var SubCategory = db.Categories.Find(category.ParentId);
                SiteConfiguration.MainID = SubCategory.ParentId;
                SiteConfiguration.SubID = category.ParentId;
                SiteConfiguration.CatID = id;
            }
            else
            {
                SiteConfiguration.MainID = category.ParentId;
                SiteConfiguration.SubID = id;
                SiteConfiguration.CatID = 0;
            }
            return PartialView("ClothListViewPartial", retlist);
        }

        //[HttpGet]
        //public ActionResult GetClothesBySubCategoryId(int id)
        //{
        //    var list = db.Clothes.Where(i => (i.CategoryId == id || i.Category.ParentId == id) && i.IsActive == true).ToList();
        //    ClothList retlist = new ClothList();
        //    retlist.List.InjectFrom(list);
        //    foreach (var item in retlist.List)
        //    {
        //        var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
        //        item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
        //    }
        //    return PartialView("ClothListViewPartial", retlist);
        //}

        [HttpGet]
        public ActionResult GetFutureDeliveryDateClothes()
        {
            ViewBag.modevalue = "f";
            var tempDate = new DateTime(1900, 1, 1);
            var list = db.Clothes.Where(i => i.IsActive == true && i.IsDelete == false && i.FutureDeliveryDate.HasValue && i.FutureDeliveryDate > tempDate).OrderBy(x => x.FutureDeliveryDate).OrderBy(x => x.Clearance).
                ThenByDescending(x => x.DateChanged).ToList();
            ClothList retlist = new ClothList();
            bool loadMSRP = true;
            if (!string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                {
                    int Type = 0;
                    string type = SiteIdentity.Type;
                    int.TryParse(type, out Type);
                    loadMSRP = Type == (int)Platini.Models.CustomerType.Retail;
                }
                else
                    loadMSRP = false;
            }
            if (loadMSRP)
                retlist.List = list.Select(x => new ClothListItem { ClothesId = x.ClothesId, FutureDeliveryDate = x.FutureDeliveryDate, Price = x.MSRP, StyleNumber = x.StyleNumber, Clearance = x.Clearance.HasValue ? x.Clearance.Value : 2 }).ToList();
            else
                retlist.List.InjectFrom(list);
            foreach (var item in retlist.List)
            {
                var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
            }
            return PartialView("ClothListViewPartial", retlist);
        }

        [HttpGet]
        public ActionResult GetDeactivatedClothes()
        {
            ViewBag.modevalue = "d";
            var list = db.Clothes.Where(i => i.IsActive == false && i.IsDelete == false).OrderBy(x => x.Clearance).
                ThenByDescending(x => x.DateChanged).ThenBy(x => x.StyleNumber).ToList();
            ClothList retlist = new ClothList();
            bool loadMSRP = true;
            if (!string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                {
                    int Type = 0;
                    string type = SiteIdentity.Type;
                    int.TryParse(type, out Type);
                    loadMSRP = Type == (int)Platini.Models.CustomerType.Retail;
                }
                else
                    loadMSRP = false;
            }
            if (loadMSRP)
                retlist.List = list.Select(x => new ClothListItem { ClothesId = x.ClothesId, FutureDeliveryDate = x.FutureDeliveryDate, Price = x.MSRP, StyleNumber = x.StyleNumber, Clearance = x.Clearance.HasValue ? x.Clearance.Value : 2 }).ToList();
            else
                retlist.List.InjectFrom(list);
            foreach (var item in retlist.List)
            {
                var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
            }
            return PartialView("ClothListViewPartial", retlist);
        }

        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.PageMessage = TempData["PageMessage"];
            HttpCookie FindCookie = Request.Cookies["Platini"];
            if (FindCookie != null)
            {
                if (FindCookie.Values["cUserName"] != null && FindCookie.Values["cPassword"] != null)
                {
                    LoginModel model = new LoginModel();
                    model.UserName = FindCookie.Values["cUserName"].ToString();
                    model.Password = Cryptography.Decrypt(FindCookie.Values["cPassword"].ToString());
                    model.RememberMe = true;
                    model.isPartial = false;
                    return View(model);
                }
            }
            return View(new LoginModel() { isPartial = false });
        }

        [HttpPost]
        public ActionResult Login(LoginModel ret, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var chkUser = db.Accounts.Where(usr => usr.UserName == ret.UserName && usr.IsActive == true && usr.IsDelete == false).FirstOrDefault();
                if (chkUser != null)
                {
                    string dbPassword = Encoding.ASCII.GetString(chkUser.Password);
                    if (dbPassword == ret.Password)
                    {
                        SiteIdentity.UserId = "";
                        //SiteConfiguration.Clear();
                        chkUser.LastLoginDate = DateTime.UtcNow;
                        db.SaveChanges();
                        if (ret.RememberMe == true)
                        {
                            HttpCookie LoginCookie = new HttpCookie("Platini");
                            LoginCookie.Values.Add("cUserName", ret.UserName);
                            LoginCookie.Values.Add("cPassword", Cryptography.Encrypt(ret.Password));
                            LoginCookie.Expires = DateTime.UtcNow.AddDays(15);
                            Response.AppendCookie(LoginCookie);
                        }
                        else
                        {
                            HttpCookie LoginCookie = Request.Cookies["Platini"];
                            if (LoginCookie != null)
                            {
                                LoginCookie.Expires = DateTime.UtcNow.AddDays(-1);
                                Response.AppendCookie(LoginCookie);
                            }
                        }

                        string IsAdmin = "FALSE";
                        string Roles = ((RolesEnum)chkUser.RoleId).ToString();
                        if (Roles == RolesEnum.SuperAdmin.ToString() || Roles == RolesEnum.Admin.ToString())
                        {
                            IsAdmin = "TRUE";
                            SiteConfiguration.RemoveFromApp(HttpContext.ApplicationInstance.Application, new SelectedListValues() { Id = chkUser.AccountId, Value = Session.SessionID });
                            SiteConfiguration.CanView = true;
                            SiteConfiguration.CanEdit = true;
                            SiteConfiguration.CanOrder = true;
                        }
                        if (Roles == RolesEnum.SalesPerson.ToString() || Roles == RolesEnum.Warehouse.ToString())
                            SiteConfiguration.RemoveFromApp(HttpContext.ApplicationInstance.Application, new SelectedListValues() { Id = chkUser.AccountId, Value = Session.SessionID });
                        string Type = ((int)Platini.Models.CustomerType.Wholesale).ToString();
                        bool isPayment = false;
                        if (Roles == RolesEnum.Customer.ToString() || Roles == RolesEnum.User.ToString())
                        {

                            if (chkUser.CustomerOptionalInfoes.FirstOrDefault() != null)
                                Type = chkUser.CustomerOptionalInfoes.FirstOrDefault().CustomerType.ToString();
                            else
                                Type = ((int)Platini.Models.CustomerType.Retail).ToString();
                            SiteConfiguration.AddToApp(HttpContext.ApplicationInstance.Application, new SelectedListValues() { Id = chkUser.AccountId, Value = Session.SessionID });
                            SiteConfiguration.CanView = true;
                            SiteConfiguration.CanEdit = false;
                            SiteConfiguration.CanOrder = true;
                            var ordCookie = Request.Cookies["ordCookie"];
                            if (Type == ((int)Platini.Models.CustomerType.Retail).ToString() && ordCookie != null)
                            {
                                try
                                {
                                    var lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                                    CreateCookieOrder(lastOrder, chkUser);
                                    Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1);
                                    isPayment = true;
                                }
                                catch { Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1); }
                            }
                        }
                        string userDataString = string.Concat(chkUser.AccountId, "|", chkUser.FirstName + " " + chkUser.LastName, "|", chkUser.Email, "|", chkUser.UserName, "|", IsAdmin, "|", Roles, "|", Type);
                        HttpCookie authCookie = FormsAuthentication.GetAuthCookie(ret.UserName, ret.RememberMe);
                        FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);

                        FormsAuthenticationTicket newTicket = new FormsAuthenticationTicket(ticket.Version, ticket.Name, ticket.IssueDate, ticket.Expiration, ticket.IsPersistent, userDataString);
                        authCookie.Value = FormsAuthentication.Encrypt(newTicket);
                        Response.Cookies.Add(authCookie);
                        if (isPayment && !ret.isPartial)
                            return RedirectToAction("Billing");

                        string returnUrlLast = this.Request.QueryString["ReturnUrl"];

                        if (!string.IsNullOrEmpty(returnUrlLast) && returnUrl.Length > 1 && returnUrlLast.StartsWith("/")
                        && !returnUrlLast.StartsWith("//") && !returnUrlLast.StartsWith("/\\"))
                        {
                            return Redirect(returnUrl);
                        }
                        else if (Request.UrlReferrer != null && Request.UrlReferrer.ToString().Contains("?ReturnUrl"))
                        {
                            returnUrl = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["ReturnUrl"];
                            return Redirect(returnUrl);
                        }


                        if (Type == ((int)Platini.Models.CustomerType.Retail).ToString())
                        {
                            string retURL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/Index";
                            if (!string.IsNullOrEmpty(Request.UrlReferrer.AbsoluteUri))
                                if (!Request.UrlReferrer.AbsoluteUri.ToLower().Contains("login") && !Request.UrlReferrer.AbsoluteUri.ToLower().Contains("login"))
                                    retURL = Request.UrlReferrer.AbsoluteUri;
                            if (ret.isPartial)
                            {
                                ViewBag.PageMessage = "success|" + Request.UrlReferrer.AbsoluteUri;
                                ViewBag.ReturnUrl = returnUrl;
                                return PartialView("LoginPartial");
                            }
                            else
                                return Redirect(Request.UrlReferrer.AbsoluteUri);
                        }


                        if (ret.isPartial)
                        {
                            ViewBag.PageMessage = "success";
                            ViewBag.ReturnUrl = returnUrl;
                            return PartialView("LoginPartial");
                        }
                        else
                            return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.PageMessage = "Please enter a valid user name and password.";
                        if (ret.isPartial)
                            return PartialView("LoginPartial");
                        else
                            return View();
                    }
                }
                else
                {
                    ViewBag.PageMessage = "Please enter a valid user name and password.";
                    if (ret.isPartial)
                        return PartialView("LoginPartial");
                    else
                        return View();
                }
            }
            if (ret.isPartial)
            {
                ViewBag.ReturnUrl = returnUrl;
                return PartialView("LoginPartial");
            }
            else
                return View();
        }

        [NonAction]
        public void CreateCookieOrder(DB.Order lastOrder, DB.Account chkUser)
        {
            int StatusId = 0;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            var newOrder = db.Orders.Where(x => (x.AccountId == chkUser.AccountId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
            if (newOrder == null)
            {
                newOrder = new DB.Order();
                newOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.Ticks));
                newOrder.OrderNumber = SiteConfiguration.OrderNumber();
                newOrder.AccountId = chkUser.AccountId;
                var sp = chkUser.CustomerSalesPersons.FirstOrDefault();
                if (sp != null)
                    newOrder.EmployeeId = sp.SalesPersonId;
                if (!newOrder.EmployeeId.HasValue)
                    newOrder.EmployeeId = chkUser.AccountId;
                newOrder.CreatedOn = lastOrder.CreatedOn;
                newOrder.DateCreated = lastOrder.DateCreated;
                newOrder.StatusId = StatusId;
                //newOrder.ShippingCost = 7;
                newOrder.ShippingCost = 0;
                decimal disc = 0.0m;
                var ci = chkUser.CustomerOptionalInfoes.FirstOrDefault();
                if (ci != null)
                    disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                newOrder.Discount = disc;
                newOrder.IsDelete = false;
                newOrder.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                newOrder.IsSentToQuickBook = false;
                db.Orders.Add(newOrder);
            }
            newOrder.DateUpdated = lastOrder.DateUpdated;
            db.SaveChanges();
            Session["Order"] = newOrder.OrderId;
            Session.Remove("WasClearead");
            foreach (var size in lastOrder.OrderSizes)
            {
                var newSize = newOrder.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesSizeId);
                var cloth = db.Clothes.Find(size.ClothesId);
                var clothessize = db.ClothesScaleSizes.Find(size.ClothesSizeId);
                if (cloth != null && clothessize != null)
                {
                    bool created = false;
                    if (newSize == null)
                    {
                        newSize = new OrderSize();

                        newSize.ClothesId = size.ClothesId;
                        newSize.ClothesSizeId = size.ClothesSizeId;
                        newSize.Quantity = size.Quantity;
                        newSize.DateCreated = DateTime.UtcNow;
                        newSize.DateUpdated = DateTime.UtcNow;
                        newSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.Ticks));
                        newSize.OrderId = newOrder.OrderId;
                        created = true;
                    }
                    else
                        newSize.Quantity += size.Quantity;
                    newSize.DateUpdated = DateTime.UtcNow;
                    if (created)
                        db.OrderSizes.Add(newSize);
                    db.SaveChanges();
                }
            }
            newOrder.OriginalQty = 0;
            if (db.OrderScales.Where(x => x.OrderId == newOrder.OrderId).ToList().Any())
                newOrder.OriginalQty = db.OrderScales.Where(x => x.OrderId == newOrder.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
            if (db.OrderSizes.Where(x => x.OrderId == newOrder.OrderId).ToList().Any())
                newOrder.OriginalQty += db.OrderSizes.Where(x => x.OrderId == newOrder.OrderId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
            db.SaveChanges();

            newOrder.GrandTotal = CalcSum(newOrder.OrderId);
            newOrder.FinalAmount = newOrder.GrandTotal - (newOrder.GrandTotal * (newOrder.Discount / 100));
            newOrder.FinalAmount += (newOrder.ShippingCost.HasValue ? newOrder.ShippingCost.Value : 0);
            db.SaveChanges();
        }

        [HttpPost]
        public ActionResult FacebookLogin(FacebookUser retModel)
        {
            if (ModelState.IsValid)
            {
                SiteIdentity.UserId = "";
                var user = db.Accounts.FirstOrDefault(x => x.Email == retModel.email);
                if (user == null)
                {
                    user = new DB.Account()
                    {
                        FirstName = retModel.first_name,
                        LastName = retModel.last_name,
                        Email = retModel.email,
                        UserName = retModel.email,
                        Password = Encoding.ASCII.GetBytes("platini"),
                        IsActive = true,
                        IsDelete = false,
                        RoleId = (int)RolesEnum.Customer,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow,
                    };
                    db.Accounts.Add(user);
                    db.SaveChanges();
                    if (retModel.location != null)
                    {
                        var address = new DB.Address()
                        {
                            AccountId = user.AccountId,
                            City = retModel.location.location.city,
                            Country = retModel.location.location.country,
                            AddressTypeId = (int)AddressTypeEnum.BillingAddress,
                            IsActive = true,
                            IsDelete = false,
                            DateCreated = DateTime.UtcNow,
                            DateUpdated = DateTime.UtcNow
                        };
                        db.Addresses.Add(address);
                        db.SaveChanges();
                    }
                    if (retModel.isWholeSale)
                    {
                        var cusInfo = new DB.CustomerOptionalInfo()
                        {
                            AccountId = user.AccountId,
                            CustomerType = (int)Models.CustomerType.Wholesale,
                            DateCreated = DateTime.UtcNow,
                            DateUpdated = DateTime.UtcNow
                        };
                        db.CustomerOptionalInfoes.Add(cusInfo);
                        db.SaveChanges();
                    }
                    TempData["PageMessage"] = "Thank you for registering.";
                    try
                    {
                        bool check = EmailManager.SendWelcomeEmail(retModel.email, "", retModel.email, "platini");
                        check = EmailManager.SendAdminEmail("", retModel.first_name + " " + retModel.last_name, "", retModel.email, user.AccountId.ToString(), "");
                    }
                    catch { }
                }
                user.FacebookUserId = retModel.id;
                user.LastLoginDate = DateTime.UtcNow;
                db.SaveChanges();
                string IsAdmin = "FALSE";
                string Roles = RolesEnum.Customer.ToString();
                string Type = ((int)Platini.Models.CustomerType.Retail).ToString();
                if (user.CustomerOptionalInfoes.FirstOrDefault() != null)
                    Type = user.CustomerOptionalInfoes.FirstOrDefault().CustomerType.ToString();
                SiteConfiguration.AddToApp(HttpContext.ApplicationInstance.Application, new SelectedListValues() { Id = user.AccountId, Value = Session.SessionID });
                SiteConfiguration.CanView = true;
                SiteConfiguration.CanEdit = false;
                SiteConfiguration.CanOrder = true;
                string userDataString = string.Concat(user.AccountId, "|", user.FirstName + " " + user.LastName, "|", user.Email, "|", user.UserName, "|", IsAdmin, "|", Roles, "|", Type);
                HttpCookie authCookie = FormsAuthentication.GetAuthCookie(user.UserName, true);
                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
                FormsAuthenticationTicket newTicket = new FormsAuthenticationTicket(ticket.Version, ticket.Name, ticket.IssueDate, ticket.Expiration, ticket.IsPersistent, userDataString);
                authCookie.Value = FormsAuthentication.Encrypt(newTicket);
                Response.Cookies.Add(authCookie);
                bool isPayment = false;
                var ordCookie = Request.Cookies["ordCookie"];
                try
                {
                    var lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                    CreateCookieOrder(lastOrder, user);
                    Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1);
                    isPayment = true;
                }
                catch { Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1); }
                return Json(new { isBill = isPayment, msg = "Success" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { isBill = false, msg = "Success" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GoogleLogin(GoogleUser retModel)
        {
            if (ModelState.IsValid)
            {
                SiteIdentity.UserId = "";
                var user = db.Accounts.FirstOrDefault(x => x.Email == retModel.email);
                if (user == null)
                {
                    user = new DB.Account()
                    {
                        FirstName = retModel.Name.Split(' ').FirstOrDefault(),
                        LastName = retModel.Name.Split(' ').Count() > 1 ? retModel.Name.Split(' ')[1] : "",
                        Email = retModel.email,
                        UserName = retModel.email,
                        Password = Encoding.ASCII.GetBytes("platini"),
                        IsActive = true,
                        IsDelete = false,
                        RoleId = (int)RolesEnum.Customer,
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow,
                    };
                    db.Accounts.Add(user);
                    db.SaveChanges();
                    if (retModel.isWholeSale)
                    {
                        var cusInfo = new DB.CustomerOptionalInfo()
                        {
                            AccountId = user.AccountId,
                            CustomerType = (int)Models.CustomerType.Wholesale,
                            DateCreated = DateTime.UtcNow,
                            DateUpdated = DateTime.UtcNow
                        };
                        db.CustomerOptionalInfoes.Add(cusInfo);
                        db.SaveChanges();
                    }
                    TempData["PageMessage"] = "Thank you for registering.";
                    try
                    {
                        bool check = EmailManager.SendWelcomeEmail(retModel.email, "", retModel.email, "platini");
                        check = EmailManager.SendAdminEmail("", retModel.Name, "", retModel.email, user.AccountId.ToString(), "");
                    }
                    catch { }
                }
                user.GoogleId = retModel.id;
                user.LastLoginDate = DateTime.UtcNow;
                db.SaveChanges();
                string IsAdmin = "FALSE";
                string Roles = RolesEnum.Customer.ToString();
                string Type = ((int)Platini.Models.CustomerType.Retail).ToString();
                if (user.CustomerOptionalInfoes.FirstOrDefault() != null)
                    Type = user.CustomerOptionalInfoes.FirstOrDefault().CustomerType.ToString();
                SiteConfiguration.AddToApp(HttpContext.ApplicationInstance.Application, new SelectedListValues() { Id = user.AccountId, Value = Session.SessionID });
                SiteConfiguration.CanView = true;
                SiteConfiguration.CanEdit = false;
                SiteConfiguration.CanOrder = true;
                string userDataString = string.Concat(user.AccountId, "|", user.FirstName + " " + user.LastName, "|", user.Email, "|", user.UserName, "|", IsAdmin, "|", Roles, "|", Type);
                //string userDataString = string.Concat(user.AccountId, "|", user.FirstName + " " + user.LastName, "|", user.Email, "|", IsAdmin, "|", Roles, "|", Type);
                HttpCookie authCookie = FormsAuthentication.GetAuthCookie(user.UserName, true);
                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
                FormsAuthenticationTicket newTicket = new FormsAuthenticationTicket(ticket.Version, ticket.Name, ticket.IssueDate, ticket.Expiration, ticket.IsPersistent, userDataString);
                authCookie.Value = FormsAuthentication.Encrypt(newTicket);
                Response.Cookies.Add(authCookie);
                bool isPayment = false;
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        var lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                        CreateCookieOrder(lastOrder, user);
                        Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1);
                        isPayment = true;
                    }
                    catch { Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1); }
                }
                return Json(new { isBill = isPayment, msg = "Success" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { isBill = false, msg = "Success" }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult Registration(string from)
        {
            ViewBag.PageMessage = TempData["PageMessage"];
            var model = new RegistrationModel() { forceWholesale = false };
            if (!string.IsNullOrEmpty(from))
                return PartialView("WholesaleRegistration", model);
            else
                return View(model);
        }

        public ActionResult WholesaleRegistration(string from)
        {
            ViewBag.PageMessage = TempData["PageMessage"];
            var model = new RegistrationModel() { forceWholesale = true };

            if (!string.IsNullOrEmpty(from))
                return PartialView(model);
            else
                return View("Registration", model);
        }

        [HttpPost]
        public ActionResult Registration2608(RegistrationModel ret)
        {
            if (ModelState.IsValid)
            {
                var verifyUser = db.Accounts.Where(a => (a.UserName == ret.UserName) && a.IsDelete == false).FirstOrDefault();
                if (verifyUser == null)
                {
                    Platini.DB.Account account = new Platini.DB.Account();
                    account.UserName = ret.UserName;
                    account.Password = System.Text.Encoding.ASCII.GetBytes(ret.Password);
                    account.FirstName = ret.FirstName;
                    account.LastName = ret.LastName;
                    account.Email = ret.Email;
                    account.RoleId = (int)RolesEnum.Customer;
                    account.IsActive = false;
                    account.IsDelete = false;
                    account.DateCreated = DateTime.UtcNow;
                    account.DateUpdated = DateTime.UtcNow;
                    db.Accounts.Add(account);
                    db.SaveChanges();
                    string cName;
                    if (ret.CompanyName != null)
                    {
                        cName = SaveUniqueCompany(ret.CompanyName, ret.City, account.AccountId);
                    }
                    else
                    {
                        cName = SaveUniqueCompany(ret.FirstName + " " + ret.LastName, ret.City, account.AccountId);
                    }

                    if (!string.IsNullOrEmpty(ret.City))
                    {
                        DB.Address dbAddress = new DB.Address();
                        dbAddress.AccountId = account.AccountId;
                        dbAddress.City = ret.City;
                        dbAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
                        dbAddress.IsActive = true;
                        dbAddress.IsDelete = false;
                        dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
                        db.Addresses.Add(dbAddress);
                        db.SaveChanges();
                    }

                    if (!string.IsNullOrEmpty(ret.PhoneNumber))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (char c in ret.PhoneNumber)
                        {
                            if ((c >= '0' && c <= '9'))
                                sb.Append(c);
                        }
                        ret.PhoneNumber = sb.ToString();
                        if (ret.CountryCode.Length > ret.PhoneNumber.Length)
                        {
                            int len = ret.CountryCode.Length - ret.PhoneNumber.Length;
                            ret.CountryCode = ret.CountryCode.Insert(len, "-");
                            ret.PhoneNumber = ret.CountryCode;
                        }
                        Communication communication = new Communication();
                        communication.AccountId = account.AccountId;
                        communication.Phone = ret.PhoneNumber;
                        communication.IsActive = true;
                        communication.IsDelete = false;
                        communication.DateCreated = communication.DateUpdated = DateTime.UtcNow;
                        db.Communications.Add(communication);
                        db.SaveChanges();
                    }

                    if (!string.IsNullOrEmpty(ret.ResellerNumber) || ret.forceWholesale)
                    {
                        CustomerOptionalInfo coi = new CustomerOptionalInfo();
                        coi.AccountId = account.AccountId;
                        coi.BusinessReseller = ret.ResellerNumber;
                        coi.CustomerType = (int)Platini.Models.CustomerType.Wholesale;
                        coi.DisplayName = !string.IsNullOrEmpty(ret.CompanyName) ? cName : ret.FirstName + " " + ret.LastName;
                        coi.DateCreated = coi.DateUpdated = DateTime.UtcNow;
                        db.CustomerOptionalInfoes.Add(coi);
                        db.SaveChanges();
                        TempData["PageMessage"] = "Your account has now been successfully reigstered.  Please check your email for confirmation and the next steps.";
                        try
                        {
                            //var token = WebSecurity.GeneratePasswordResetToken(UserName);
                            bool check = EmailManager.SendWelcomeEmail(ret.Email, !string.IsNullOrEmpty(ret.CompanyName) ? ret.CompanyName : ret.UserName);
                            check = EmailManager.SendAdminEmail(cName, ret.FirstName + " " + ret.LastName, ret.PhoneNumber, ret.Email, account.AccountId.ToString(), ret.City);
                        }
                        catch { }
                    }
                    else
                    {
                        CustomerOptionalInfo coi = new CustomerOptionalInfo();
                        coi.AccountId = account.AccountId;
                        coi.DisplayName = !string.IsNullOrEmpty(ret.CompanyName) ? cName : ret.FirstName + " " + ret.LastName;
                        coi.DateCreated = coi.DateUpdated = DateTime.UtcNow;
                        coi.CustomerType = (int)Platini.Models.CustomerType.Retail;
                        db.CustomerOptionalInfoes.Add(coi);
                        account.IsActive = true;
                        db.SaveChanges();
                        TempData["PageMessage"] = "Thank you for registering.  You can now login to the site.";
                    }

                    string returnUrl = this.Request.QueryString["ReturnUrl"];
                    if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                       && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        //return Redirect(returnUrl);
                        return RedirectToAction("Login", new { ReturnUrl = returnUrl });
                    }
                    else if (Request.UrlReferrer != null && Request.UrlReferrer.ToString().Contains("?ReturnUrl"))
                    {
                        returnUrl = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["ReturnUrl"];
                        //return Redirect(returnUrl);
                        return RedirectToAction("Login", new { ReturnUrl = returnUrl });
                    }
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.PageMessage = string.Format("The user name {0} is already taken.", ret.UserName);
                }
            }
            return View("Registration");
        }

        [HttpPost]
        public ActionResult RegistrationPost(RegistrationModel ret)
        {
            if (!ret.forceWholesale)
                ModelState.Remove("UserName");
            else
                ModelState.Remove("cPassword");
            ModelState.Remove("Email");
            if (ModelState.IsValid)
            {
                var verifyUser = db.Accounts.Where(a => (a.UserName == (!ret.forceWholesale ? ret.Email : ret.UserName)) && a.IsDelete == false).FirstOrDefault();
                if (verifyUser == null)
                {
                    Platini.DB.Account account = new Platini.DB.Account();
                    account.UserName = !ret.forceWholesale ? ret.Email : ret.UserName;
                    account.Password = System.Text.Encoding.ASCII.GetBytes(ret.Password);
                    account.FirstName = ret.FirstName;
                    account.LastName = ret.LastName;
                    account.Email = !string.IsNullOrEmpty(ret.Email) ? ret.Email : string.Empty;
                    account.RoleId = (int)RolesEnum.Customer;
                    account.IsActive = false;
                    account.IsDelete = false;
                    account.DateCreated = DateTime.UtcNow;
                    account.DateUpdated = DateTime.UtcNow;
                    db.Accounts.Add(account);
                    db.SaveChanges();
                    string cName;
                    if (ret.CompanyName != null)
                    {
                        cName = SaveUniqueCompany(ret.CompanyName, ret.City, account.AccountId);
                    }
                    else
                    {
                        cName = SaveUniqueCompany(ret.FirstName + " " + ret.LastName, ret.City, account.AccountId);
                    }

                    if (!string.IsNullOrEmpty(ret.City))
                    {
                        DB.Address dbAddress = new DB.Address();
                        dbAddress.AccountId = account.AccountId;
                        dbAddress.City = ret.City;
                        dbAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
                        dbAddress.IsActive = true;
                        dbAddress.IsDelete = false;
                        dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
                        db.Addresses.Add(dbAddress);
                        db.SaveChanges();
                    }

                    if (!string.IsNullOrEmpty(ret.PhoneNumber))
                    {
                        Communication communication = new Communication();
                        communication.AccountId = account.AccountId;
                        //communication.Phone = ret.PhoneNumber;
                        StringBuilder sb = new StringBuilder();
                        foreach (char c in ret.PhoneNumber)
                        {
                            if ((c >= '0' && c <= '9'))
                                sb.Append(c);
                        }
                        ret.PhoneNumber = sb.ToString();
                        if (ret.CountryCode != null)
                        {
                            if (ret.CountryCode.Length > ret.PhoneNumber.Length)
                            {
                                int len = ret.CountryCode.Length - ret.PhoneNumber.Length;
                                ret.CountryCode = ret.CountryCode.Insert(len, "-");
                                ret.PhoneNumber = ret.CountryCode;
                            }
                        }
                        communication.Phone = ret.PhoneNumber;
                        communication.IsActive = true;
                        communication.IsDelete = false;
                        communication.DateCreated = communication.DateUpdated = DateTime.UtcNow;
                        db.Communications.Add(communication);
                        db.SaveChanges();
                    }

                    if (!string.IsNullOrEmpty(ret.ResellerNumber) || ret.forceWholesale)
                    {
                        CustomerOptionalInfo coi = new CustomerOptionalInfo();
                        coi.AccountId = account.AccountId;
                        coi.BusinessReseller = ret.ResellerNumber;
                        coi.CustomerType = (int)Platini.Models.CustomerType.Wholesale;
                        coi.DisplayName = !string.IsNullOrEmpty(ret.CompanyName) ? cName : ret.FirstName + " " + ret.LastName;
                        coi.DateCreated = coi.DateUpdated = DateTime.UtcNow;
                        db.CustomerOptionalInfoes.Add(coi);
                        db.SaveChanges();
                        if (QuickBookStrings.UseQuickBook())
                            AddCustomerToQuickBook(account.AccountId);
                        TempData["PageMessage"] = "Your account has now been successfully reigstered.  Please check your email for confirmation and the next steps.";
                        try
                        {
                            bool check = EmailManager.SendWelcomeEmail(ret.Email, !string.IsNullOrEmpty(ret.CompanyName) ? ret.CompanyName : ret.UserName);
                            check = EmailManager.SendAdminEmail(cName, ret.FirstName + " " + ret.LastName, ret.PhoneNumber, ret.Email, account.AccountId.ToString(), ret.City);
                        }
                        catch { }
                    }
                    else
                    {
                        CustomerOptionalInfo coi = new CustomerOptionalInfo();
                        coi.AccountId = account.AccountId;
                        coi.DisplayName = !string.IsNullOrEmpty(ret.CompanyName) ? cName : ret.FirstName + " " + ret.LastName;
                        coi.DateCreated = coi.DateUpdated = DateTime.UtcNow;
                        coi.CustomerType = (int)Platini.Models.CustomerType.Retail;
                        db.CustomerOptionalInfoes.Add(coi);
                        account.IsActive = true;
                        db.SaveChanges();
                        string IsAdmin = "FALSE";
                        string Roles = RolesEnum.Customer.ToString();
                        string Type = ((int)Platini.Models.CustomerType.Retail).ToString();

                        SiteConfiguration.AddToApp(HttpContext.ApplicationInstance.Application, new SelectedListValues() { Id = account.AccountId, Value = Session.SessionID });
                        SiteConfiguration.CanView = true;
                        SiteConfiguration.CanEdit = false;
                        SiteConfiguration.CanOrder = true;
                        string userDataString = string.Concat(account.AccountId, "|", account.FirstName + " " + account.LastName, "|", account.Email, "|", account.UserName, "|", IsAdmin, "|", Roles, "|", Type);
                        HttpCookie authCookie = FormsAuthentication.GetAuthCookie(account.UserName, false);
                        FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
                        FormsAuthenticationTicket newTicket = new FormsAuthenticationTicket(ticket.Version, ticket.Name, ticket.IssueDate, ticket.Expiration, ticket.IsPersistent, userDataString);
                        authCookie.Value = FormsAuthentication.Encrypt(newTicket);
                        Response.Cookies.Add(authCookie);
                        if (QuickBookStrings.UseQuickBook())
                            AddCustomerToQuickBook(account.AccountId);
                        TempData["PageMessage"] = "Thank you for registering.";
                        var ordCookie = Request.Cookies["ordCookie"];
                        if (ordCookie == null)
                        {
                            if (!string.IsNullOrEmpty(Request.UrlReferrer.AbsoluteUri))
                            {
                                if (!Request.UrlReferrer.AbsoluteUri.ToLower().Contains("login") && !Request.UrlReferrer.AbsoluteUri.ToLower().Contains("login"))
                                    return Redirect(Request.ApplicationPath);
                                else
                                    return RedirectToAction("Index");

                                //return Redirect(Request.UrlReferrer.AbsoluteUri);
                            }
                        }
                        else
                        {
                            try
                            {
                                var lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                                CreateCookieOrder(lastOrder, account);
                                Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1);
                                return RedirectToAction("Billing");
                            }
                            catch { }
                        }
                    }

                    string returnUrl = this.Request.QueryString["ReturnUrl"];
                    if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                       && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        //return Redirect(returnUrl);
                        return RedirectToAction("Login", new { ReturnUrl = returnUrl });
                    }
                    else if (Request.UrlReferrer != null && Request.UrlReferrer.ToString().Contains("?ReturnUrl"))
                    {
                        returnUrl = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["ReturnUrl"];
                        //return Redirect(returnUrl);
                        return RedirectToAction("Login", new { ReturnUrl = returnUrl });
                    }
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.PageMessage = string.Format("The user name {0} is already taken.", ret.UserName);
                }
            }
            return View("Registration", ret);
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

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            SiteConfiguration.Clear();
            SiteIdentity.UserId = "";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult CheckUser(string Username)
        {
            string strStatus = "";
            string strMessage = "";
            var verifyUser = db.Accounts.Where(a => (a.UserName == Username) && a.IsDelete == false).FirstOrDefault();
            if (verifyUser != null)
            {
                strStatus = "Fail";
                strMessage = string.Format("The user name {0} is already taken.  Please enter another and try again", Username);
            }
            else
            {
                strStatus = "Success";
                strMessage = "The username you have selected is available!";
            }

            return Json(new { status = strStatus, message = strMessage });
        }


        public string Sync()
        {
            string strUrl = Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath;
            //HttpWebRequest tRequest = (HttpWebRequest)WebRequest.Create("https://www.platinijeans.com/API/Sync/InitialSync");
            HttpWebRequest tRequest = (HttpWebRequest)WebRequest.Create(strUrl + "API/Sync/InitialSync");
            tRequest.Method = "POST";
            tRequest.ContentType = "application/json";
            string postData = new JavaScriptSerializer().Serialize(new
            {
                UserName = "Platini1"
            });

            byte[] bytes = Encoding.UTF8.GetBytes(postData);
            tRequest.ContentLength = bytes.Length;
            tRequest.KeepAlive = true;
            Stream requestStream = tRequest.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);

            HttpWebResponse tResponse = (HttpWebResponse)tRequest.GetResponse();
            Stream dataStream = tResponse.GetResponseStream();
            StreamReader tReader = new StreamReader(dataStream);
            string sResponseFromServer = tReader.ReadToEnd();
            System.IO.File.WriteAllText(Server.MapPath("~/Template/InitialSync.txt"), sResponseFromServer);
            //Response.Clear();
            //Response.ClearHeaders();
            //Response.AddHeader("Content-Length", sResponseFromServer.Length.ToString());
            //Response.ContentType = "text/plain";
            //Response.AppendHeader("content-disposition", "attachment;filename=\"InitialSync.txt\"");
            //Response.Write(sResponseFromServer);
            //Response.End();
            string strText = string.Format("Success. Check File - <a href='{0}Template/InitialSync.txt'>Download File</a>", strUrl);
            return strText;
        }

        public ActionResult Test()
        {
            //var request = (HttpWebRequest)WebRequest.Create(Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath + "API/Sync/FullSync");
            var request = (HttpWebRequest)WebRequest.Create("http://www.elanvitalshirts.com/API/Sync/FullSync");
            //string poststring = System.IO.File.ReadAllText(Server.MapPath("~/SyncCheck.txt"));
            //var postData = Encoding.ASCII.GetBytes(poststring);
            var Wrapper = new RequestWrapper();
            Wrapper.DeviceInfo = "Build version - Version : 0.2.5,Device Type - iPhone Simulator,Device Name - iPhone Simulator,System Version - 9.2,User ID - 2";
            Wrapper.Mode = 0;
            Wrapper.SubMode = 1;
            Wrapper.SyncDate = null;
            Wrapper.UserId = 2;
            string poststring = JsonConvert.SerializeObject(Wrapper);
            var postData = Encoding.ASCII.GetBytes(poststring);
            request.Method = "POST";
            request.ContentLength = postData.Length;
            request.ContentType = "application/json";
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }
            try
            {
                var response = request.GetResponse();
                string responseString = "";
                using (var responseStream = new StreamReader(response.GetResponseStream()))
                {
                    responseString = responseStream.ReadToEnd();
                }
                System.IO.File.WriteAllText(Server.MapPath("~/Template/FullSync.txt"), responseString);
                //ViewBag.Response = responseString;
                //var wrapper = JsonConvert.DeserializeObject<PrintCart>(responseString);
                //byte[] sPDFDecoded = Convert.FromBase64String(wrapper.PdfString);
                //System.IO.File.WriteAllBytes(@"d:\pdf7.pdf", sPDFDecoded);
                //System.IO.File.AppendAllText(Server.MapPath("~/Template/Synccheck.txt"), responseString);
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                }
            }
            //var wrapper = JsonConvert.DeserializeObject<TestSync>(responseString);
            //return responseString;
            return View();
        }

        public ActionResult GetBackGrounds()
        {
            var bPics = db.BackgroundPictures.Where(x => !string.IsNullOrEmpty(x.Picture) && x.IsActive == true && x.IsDelete == false).ToList().OrderBy(x => x.SortOrder).Select(x => new { image = "/Library/Backgrounds/" + x.Picture, url = string.Empty });
            return Json(bPics, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpdateMode(string Type)
        {
            if (!string.IsNullOrEmpty(Type))
            {
                if (ModeEnum.View.ToString().ToLower() == Type.ToLower() || ModeEnum.Order.ToString().ToLower() == Type.ToLower())
                {
                    if (SiteConfiguration.Mode.ToLower() != Type.ToLower())
                    {
                        string Mode = Type;
                        SiteConfiguration.Mode = Type;
                    }
                    if (SiteConfiguration.Mode.ToLower() == ModeEnum.Order.ToString().ToLower())
                    {

                        int UserId = 0;
                        int StatusId = 0;
                        List<string> Errors = new List<string>();
                        DB.Order lastOrder;
                        var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
                        if (status != null)
                            StatusId = status.OrderStatusId;
                        if (Session["Order"] == null)
                        {
                            if (Session["EditingOrder"] == null)
                            {
                                int.TryParse(SiteIdentity.UserId, out UserId);
                                lastOrder = db.Orders.Where(x => (x.AccountId == UserId || x.EmployeeId == UserId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
                                if (lastOrder != null)
                                    Session["Order"] = lastOrder.OrderId;
                            }
                            else
                                Session["Order"] = Session["EditingOrder"];
                        }
                    }
                    return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ConfirmPass(string pwd)
        {
            if (!string.IsNullOrEmpty(pwd))
            {

                int Id = 0;
                int.TryParse(SiteIdentity.UserId, out Id);
                var user = db.Accounts.FirstOrDefault(x => x.AccountId == Id && x.IsActive == true && x.IsDelete == false);
                if (user != null)
                {
                    string pass = Encoding.ASCII.GetString(user.Password);
                    if (pass == pwd)
                        SiteConfiguration.Mode = ModeEnum.Edit.ToString();
                    else
                        TempData["PageMessage"] = "Password did not match";

                }
                else
                {
                    TempData["PageMessage"] = "Please login";
                    return RedirectToAction("Login");
                }
            }
            else
                TempData["PageMessage"] = "Please enter your password";
            if (!string.IsNullOrEmpty(Request.UrlReferrer.AbsoluteUri))
                return Redirect(Request.UrlReferrer.AbsoluteUri);
            return RedirectToAction("Index");
        }


        public ActionResult Detail(string id, string c)
        {
            var model = new DetailViewClass();

            model.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            model.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            var cloth = db.Clothes.FirstOrDefault(x => x.StyleNumber == id);
            if (cloth == null)
            {
                int clothId = 0;
                int.TryParse(id, out clothId);
                cloth = db.Clothes.FirstOrDefault(x => x.ClothesId == clothId);
            }
            if (cloth != null)
            {

                model.ClothesId = cloth.ClothesId;
                model.StyleNumber = cloth.StyleNumber;
                model.Color = cloth.Color;
                model.MSRP = cloth.MSRP.HasValue ? cloth.MSRP.Value : 0.0m;
                model.Price = cloth.Price.HasValue ? cloth.Price.Value : 0.0m;
                model.DiscountedMSRP = cloth.DiscountedMSRP.HasValue ? cloth.DiscountedMSRP.Value : 0.0m;
                model.DiscountedPrice = cloth.DiscountedPrice.HasValue ? cloth.DiscountedPrice.Value : 0.0m;
                model.Cost = cloth.ProductCost.HasValue ? cloth.ProductCost.Value : 0.0m;
                model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                model.CategoryId = cloth.CategoryId;
                model.Clearance = cloth.Clearance.HasValue ? cloth.Clearance.Value : 0;
                model.Description = cloth.ClothesDescription;
                model.Note = cloth.AdminNote;
                if (cloth.Clearance == 1)
                {
                    ViewBag.modevalue = "c";
                }
                else
                {
                    ViewBag.modevalue = "";
                }

                var sizeGroupId = cloth.SizeGroupId;
                model.isFuture = cloth.FutureDeliveryDate.HasValue ? (cloth.FutureDeliveryDate.Value != DateTime.MinValue) : false;             
                bool loadMSRP = true;
                if (!string.IsNullOrEmpty(SiteIdentity.UserId))
                {
                    if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                    {
                        int Type = 0;
                        string type = SiteIdentity.Type;
                        int.TryParse(type, out Type);
                        loadMSRP = Type == (int)Platini.Models.CustomerType.Retail;
                    }
                    else
                        loadMSRP = false;
                }
                var Category = db.Categories.Find(cloth.CategoryId);
                var SubCategory = db.Categories.Find(Category.ParentId);
                SiteConfiguration.MainID = SubCategory.ParentId;
                SiteConfiguration.SubID = Category.ParentId;
                SiteConfiguration.CatID = cloth.CategoryId;
                var prodList1 = db.RelatedClothes.Where(x => x.ClothesId == model.ClothesId && x.IsActive == true && x.IsDelete == false).Select(x => x.RelClothesId);
                var prodList2 = db.RelatedClothes.Where(x => x.RelClothesId == model.ClothesId && x.IsActive == true && x.IsDelete == false).Select(x => x.ClothesId);
                var prodList = new List<int>();
                prodList.AddRange(prodList1);
                prodList.AddRange(prodList2);
                prodList = prodList.FindAll(x => x != model.ClothesId).Distinct().ToList();
                model.RelatedProducts = db.Clothes.Where(x => prodList.Contains(x.ClothesId) && x.IsActive == true && x.IsDelete == false).Select(y => new RelatedProductsItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = loadMSRP ? y.MSRP : y.Price }).ToList();
                prodList1 = db.RelatedColors.Where(x => x.ClothesId == model.ClothesId && x.IsActive == true && x.IsDelete == false).Select(x => x.RelClothesId);
                prodList2 = db.RelatedColors.Where(x => x.RelClothesId == model.ClothesId && x.IsActive == true && x.IsDelete == false).Select(x => x.ClothesId);
                prodList = new List<int>();
                prodList.AddRange(prodList1);
                prodList.AddRange(prodList2);
                prodList = prodList.FindAll(x => x != model.ClothesId).Distinct().ToList();
                model.RelatedColors = db.Clothes.Where(x => prodList.Contains(x.ClothesId) && x.IsActive == true && x.IsDelete == false).Select(y => new RelatedProductsItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = loadMSRP ? y.MSRP : y.Price }).ToList();
                List<RelatedProductsItem> masterList = null;
                var tempDate = new DateTime(1900, 1, 1);
                if (!string.IsNullOrEmpty(c))
                    masterList = db.Clothes.Where(x => x.CategoryId == model.CategoryId && x.IsActive == true && x.IsDelete == false && (!x.FutureDeliveryDate.HasValue || x.FutureDeliveryDate == DateTime.MinValue || x.FutureDeliveryDate == tempDate)).OrderBy(x => x.Clearance).ThenByDescending(x => x.DateChanged).Select(y => new RelatedProductsItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = y.Price }).ToList();
                else
                {
                    var cIds = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == SubCategory.CategoryId).ToList().Select(x => x.CategoryId);
                    masterList = db.Clothes.Where(x => cIds.Contains(x.CategoryId) && x.IsActive == true && x.IsDelete == false && (!x.FutureDeliveryDate.HasValue || x.FutureDeliveryDate == DateTime.MinValue || x.FutureDeliveryDate == tempDate)).OrderBy(x => x.Clearance).ThenByDescending(x => x.DateChanged).Select(y => new RelatedProductsItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = y.Price }).ToList();
                }
                var index = masterList.IndexOf(masterList.FirstOrDefault(x => x.ClothesId == model.ClothesId));
                var list1 = masterList.Take(index).ToList();
                var list2 = masterList.Skip(index + 1).ToList();
                model.MoreProducts.AddRange(list2);
                model.MoreProducts.AddRange(list1);
                ViewBag.PageMessage = TempData["PageMessage"];
                if (model.MSRP == 0.0m)
                {
                    ViewBag.PageMessage = "This product has not been priced yet.";
                }
                return View(model);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult BreakPack(int id)
        {
            if (id > 0)
            {
                var pack = db.ClothesScales.Find(id);
                if (pack != null)
                {
                    if (pack.InvQty <= 0)
                        return Json(null, JsonRequestBehavior.AllowGet);

                    if (pack.IsActive == true && pack.IsDelete == false && pack.IsOpenSize == false)
                    {
                        bool checkFit = pack.FitId.HasValue;
                        bool checkInseam = pack.InseamId.HasValue;
                        var open = db.ClothesScales.Where(x => x.ClothesId == pack.ClothesId && (checkInseam ? x.InseamId == pack.InseamId : true) && (checkFit ? x.FitId == pack.FitId : true) && x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                        if (open == null)
                        {
                            open = new ClothesScale();
                            open.ClothesId = pack.ClothesId;
                            open.FitId = pack.FitId;
                            open.InseamId = pack.InseamId;
                            open.InvQty = 0;
                            open.IsOpenSize = true;
                            open.Name = null;
                            open.IsActive = true;
                            open.IsDelete = false;
                            open.DateCreated = open.DateUpdated = DateTime.UtcNow;
                            db.ClothesScales.Add(open);
                            db.SaveChanges();
                        }
                        foreach (var size in pack.ClothesScaleSizes)
                        {
                            var openSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == open.ClothesScaleId && x.SizeId == size.SizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                            if (openSize == null)
                            {
                                openSize = new ClothesScaleSize();
                                openSize.ClothesScaleId = open.ClothesScaleId;
                                openSize.SizeId = size.SizeId;
                                openSize.Quantity = size.Quantity;
                                openSize.IsActive = true;
                                openSize.IsDelete = false;
                                openSize.DateCreated = openSize.DateUpdated = DateTime.UtcNow;
                                db.ClothesScaleSizes.Add(openSize);
                            }
                            else
                            {
                                openSize.Quantity = openSize.Quantity + size.Quantity;
                                openSize.DateUpdated = DateTime.UtcNow;
                            }
                            db.SaveChanges();
                        }
                        pack.InvQty -= 1;
                        pack.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                }
                return Json("Pack not found", JsonRequestBehavior.AllowGet);
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult BreakPackCart(int id, Guid OrderId)
        {
            if (id > 0)
            {
                var order = db.Orders.Find(OrderId);
                var pack = db.ClothesScales.Find(id);
                if (pack != null)
                {
                    if (pack.InvQty <= 0)
                        return Json(null, JsonRequestBehavior.AllowGet);

                    if (pack.IsActive == true && pack.IsDelete == false && pack.IsOpenSize == false)
                    {
                        bool checkFit = pack.FitId.HasValue;
                        bool checkInseam = pack.InseamId.HasValue;
                        var open = db.ClothesScales.Where(x => x.ClothesId == pack.ClothesId && (checkInseam ? x.InseamId == pack.InseamId : true) && (checkFit ? x.FitId == pack.FitId : true) && x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                        if (open == null)
                        {
                            open = new ClothesScale();
                            open.ClothesId = pack.ClothesId;
                            open.FitId = pack.FitId;
                            open.InseamId = pack.InseamId;
                            open.InvQty = 0;
                            open.IsOpenSize = true;
                            open.Name = null;
                            open.IsActive = true;
                            open.IsDelete = false;
                            open.DateCreated = open.DateUpdated = DateTime.UtcNow;
                            db.ClothesScales.Add(open);
                            db.SaveChanges();
                        }
                        foreach (var size in pack.ClothesScaleSizes)
                        {
                            var openSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == open.ClothesScaleId && x.SizeId == size.SizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                            if (openSize == null)
                            {
                                openSize = new ClothesScaleSize();
                                openSize.ClothesScaleId = open.ClothesScaleId;
                                openSize.SizeId = size.SizeId;
                                openSize.Quantity = size.Quantity;
                                openSize.IsActive = true;
                                openSize.IsDelete = false;
                                openSize.DateCreated = openSize.DateUpdated = DateTime.UtcNow;
                                db.ClothesScaleSizes.Add(openSize);
                            }
                            else
                            {
                                openSize.Quantity = openSize.Quantity + size.Quantity;
                                openSize.DateUpdated = DateTime.UtcNow;
                            }
                            db.SaveChanges();

                            //var orderSize = order.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == openSize.ClothesScaleSizeId);
                            //if (orderSize == null)
                            //{
                            //    orderSize = new OrderSize();
                            //    orderSize.ClothesSizeId = openSize.ClothesScaleSizeId;
                            //    orderSize.OrderId = order.OrderId;
                            //    orderSize.DateCreated = DateTime.UtcNow;
                            //    orderSize.Quantity = size.Quantity;
                            //    orderSize.DateUpdated = DateTime.UtcNow;
                            //    orderSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            //    db.OrderSizes.Add(orderSize);
                            //}
                            //else
                            //{
                            //    if (orderSize.Quantity == null)
                            //        orderSize.Quantity = 0;

                            //    orderSize.Quantity += size.Quantity;
                            //    orderSize.DateUpdated = DateTime.UtcNow;
                            //}
                            //db.SaveChanges();
                        }
                        pack.InvQty -= 1;
                        pack.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                }
                return Json("Pack not found", JsonRequestBehavior.AllowGet);
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Detail(DetailViewClass Items)
        {
            Session.Remove("PaypalToken");
            Session.Remove("WasCleared");
            var cloth = db.Clothes.Find(Items.ClothesId);
            if (cloth.IsActive == false)
            {
                TempData["PageMessage"] = "Can't order inactive items";
                RedirectToAction("Detail", new { id = cloth.StyleNumber });
            }
            bool created = false;
            int UserId = 0;
            int StatusId = 0;
            bool loadRetail = false, isCookie = false;
            if (string.IsNullOrEmpty(SiteIdentity.UserId))
                loadRetail = isCookie = true;
            else
            {
                int userId = int.Parse(SiteIdentity.UserId);
                var user = db.Accounts.Find(userId);
                if (user != null)
                {
                    if (user.RoleId == (int)RolesEnum.Customer || user.RoleId == (int)RolesEnum.User)
                    {
                        if (user.CustomerOptionalInfoes.Count > 0)
                            loadRetail = user.CustomerOptionalInfoes.FirstOrDefault().CustomerType == (int)Platini.Models.CustomerType.Retail;
                        else
                            loadRetail = true;
                    }
                }
            }
            List<string> Errors = new List<string>();
            DB.Order lastOrder = null;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;

            if (Session["Order"] == null && Session["EditingOrder"] == null && !isCookie)
            {
                int.TryParse(SiteIdentity.UserId, out UserId);
                lastOrder = db.Orders.Where(x => (x.AccountId == UserId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
                if (lastOrder == null)
                {
                    lastOrder = new DB.Order();
                    lastOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                    lastOrder.OrderNumber = string.Empty;
                    lastOrder.AccountId = UserId;
                    if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower())
                    {
                        var sp = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == UserId);
                        if (sp != null)
                            lastOrder.EmployeeId = sp.SalesPersonId;
                    }
                    if (!lastOrder.EmployeeId.HasValue)
                        lastOrder.EmployeeId = UserId;
                    lastOrder.CreatedOn = DateTime.UtcNow;
                    lastOrder.DateCreated = lastOrder.DateUpdated = DateTime.UtcNow;
                    lastOrder.StatusId = StatusId;
                    //lastOrder.ShippingCost = loadRetail ? 7 : 0;
                    lastOrder.ShippingCost = 0;
                    decimal disc = 0.0m;
                    if (UserId > 0)
                    {
                        var ci = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == UserId);
                        if (ci != null)
                            disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                    }
                    lastOrder.Discount = disc;
                    lastOrder.IsDelete = false;
                    lastOrder.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                    lastOrder.IsSentToQuickBook = false;
                    db.Orders.Add(lastOrder);
                    db.SaveChanges();
                    created = true;
                }
                Session["Order"] = lastOrder.OrderId;
            }
            else if (Session["EditingOrder"] != null)
            {
                var OrderId = (Guid)Session["EditingOrder"];
                lastOrder = db.Orders.Find(OrderId);
                if (lastOrder == null)
                    return RedirectToAction("Index");
            }
            else if (Session["Order"] != null)
            {
                var OrderId = (Guid)Session["Order"];
                lastOrder = db.Orders.Find(OrderId);
                if (lastOrder == null)
                    return RedirectToAction("Index");
            }

            else
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie == null)
                {
                    lastOrder = new DB.Order();
                    lastOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                    lastOrder.OrderNumber = string.Empty;
                    lastOrder.AccountId = UserId;
                    lastOrder.EmployeeId = UserId;
                    lastOrder.CreatedOn = DateTime.UtcNow;
                    lastOrder.DateCreated = lastOrder.DateUpdated = DateTime.UtcNow;
                    lastOrder.StatusId = StatusId;
                    //lastOrder.ShippingCost = 7;
                    lastOrder.ShippingCost = 0;
                    decimal disc = 0.0m;
                    lastOrder.Discount = disc;
                    lastOrder.IsDelete = false;
                    lastOrder.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                    lastOrder.IsSentToQuickBook = false;
                    created = true;
                }
                else
                {
                    try
                    {
                        lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                    }
                    catch
                    {
                        return RedirectToAction("Index");
                    }
                }
            }
            if (Items != null)
            {
                if (Items.AvailablePrePacks != null)
                {
                    foreach (var item in Items.AvailablePrePacks)
                    {
                        if (item != null)
                        {
                            if (item.ClothesScaleId > 0) //&& (item.InvQty.HasValue ? item.InvQty.Value > 0 : false))
                            {
                                var pack = db.ClothesScales.Find(item.ClothesScaleId);
                                var ordPack = db.OrderScales.FirstOrDefault(x => x.ClothesScaleId == item.ClothesScaleId && x.OrderId == lastOrder.OrderId);
                                if (pack != null)
                                {
                                    item.InvQty = item.InvQty.HasValue ? (item.InvQty.Value > 0 ? item.InvQty.Value : 0) : 0;
                                    if (pack.InvQty >= item.InvQty && pack.IsActive == true && pack.IsDelete == false)
                                    {
                                        if (ordPack == null)
                                        {
                                            ordPack = new OrderScale();
                                            ordPack.OrderScaleId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                            ordPack.OrderId = lastOrder.OrderId;
                                            ordPack.Quantity = item.InvQty;
                                            ordPack.ClothesScaleId = pack.ClothesScaleId;
                                            ordPack.ClothesId = pack.ClothesId;
                                            ordPack.PackedQty = 0;
                                            ordPack.IsConfirmed = false;
                                            ordPack.DateCreated = ordPack.DateUpdated = DateTime.UtcNow;
                                            db.OrderScales.Add(ordPack);
                                        }
                                        else
                                        {
                                            ordPack.Quantity = item.InvQty;
                                            ordPack.DateUpdated = DateTime.UtcNow;
                                        }
                                        db.SaveChanges();
                                    }
                                    else
                                        Errors.Add(string.Format("You cannot order quantity {0} for pack {1}", item.InvQty, pack.Name));
                                }
                                else
                                    Errors.Add("Pack was not found");
                            }
                        }
                    }
                }
                if (Items.AvailableOpenSizes != null)
                {
                    foreach (var item in Items.AvailableOpenSizes)
                    {
                        if (item != null)
                        {
                            if (item.ClothesScaleSizeClass != null)
                            {
                                foreach (var size in item.ClothesScaleSizeClass)
                                {
                                    if (size != null)
                                    {
                                        if (size.ClothesScaleSizeId > 0 && (size.Quantity.HasValue ? size.Quantity > 0 : false))
                                        {
                                            var cSize = db.ClothesScaleSizes.Find(size.ClothesScaleSizeId);
                                            if (cSize != null && cSize.IsActive == true && cSize.IsDelete == false)
                                            {
                                                if (cSize.Quantity >= size.Quantity || loadRetail)
                                                {
                                                    var oSize = new OrderSize();
                                                    if (!isCookie)
                                                        oSize = db.OrderSizes.FirstOrDefault(x => x.OrderId == lastOrder.OrderId && x.ClothesSizeId == cSize.ClothesScaleSizeId);
                                                    else
                                                        oSize = lastOrder.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == cSize.ClothesScaleSizeId);
                                                    if (oSize == null)
                                                    {
                                                        oSize = new OrderSize();
                                                        oSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                                        oSize.OrderId = lastOrder.OrderId;
                                                        oSize.ClothesSizeId = cSize.ClothesScaleSizeId;
                                                        oSize.Quantity = size.Quantity;
                                                        oSize.ClothesId = cSize.ClothesScale.ClothesId;
                                                        oSize.PackedQty = 0;
                                                        oSize.IsConfirmed = false;
                                                        oSize.DateCreated = oSize.DateUpdated = DateTime.UtcNow;
                                                        if (!isCookie)
                                                            db.OrderSizes.Add(oSize);
                                                        else
                                                            lastOrder.OrderSizes.Add(oSize);

                                                    }
                                                    else
                                                    {
                                                        oSize.Quantity = size.Quantity;
                                                        oSize.DateUpdated = DateTime.UtcNow;
                                                    }
                                                    if (!isCookie)
                                                        db.SaveChanges();
                                                }
                                                else
                                                    Errors.Add(string.Format("You cannot order quantity {0} for size {1}", size.Quantity, cSize.Size.Name));
                                            }
                                            else
                                                Errors.Add("Size was not found");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            lastOrder.DateUpdated = DateTime.UtcNow;

            lastOrder.OriginalQty = 0;
            if (!isCookie)
            {
                if (db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                    lastOrder.OriginalQty = db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
                if (db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                    lastOrder.OriginalQty += db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                db.SaveChanges();
                Session["Order"] = lastOrder.OrderId;
            }
            else
            {
                if (lastOrder.OrderSizes.Any())
                    lastOrder.OriginalQty = lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                HttpCookie ordCookie = new HttpCookie("ordCookie");
                ordCookie.Value = JsonConvert.SerializeObject(lastOrder);
                ordCookie.Expires = DateTime.Now.AddMonths(1);
                if (created)
                    Response.Cookies.Add(ordCookie);
                else
                {
                    //Response.Cookies.Set(ordCookie);
                    Response.Cookies["ordCookie"].Value = ordCookie.Value;
                    Response.Cookies["ordCookie"].Expires = ordCookie.Expires;
                }
            }
            if (Errors.Count > 0)
                TempData["PageMessage"] = "The Order was " + (created ? "created" : "updated") + " with errors. " + string.Join(". ", Errors);

            if (Request.UrlReferrer != null)
            {
                if (!string.IsNullOrEmpty(Request.UrlReferrer.AbsolutePath) && Request.UrlReferrer.AbsolutePath.Contains("SeeAll"))
                {

                    if (cloth.FutureDeliveryDate.HasValue && (cloth.FutureDeliveryDate.Value != DateTime.MinValue && cloth.FutureDeliveryDate.Value > new DateTime(1900, 1, 1)))
                        return RedirectToAction("SeeAll", new { id = "f" });
                    var Category = db.Categories.Find(cloth.CategoryId);
                    return RedirectToAction("SeeAll", new { id = Category.ParentId });
                }
            }
            return RedirectToAction("Detail", new { id = cloth.StyleNumber });
        }

        [HttpPost]
        public ActionResult EditCloth(DetailViewClass Cloth)
        {
            List<string> Errors = new List<string>();
            if (Cloth != null)
            {

                var existCloth = db.Clothes.FirstOrDefault(x => x.StyleNumber.ToLower() == Cloth.StyleNumber.ToLower() && x.IsDelete == false && x.ClothesId != Cloth.ClothesId);
                if (existCloth != null)
                    Errors.Add("The style number already exists");
                else
                {
                    existCloth = db.Clothes.FirstOrDefault(x => x.IsDelete == false && x.ClothesId == Cloth.ClothesId);
                    if (existCloth != null)
                    {
                        existCloth.StyleNumber = !string.IsNullOrEmpty(Cloth.StyleNumber) ? Cloth.StyleNumber : existCloth.StyleNumber;
                        existCloth.ProductCost = Cloth.Cost;
                        existCloth.Price = Cloth.Price;
                        existCloth.MSRP = Cloth.MSRP;
                        existCloth.Color = Cloth.Color;
                        existCloth.Clearance = Cloth.Clearance;
                        existCloth.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                        if (Cloth.AvailableOpenSizes != null)
                        {
                            foreach (var item in Cloth.AvailablePrePacks)
                            {
                                if (item != null)
                                {
                                    var pack = db.ClothesScales.Find(item.ClothesScaleId);
                                    if (pack != null)
                                    {
                                        pack.Name = item.Name;
                                        pack.InvQty = item.InvQty;
                                        foreach (var size in item.ClothesScaleSizeClass)
                                        {
                                            if (size != null)
                                            {
                                                var dbSize = db.ClothesScaleSizes.Find(size.ClothesScaleSizeId);
                                                if (dbSize != null)
                                                {
                                                    dbSize.Quantity = size.Quantity.HasValue ? size.Quantity.Value : dbSize.Quantity;
                                                    dbSize.DateUpdated = DateTime.UtcNow;
                                                }
                                            }
                                        }
                                        pack.DateUpdated = DateTime.UtcNow;
                                    }
                                    db.SaveChanges();
                                }
                            }
                        }
                        if (Cloth.AvailableOpenSizes != null)
                        {
                            foreach (var item in Cloth.AvailableOpenSizes)
                            {
                                if (item != null)
                                {
                                    var pack = db.ClothesScales.Find(item.ClothesScaleId);
                                    if (pack != null)
                                    {
                                        foreach (var size in item.ClothesScaleSizeClass)
                                        {
                                            if (size != null)
                                            {
                                                var dbSize = db.ClothesScaleSizes.Find(size.ClothesScaleSizeId);
                                                if (dbSize != null)
                                                {
                                                    dbSize.Quantity = size.Quantity.HasValue ? size.Quantity.Value : dbSize.Quantity;
                                                    dbSize.DateUpdated = DateTime.UtcNow;
                                                }
                                            }
                                        }
                                        pack.DateUpdated = DateTime.UtcNow;
                                    }
                                    db.SaveChanges();
                                }
                            }
                        }
                        HttpPostedFileBase upPic = Request.Files["fup1"];
                        if (upPic != null && upPic.ContentLength != 0 && upPic.InputStream != null)
                        {
                            int so = 1;
                            var imageOrder = db.ClothesImages.Where(x => x.ClothesId == Cloth.ClothesId && x.IsActive && !x.IsDelete).ToList().OrderBy(x => x.SortOrder).LastOrDefault();
                            if (imageOrder != null)
                                so = (imageOrder.SortOrder.HasValue ? imageOrder.SortOrder.Value : so) + 1;
                            string res = SaveUpdateClothesImage(upPic, so, Cloth.ClothesId, Cloth.StyleNumber);
                            if (!string.IsNullOrEmpty(res))
                                Errors.Add(res);
                        }
                    }
                    else
                        Errors.Add("Cloth not found");
                }
                if (Errors.Count > 0)
                    TempData["PageMessage"] = string.Join(". ", Errors);
                return RedirectToAction("Detail", new { id = existCloth.StyleNumber });

            }
            TempData["PageMessage"] = "No cloth received.";
            return RedirectToAction("Index");
        }

        [NonAction]
        public string SaveUpdateClothesImage(HttpPostedFileBase upPic, int SortOrder, int ClothesId, string StyleNumber)
        {
            string fileName;
            System.Drawing.Imaging.ImageFormat format;
            if (VerifyImage(upPic.ContentType.ToLower(), out fileName, out format))
            {
                var clothesImage = db.ClothesImages.Where(i => i.ClothesId == ClothesId && i.SortOrder == SortOrder).FirstOrDefault();
                if (clothesImage != null)
                {
                    string imagePath = clothesImage.ImagePath;
                    System.IO.File.Delete(Server.MapPath("~/Library/Uploads/WebThumb/" + imagePath));
                }
                else
                {
                    clothesImage = new ClothesImage();
                    clothesImage.DateCreated = DateTime.UtcNow;
                }
                clothesImage.ImageName = StyleNumber + "-" + ClothesId + "-" + SortOrder;
                clothesImage.ImagePath = fileName;
                clothesImage.ClothesId = ClothesId;
                clothesImage.SortOrder = SortOrder;
                clothesImage.IsActive = true;
                clothesImage.IsDelete = false;
                clothesImage.DateUpdated = DateTime.UtcNow;
                db.ClothesImages.Add(clothesImage);
                db.SaveChanges();
                System.Drawing.Image.FromStream(upPic.InputStream).Save(Server.MapPath("~/Library/Uploads/WebThumb/" + fileName), format);
                return "";
            }
            return "Please upload only image files";
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

        [HttpPost]
        public ActionResult DeleteImage(int ClothesId)
        {
            if (ClothesId > 0)
            {
                var image = db.ClothesImages.Find(ClothesId);
                if (image != null)
                {
                    if (!string.IsNullOrEmpty(image.ImagePath))
                        System.IO.File.Delete(Server.MapPath("~/Library/Uploads/WebThumb/" + image.ImagePath));
                    image.IsDelete = true;
                    image.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();

                    return Json("Success", JsonRequestBehavior.AllowGet);
                }
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteProduct(int ClothesId)
        {
            if (ClothesId > 0)
            {
                var cloth = db.Clothes.Find(ClothesId);
                if (cloth != null)
                {
                    cloth.IsActive = false;
                    cloth.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                    return Json("Success", JsonRequestBehavior.AllowGet);
                }
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        //private void UpdateIdentity(string userDataString)
        //{
        //    if (System.Web.HttpContext.Current.User.Identity.IsAuthenticated)
        //    {
        //        if (System.Web.HttpContext.Current.User.Identity is FormsIdentity)
        //        {
        //            //FormsIdentity id = (FormsIdentity)System.Web.HttpContext.Current.User.Identity;
        //            //FormsAuthenticationTicket ticket = id.Ticket;
        //            //string userData = ticket.UserData;
        //            //userData = userDataString;
        //            //string[] roles = userData.Split('|');
        //            //System.Web.HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(id, roles);
        //            HttpCookie authCookie = FormsAuthentication.GetAuthCookie(SiteIdentity.Name, true);
        //            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
        //            FormsAuthenticationTicket newTicket = new FormsAuthenticationTicket(ticket.Version, ticket.Name, ticket.IssueDate, ticket.Expiration, ticket.IsPersistent, userDataString);
        //            authCookie.Value = FormsAuthentication.Encrypt(newTicket);
        //            Response.Cookies.Add(authCookie); 
        //        }
        //    }
        //}

        [HttpGet]
        public ActionResult LineSheet(int? page, int? TypeId, string Ids, int? PageSize, string future, string deactive, string search, int? sortBy)
        {
            var retLineModel = new List<LineSheetViewClass>();
            IEnumerable<int> values = new List<int>();
            ViewBag.MassMode = false;
            if (!string.IsNullOrEmpty(Ids))
            {
                ViewBag.Ids = Ids;
                ViewBag.MassMode = true;
                values = Ids.Split(',').Select(s => int.Parse(s));
                TypeId = db.Clothes.Find(values.FirstOrDefault()).CategoryId;
            }
            ViewBag.PageMessage = TempData["PageMessage"];
            ViewBag.TypeId = TypeId;
            ViewBag.PageSize = defaultpageSize;
            bool loadRetail = true;
            if (!string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                int UserId = 0;
                int.TryParse(SiteIdentity.UserId, out UserId);
                var account = db.Accounts.Find(UserId);
                if (account != null)
                {
                    if (account.RoleId == (int)RolesEnum.Customer)
                    {
                        var ci = account.CustomerOptionalInfoes.FirstOrDefault();
                        if (ci != null)
                            loadRetail = ci.CustomerType == (int)Models.CustomerType.Retail;
                    }
                    else
                        loadRetail = false;
                }
            }
            if (loadRetail)
                deactive = future = null;
            sortBy = sortBy.HasValue ? sortBy.Value : 1;
            ViewBag.loadRetail = loadRetail;
            ViewBag.Header = !string.IsNullOrEmpty(future) ? "Future Deliveries" : (!string.IsNullOrEmpty(deactive) ? "Deactivated Products" : "Active Products");
            ViewBag.isFuture = future;
            ViewBag.isDeactive = deactive;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            if (PageSize.HasValue)
                ViewBag.PageSize = PageSize.Value;
            if (!loadRetail)
                return View(LineSheetData(page, TypeId, Ids, PageSize, !string.IsNullOrEmpty(future), !string.IsNullOrEmpty(deactive), search, sortBy.Value));
            else
                return View("LineSheetRetail", LineSheetData(page, TypeId, Ids, PageSize, !string.IsNullOrEmpty(future), !string.IsNullOrEmpty(deactive), search, sortBy.Value));
        }

        [HttpPost]
        public ActionResult LineSheetS(int? page, int? TypeId, string search, string deactive)
        {
            string s = Request.Form["deactive"];
            ViewBag.TypeId = TypeId;
            ViewBag.Search = search;
            ViewBag.isDeactive = s;
            return RedirectToAction("LineSheet", new { @TypeId = TypeId, @search = search, @deactive = deactive });
        }

        [NonAction]
        public IPagedList<LineSheetViewClass> LineSheetData(int? page, int? TypeId, string Ids, int? PageSize, bool future, bool deactive, string search, int SortBy)
        {
            var retLineModel = new List<LineSheetViewClass>();

            bool loadRetail = true;
            if (!string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                int UserId = 0;
                int.TryParse(SiteIdentity.UserId, out UserId);
                var account = db.Accounts.Find(UserId);
                if (account != null)
                {
                    if (account.RoleId == (int)RolesEnum.Customer)
                    {
                        var ci = account.CustomerOptionalInfoes.FirstOrDefault();
                        if (ci != null)
                            loadRetail = ci.CustomerType == (int)Models.CustomerType.Retail;
                    }
                    else
                        loadRetail = false;
                }
            }

            bool skip = (future || deactive) && (loadRetail ? true : SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower());
            bool sortCat = false;
            IEnumerable<int> values = new List<int>();
            if (!string.IsNullOrEmpty(Ids))
            {
                values = Ids.Split(',').Select(s => int.Parse(s));
                TypeId = db.Clothes.Find(values.FirstOrDefault()).CategoryId;
            }
            if (TypeId.HasValue && SiteConfiguration.CatID != TypeId.Value)
            {
                var Category = db.Categories.Find(TypeId);
                var SubCategory = db.Categories.Find(Category.ParentId);
                SiteConfiguration.MainID = SubCategory.ParentId;
                SiteConfiguration.SubID = Category.ParentId;
                SiteConfiguration.CatID = TypeId.Value;
            }

            if (!TypeId.HasValue && SiteConfiguration.CatID > 0 && !future && !deactive)
            {
                TypeId = SiteConfiguration.CatID;
            }

            if ((TypeId > 0 || future || deactive) && !skip)
            {

                var tempDate = new DateTime(1900, 1, 1);
                var list = new List<Cloth>();

                if (future)
                    list = db.Clothes.Where(x => (x.FutureDeliveryDate.HasValue ? (x.FutureDeliveryDate.Value != DateTime.MinValue && x.FutureDeliveryDate.Value != tempDate) : false) && x.IsActive == true && x.IsDelete == false && (TypeId.HasValue ? x.CategoryId == TypeId.Value : true)).ToList();
                else if (deactive)
                    list = db.Clothes.Where(x => x.IsActive == false && x.IsDelete == false && (!string.IsNullOrEmpty(search) ? !string.IsNullOrEmpty(x.StyleNumber) ? x.StyleNumber.Contains(search) : false : true)).ToList();
                else if (TypeId > 0)
                    list = db.Clothes.Where(x => x.CategoryId == TypeId && x.IsActive == true && x.IsDelete == false && (values.Count() > 0 ? values.Contains(x.ClothesId) : true) && (x.FutureDeliveryDate == null || x.FutureDeliveryDate == DateTime.MinValue || x.FutureDeliveryDate == tempDate) && (loadRetail ? x.MSRP > 0 : true))
                        .ToList();

                if (!string.IsNullOrEmpty(search))
                    list = list.Where(x => x.StyleNumber.ToLower().Contains(search.ToLower())).ToList();
                if (SortBy == (int)LineSheetSort.DateChanged)
                    list = list.OrderBy(x => x.SortOrder).ThenByDescending(x => x.DateChanged).ThenBy(x => x.Clearance).ToList();
                else if (SortBy == (int)LineSheetSort.FutureDelivery)
                    list = list.OrderBy(x => x.FutureDeliveryDate).ToList();
                else if (SortBy == (int)LineSheetSort.Category)
                    sortCat = true;
                var webSetting = db.WebsiteSettings.Where(x => x.SettingKey.ToLower() == "settings").FirstOrDefault();
                bool check = false;
                if (webSetting != null)
                {
                    check = !string.IsNullOrEmpty(webSetting.SettingValue) ? (webSetting.SettingValue != "0" ? true : false) : false;
                }
                if (check)
                {
                    List<DB.Cloth> removeClothes = new List<DB.Cloth>().InjectFrom(list);
                    foreach (var item in removeClothes)
                    {
                        int cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        if (cQty <= 0)
                        {
                            var itemL = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault();
                            list.Remove(itemL);
                        }
                    }
                }
                if (list.Any())
                {
                    var dbFitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                    var dbInseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                    foreach (var cloth in list)
                    {
                        var model = new LineSheetViewClass();

                        model.ClothesId = cloth.ClothesId;
                        model.StyleNumber = cloth.StyleNumber;
                        model.Color = cloth.Color;
                        model.Clearance = cloth.Clearance ?? 0;
                        model.MSRP = cloth.MSRP.HasValue ? cloth.MSRP.Value : 0.0m;
                        model.Price = cloth.Price.HasValue ? cloth.Price.Value : 0.0m;
                        model.Cost = cloth.ProductCost.HasValue ? cloth.ProductCost.Value : 0.0m;
                        model.DiscountedMSRP = cloth.DiscountedMSRP.HasValue ? cloth.DiscountedMSRP.Value : 0.0m;
                        model.DiscountedPrice = cloth.DiscountedPrice.HasValue ? cloth.DiscountedPrice.Value : 0.0m;
                        model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                        model.CategoryId = cloth.CategoryId;
                        model.isActive = cloth.IsActive.HasValue ? cloth.IsActive.Value : false;
                        model.TotalQty = (cloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        if (sortCat)
                        {
                            model.Type = cloth.Category.Name;
                            model.Sub = db.Categories.Find(cloth.Category.ParentId).Name;
                        }
                        var sizeGroupId = cloth.SizeGroupId;
                        model.isFuture = cloth.FutureDeliveryDate.HasValue ? (cloth.FutureDeliveryDate.Value != DateTime.MinValue && cloth.FutureDeliveryDate.Value != tempDate) : false;
                        model.fDate = model.isFuture ? cloth.FutureDeliveryDate.Value.ToString() : "";
                        List<int?> FitList = cloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                        List<int?> InseamList = cloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();

                        model.FitList = dbFitList.Where(x => FitList.Contains(x.Id)).ToList();
                        model.InseamList = dbInseamList.Where(x => InseamList.Contains(x.Id)).ToList();

                        if (FitList.Count() == 0)
                            FitList.Add(null);

                        if (InseamList.Count() == 0)
                            InseamList.Add(null);

                        var scaleList = cloth.ClothesScales;
                        foreach (var fitid in FitList)
                        {
                            foreach (var inseamid in InseamList)
                            {
                                if (scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid).Any())
                                {
                                    var clothesScales = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid);
                                    foreach (var scale in clothesScales)
                                    {
                                        if (loadRetail && scale.IsOpenSize == false)
                                            continue;
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        var sSQOpenSize = scale.ClothesScaleSizes.Where(x => x.IsActive == true && x.IsDelete == false);
                                        foreach (var item in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = item.ClothesScaleId;
                                            scaleRow.SizeId = item.SizeId;
                                            scaleRow.SizeName = item.Size.Name;
                                            scaleRow.Quantity = item.Quantity;
                                            scaleRow.RtlAvlbl = true;
                                            if (!scaleRow.Quantity.HasValue || scaleRow.Quantity <= 0)
                                                if (!scaleList.Any(x => x.FitId == fitid && x.InseamId == inseamid && x.InvQty > 0))
                                                    scaleRow.RtlAvlbl = false;
                                                else if (scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.InvQty > 0).SelectMany(x =>
                                                    x.ClothesScaleSizes.Where(y => y.SizeId == scaleRow.SizeId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        var availableOpenSizeItem = new ClothesScaleClass();
                                        availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizesForOS);
                                        availableOpenSizeItem.InseamId = scale.InseamId;
                                        availableOpenSizeItem.FitId = scale.FitId;
                                        availableOpenSizeItem.IsOpenSize = scale.IsOpenSize;
                                        availableOpenSizeItem.Name = scale.Name;
                                        availableOpenSizeItem.InvQty = scale.InvQty;
                                        if (fitid.HasValue)
                                            availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                        if (inseamid.HasValue)
                                            availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                        availableOpenSizeItem.ClothesId = scale.ClothesId;
                                        availableOpenSizeItem.ClothesScaleId = scale.ClothesScaleId;
                                        model.ClothesScale.Add(availableOpenSizeItem);
                                    }
                                }
                            }
                        }
                        model.ClothesScale = model.ClothesScale.OrderBy(x => x.FitId).ThenByDescending(x => x.IsOpenSize).ThenBy(x => x.InseamId).ToList();
                        retLineModel.Add(model);
                    }
                }
            }
            if (TempData["SelectIds"] != null)
            {
                string sIds = TempData["SelectIds"].ToString();
                values = sIds.Trim(',').Split(',').Select(x => int.Parse(x));
                retLineModel = retLineModel.FindAll(x => values.Contains(x.ClothesId));
                TempData["SelectIds"] = null;
            }
            if (sortCat)
                retLineModel = retLineModel.OrderBy(x => x.Sub).ThenBy(x => x.Type).ToList();
            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            if (!string.IsNullOrEmpty(Ids))
                return retLineModel.ToPagedList(currentPageIndex, values.Count());
            else
                return retLineModel.ToPagedList(currentPageIndex, (PageSize.HasValue ? (PageSize.Value > 0 ? PageSize.Value : retLineModel.Count) : defaultpageSize));
        }

        public ActionResult LineSheetCategory(int? TypeId, string future, string deactive)
        {
            var retModel = new LinesheetSelectModel();
            retModel.Categories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
            if (TypeId.HasValue)
            {
                retModel.SubCategoryTypeId = TypeId.Value;
                retModel.SubCategoryId = db.Categories.Where(x => x.CategoryId == retModel.SubCategoryTypeId).FirstOrDefault().ParentId;
                retModel.CategoryId = db.Categories.Where(x => x.CategoryId == retModel.SubCategoryId).FirstOrDefault().ParentId;
                retModel.SubCategories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == retModel.CategoryId).Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
                retModel.Types = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == retModel.SubCategoryId).Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();

            }
            else if (SiteConfiguration.SubID > 0)
            {
                retModel.SubCategoryId = SiteConfiguration.SubID;
                retModel.CategoryId = db.Categories.Where(x => x.CategoryId == retModel.SubCategoryId).FirstOrDefault().ParentId;
                retModel.SubCategories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == retModel.CategoryId).Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
                retModel.Types = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == retModel.SubCategoryId).Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
            }
            else if (SiteConfiguration.MainID > 0)
            {
                retModel.CategoryId = SiteConfiguration.MainID;
                retModel.SubCategories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == retModel.CategoryId).Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
                retModel.Types = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == retModel.SubCategoryId).Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
            }
            else
            {
                retModel.SubCategories = new List<Category>().Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
                retModel.Types = new List<Category>().Select(x => new SelectedListValues { Id = x.CategoryId, Value = x.Name, IsSelected = false }).ToList();
            }
            ViewBag.isFuture = !string.IsNullOrEmpty(future);
            ViewBag.isDeactive = !string.IsNullOrEmpty(deactive);
            return PartialView("LineSheetPartial", retModel);
        }

        [HttpPost]
        public ActionResult LineSheet(List<LineSheetViewClass> ls)
        {

            if (ls != null)
            {
                foreach (var lsItem in ls)
                {
                    if (lsItem.isSelected)
                    {
                        var cloth = db.Clothes.Find(lsItem.ClothesId);
                        if (cloth != null)
                        {
                            bool uniqeStyleNumber = db.Clothes.FirstOrDefault(x => x.ClothesId != lsItem.ClothesId && x.StyleNumber == lsItem.StyleNumber && x.IsActive == true && x.IsDelete == false) == null;
                            if (uniqeStyleNumber)
                                cloth.StyleNumber = !string.IsNullOrEmpty(lsItem.StyleNumber) ? lsItem.StyleNumber : cloth.StyleNumber;
                            cloth.Price = lsItem.Price;
                            cloth.MSRP = lsItem.MSRP;
                            cloth.DiscountedMSRP = lsItem.DiscountedMSRP != null ? lsItem.DiscountedMSRP.Value : 0.0m;
                            cloth.DiscountedPrice = lsItem.DiscountedPrice != null ? lsItem.DiscountedPrice.Value : 0.0m;
                            //cloth.ProductCost = lsItem.Cost;
                            if (cloth.Clearance != lsItem.Clearance && lsItem.Clearance == 1)
                                cloth.SortOrder = int.MaxValue;
                            cloth.Clearance = lsItem.Clearance;
                            cloth.IsActive = lsItem.isActive != null ? lsItem.isActive : false;
                            if (!string.IsNullOrEmpty(lsItem.fDate))
                            {
                                DateTime fDate;
                                DateTime.TryParse(lsItem.fDate, out fDate);
                                if (fDate != DateTime.MinValue)
                                    cloth.FutureDeliveryDate = fDate;
                            }
                            else
                            {
                                cloth.FutureDeliveryDate = null;
                                if (cloth.SortOrder != int.MaxValue)
                                    cloth.SortOrder = 0;
                                //cloth.DateChanged = DateTime.UtcNow;
                            }
                            cloth.Color = lsItem.Color;
                            if (lsItem.ClothesScale != null)
                            {
                                foreach (var clothScale in lsItem.ClothesScale)
                                {
                                    if (clothScale != null)
                                    {
                                        var scale = cloth.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == clothScale.ClothesScaleId && x.IsDelete == false);
                                        if (scale != null)
                                        {
                                            if (scale.IsOpenSize == false)
                                            {
                                                scale.InvQty = clothScale.InvQty.HasValue ? clothScale.InvQty.Value : scale.InvQty;
                                                scale.DateUpdated = DateTime.UtcNow;
                                            }
                                        }
                                        else
                                        {
                                            scale = new ClothesScale();
                                            scale.ClothesId = cloth.ClothesId;
                                            if (clothScale.FitId > 0)
                                                scale.FitId = clothScale.FitId;
                                            if (clothScale.InseamId > 0)
                                                scale.InseamId = clothScale.InseamId;
                                            scale.InvQty = clothScale.InvQty;
                                            scale.Name = clothScale.Name;
                                            scale.IsActive = true;
                                            scale.IsDelete = false;
                                            scale.IsOpenSize = clothScale.IsOpenSize;
                                            scale.DateCreated = DateTime.UtcNow;
                                            scale.DateUpdated = DateTime.UtcNow;
                                            db.ClothesScales.Add(scale);
                                        }
                                        db.SaveChanges();
                                        if (clothScale.ClothesScaleSizeClass != null)
                                        {
                                            foreach (var scalesize in clothScale.ClothesScaleSizeClass)
                                            {
                                                if (scalesize != null)
                                                {
                                                    var scaleSize = scale.ClothesScaleSizes.FirstOrDefault(x => x.IsDelete == false && x.ClothesScaleSizeId == scalesize.ClothesScaleSizeId);
                                                    if (scaleSize != null)
                                                    {
                                                        scaleSize.Quantity = scalesize.Quantity.HasValue ? scalesize.Quantity.Value : scaleSize.Quantity;
                                                        scaleSize.DateUpdated = DateTime.UtcNow;
                                                    }
                                                    else
                                                    {
                                                        scaleSize = new ClothesScaleSize();
                                                        scaleSize.ClothesScaleId = scale.ClothesScaleId;
                                                        scaleSize.SizeId = scalesize.SizeId;
                                                        scaleSize.Quantity = scalesize.Quantity;
                                                        scaleSize.DateCreated = DateTime.UtcNow;
                                                        scaleSize.DateUpdated = DateTime.UtcNow;
                                                        db.ClothesScaleSizes.Add(scaleSize);
                                                    }
                                                    db.SaveChanges();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            int quant = (cloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                         + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                            if (!cloth.OriginalQty.HasValue)
                                cloth.OriginalQty = quant;

                            cloth.AdjustQty = quant - cloth.OriginalQty.Value;
                            cloth.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();

                            if (QuickBookStrings.UseQuickBook())
                                AddProductToQuickBook(cloth.ClothesId);
                        }
                    }                                    
                }
                int? page = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["page"] != null ? Convert.ToInt32(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["page"].ToString()) : (int?)null;
                int? pS = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["PageSize"] != null ? Convert.ToInt32(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["PageSize"].ToString()) : (int?)null;
                string future = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["future"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["future"] : null;
                string deactive = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["deactive"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["deactive"] : null;
                if (string.IsNullOrEmpty(future) && string.IsNullOrEmpty(deactive))
                    return RedirectToAction("LineSheet", new { TypeId = ls.FirstOrDefault().CategoryId, @page = page, @PageSize = pS });
                else
                    return RedirectToAction("LineSheet", new { @future = future, @deactive = deactive, @page = page, @PageSize = pS });
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult LineSheetShop(List<LineSheetViewClass> ls)
        {
            Session.Remove("PaypalToken");
            Session.Remove("WasCleared");
            bool created = false;
            int UserId = 0;
            int StatusId = 0;
            List<string> Errors = new List<string>();
            DB.Order lastOrder = null;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            bool loadRetail = true;
            bool isCookie = string.IsNullOrEmpty(SiteIdentity.UserId);
            int.TryParse(SiteIdentity.UserId, out UserId);
            if (UserId > 0)
            {
                var account = db.Accounts.Find(UserId);
                if (account.CustomerOptionalInfoes.FirstOrDefault() != null)
                    loadRetail = account.CustomerOptionalInfoes.FirstOrDefault().CustomerType == (int)Models.CustomerType.Retail;
            }
            if (Session["Order"] == null && Session["EditingOrder"] == null && !isCookie)
            {

                lastOrder = db.Orders.Where(x => (x.AccountId == UserId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
                if (lastOrder != null)
                    Session["Order"] = lastOrder.OrderId;
            }
            else if (Session["EditingOrder"] != null)
            {
                var OrderId = (Guid)Session["EditingOrder"];
                lastOrder = db.Orders.Find(OrderId);
                if (lastOrder == null)
                    return RedirectToAction("Index");
            }
            else if (Session["Order"] != null)
            {
                var OrderId = (Guid)Session["Order"];
                lastOrder = db.Orders.Find(OrderId);
                if (lastOrder == null)
                    return RedirectToAction("Index");
            }
            else
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                    }
                    catch
                    {
                        lastOrder = null;
                    }
                }
            }
            if (lastOrder == null)
            {
                lastOrder = new DB.Order();
                lastOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                lastOrder.OrderNumber = string.Empty;
                lastOrder.AccountId = UserId;
                if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower())
                {
                    var sp = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == UserId);
                    if (sp != null)
                        lastOrder.EmployeeId = sp.SalesPersonId;
                }
                if (!lastOrder.EmployeeId.HasValue)
                    lastOrder.EmployeeId = UserId;
                lastOrder.CreatedOn = DateTime.UtcNow;
                lastOrder.DateCreated = lastOrder.DateUpdated = DateTime.UtcNow;
                lastOrder.StatusId = StatusId;
                //lastOrder.ShippingCost = loadRetail ? 7 : 0;
                lastOrder.ShippingCost = 0;
                decimal disc = 0.0m;
                if (UserId > 0)
                {
                    var ci = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == UserId);
                    if (ci != null)
                        disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                }
                lastOrder.Discount = disc;
                lastOrder.IsDelete = false;
                lastOrder.IsSentToQuickBook = false;
                if (!isCookie)
                    db.Orders.Add(lastOrder);
                lastOrder.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                if (!isCookie)
                {
                    db.SaveChanges();
                    Session["Order"] = lastOrder.OrderId;
                }
                created = true;
            }
            if (ls != null && lastOrder != null)
            {
                foreach (var item in ls)
                {
                    if (item.isSelected)
                    {
                        var cloth = db.Clothes.Find(item.ClothesId);
                        if (cloth != null && cloth.IsActive == true && cloth.IsDelete == false)
                        {
                            if (item.ClothesScale != null)
                            {
                                var relScales = item.ClothesScale.Where(x => x.FitId == item.fitID);
                                foreach (var scale in relScales)
                                {
                                    var dbScale = cloth.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == scale.ClothesScaleId);
                                    if (dbScale != null)
                                    {
                                        if (scale.IsOpenSize == false)
                                        {
                                            scale.PurchasedQty = scale.PurchasedQty.HasValue ? (scale.PurchasedQty.Value > 0 ? scale.InvQty.Value : 0) : 0;
                                            if (scale.PurchasedQty.HasValue ? (scale.InvQty >= scale.PurchasedQty) : true)
                                            {
                                                //if (scale.PurchasedQty > 0)
                                                //{
                                                var existScale = lastOrder.OrderScales.FirstOrDefault(x => x.ClothesScaleId == scale.ClothesScaleId);
                                                if (existScale != null)
                                                {
                                                    existScale.Quantity = scale.PurchasedQty;
                                                    existScale.DateUpdated = DateTime.UtcNow;
                                                }
                                                else
                                                {
                                                    existScale = new OrderScale();
                                                    existScale.OrderScaleId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                                    existScale.ClothesScaleId = scale.ClothesScaleId;
                                                    existScale.OrderId = lastOrder.OrderId;
                                                    existScale.Quantity = scale.PurchasedQty;
                                                    existScale.ClothesId = dbScale.ClothesId;
                                                    existScale.PackedQty = 0;
                                                    existScale.IsConfirmed = false;
                                                    existScale.DateCreated = existScale.DateUpdated = DateTime.UtcNow;
                                                    db.OrderScales.Add(existScale);
                                                }
                                                db.SaveChanges();
                                                //}
                                            }
                                            else
                                                Errors.Add(string.Format("You cannot order quantity {0} for pack {1}", scale.PurchasedQty, dbScale.Name));
                                        }
                                        else if (scale.IsOpenSize == true)
                                        {
                                            if (scale.ClothesScaleSizeClass != null)
                                            {
                                                foreach (var size in scale.ClothesScaleSizeClass)
                                                {
                                                    if (size.ClothesScaleSizeId > 0 && (size.Quantity > 0 || loadRetail) && size.PurchasedQuantity.HasValue ? (size.Quantity >= size.PurchasedQuantity || loadRetail) : true)
                                                    {
                                                        if (size.PurchasedQuantity > 0)
                                                        {
                                                            var existSize = lastOrder.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesScaleSizeId);
                                                            if (existSize != null)
                                                            {
                                                                existSize.Quantity = size.PurchasedQuantity;
                                                                existSize.DateUpdated = DateTime.UtcNow;
                                                            }
                                                            else
                                                            {
                                                                existSize = new OrderSize();
                                                                existSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                                                existSize.ClothesSizeId = size.ClothesScaleSizeId;
                                                                existSize.OrderId = lastOrder.OrderId;
                                                                existSize.Quantity = size.PurchasedQuantity;
                                                                existSize.ClothesId = dbScale.ClothesId;
                                                                existSize.PackedQty = 0;
                                                                existSize.IsConfirmed = false;
                                                                existSize.DateCreated = existSize.DateUpdated = DateTime.UtcNow;
                                                                if (!isCookie)
                                                                    db.OrderSizes.Add(existSize);
                                                                else
                                                                    lastOrder.OrderSizes.Add(existSize);
                                                            }
                                                            if (!isCookie)
                                                                db.SaveChanges();
                                                        }
                                                    }
                                                    else
                                                        Errors.Add(string.Format("You cannot order quantity {0} for size {1}", size.PurchasedQuantity, size.SizeName));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                lastOrder.DateUpdated = DateTime.UtcNow;
                lastOrder.OriginalQty = 0;
                if (!isCookie)
                {
                    if (db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                        lastOrder.OriginalQty = db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
                    if (db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                        lastOrder.OriginalQty += db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                    db.SaveChanges();
                    Session["Order"] = lastOrder.OrderId;
                }
                else
                {
                    if (lastOrder.OrderSizes.Any())
                        lastOrder.OriginalQty = lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                    HttpCookie ordCookie = new HttpCookie("ordCookie");
                    ordCookie.Value = JsonConvert.SerializeObject(lastOrder);
                    ordCookie.Expires = DateTime.Now.AddMonths(1);
                    if (created)
                        Response.Cookies.Add(ordCookie);
                    else
                    {
                        //Response.Cookies.Set(ordCookie);
                        Response.Cookies["ordCookie"].Value = ordCookie.Value;
                        Response.Cookies["ordCookie"].Expires = ordCookie.Expires;
                    }
                }
                if (Errors.Count > 0)
                    TempData["PageMessage"] = "The Order was " + (created ? "created" : "updated") + " with errors. " + string.Join(". ", Errors);
                string future = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["future"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["future"] : null;
                string deactive = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["deactive"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["deactive"] : null;
                if (string.IsNullOrEmpty(future) && string.IsNullOrEmpty(deactive))
                    return RedirectToAction("LineSheet", new { TypeId = ls.FirstOrDefault().CategoryId });
                else
                    return RedirectToAction("LineSheet", new { @future = future, @deactive = deactive });
            }
            return RedirectToAction("Index");
        }


        public bool AddProductToQuickBook(int ClothesId)
        {
            QuickBookStrings.LoadQuickBookStrings(FailureFrom.Product.ToString());
            string StyleNumber = string.Empty;
            string Type = FailureFrom.Product.ToString();
            var oauthValidator = new OAuthRequestValidator(QuickBookStrings.AccessToken, QuickBookStrings.AccessTokenSecret, QuickBookStrings.ConsumerKey, QuickBookStrings.ConsumerSecret);
            QuickBookFailureRecord existFailure = null;
            if (ClothesId > 0)
                existFailure = db.QuickBookFailureRecords.FirstOrDefault(x => x.FailureFrom.ToLower() == Type.ToLower() && x.FailureFromId == ClothesId);
            try
            {
                var context = new ServiceContext(QuickBookStrings.AppToken, QuickBookStrings.CompanyId, IntuitServicesType.QBO, oauthValidator);
                context.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = GetLoc(Type, ClothesId.ToString());
                context.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl;

                var service = new DataService(context);
                var productService = new QueryService<Intuit.Ipp.Data.Item>(context);
                var newProduct = new Intuit.Ipp.Data.Item();
                var dbCloth = db.Clothes.Find(ClothesId);
                if (existFailure != null)
                    StyleNumber = existFailure.FailureOriginalValue;
                else if (dbCloth != null)
                {
                    if (!string.IsNullOrEmpty(dbCloth.StyleNumber) && dbCloth.IsActive == true && dbCloth.IsDelete == false)
                        StyleNumber = !string.IsNullOrEmpty(dbCloth.OldStyleNumber) ? dbCloth.OldStyleNumber : dbCloth.StyleNumber;
                }
                if (!string.IsNullOrEmpty(StyleNumber) && dbCloth != null)
                {
                    newProduct = productService.ExecuteIdsQuery("Select * From Item where Name='" + StyleNumber + "'").FirstOrDefault();
                    if (newProduct == null)
                    {
                        newProduct = new Intuit.Ipp.Data.Item();
                        newProduct.SpecialItem = true;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.Name = dbCloth.StyleNumber;
                        newProduct.FullyQualifiedName = GetNames(dbCloth, 1);
                        newProduct.Active = true;
                        newProduct.PurchaseDesc = GetNames(dbCloth, 2);                        
                        string str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                        string noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();
                        newProduct.Description = noHTML;
                        newProduct.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                        newProduct.PurchaseCostSpecified = true;
                        newProduct.ActiveSpecified = true;
                        newProduct.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                           + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        newProduct.QtyOnHandSpecified = true;
                        //newProduct.Type = ItemTypeEnum.NonInventory;
                        newProduct.TrackQtyOnHand = true;
                        newProduct.InvStartDate = DateTime.UtcNow;
                        newProduct.InvStartDateSpecified = true;                       
                        newProduct.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                        newProduct.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                        newProduct.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };
                        service.Add<Intuit.Ipp.Data.Item>(newProduct);
                    }
                    else
                    {
                        newProduct.SpecialItem = true;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.Name = dbCloth.StyleNumber;
                        newProduct.FullyQualifiedName = GetNames(dbCloth, 1);
                        newProduct.Active = true;
                        newProduct.PurchaseDesc = GetNames(dbCloth, 2);                        
                        string str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                        string noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();
                        newProduct.Description = noHTML;
                        newProduct.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                        newProduct.UnitPriceSpecified = true;
                        newProduct.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                        newProduct.PurchaseCostSpecified = true;
                        newProduct.ActiveSpecified = true;
                        newProduct.ActiveSpecified = true;
                        newProduct.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                           + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        newProduct.QtyOnHandSpecified = true;
                        //newProduct.Type = ItemTypeEnum.NonInventory;
                        newProduct.TrackQtyOnHand = true;
                        newProduct.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                        newProduct.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                        newProduct.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };
                        service.Update<Intuit.Ipp.Data.Item>(newProduct);
                        var newItem = new Intuit.Ipp.Data.Item();
                        newItem = productService.ExecuteIdsQuery("Select * From Item where Name='" + dbCloth.StyleNumber + "'").FirstOrDefault();
                        if (newItem == null)
                        {
                            newItem = new Intuit.Ipp.Data.Item();
                            newItem.SpecialItem = true;
                            newItem.UnitPriceSpecified = true;
                            newItem.Name = dbCloth.StyleNumber;
                            newItem.FullyQualifiedName = GetNames(dbCloth, 1);
                            newItem.PurchaseDesc = GetNames(dbCloth, 2);
                            str = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                            noHTML = Regex.Replace(str, @"&lt;.+?&gt;|&nbsp;", "").Trim();
                            newItem.Description = noHTML;
                            newItem.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                            newItem.UnitPriceSpecified = true;
                            newItem.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                            newItem.PurchaseCostSpecified = true;
                            newItem.ActiveSpecified = true;
                            newItem.InvStartDate = DateTime.UtcNow;
                            newItem.InvStartDateSpecified = true;
                            //newItem.Type = ItemTypeEnum.Inventory;
                            newProduct.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                            + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                            newProduct.QtyOnHandSpecified = true;
                            newItem.TrackQtyOnHand = true;
                            newItem.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                            newItem.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                            newItem.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };
                            service.Add<Intuit.Ipp.Data.Item>(newItem);
                        }
                    }
                    dbCloth.OldStyleNumber = dbCloth.StyleNumber;
                    db.SaveChanges();
                }
            }
            catch { }
            return false;
        }

        public ActionResult PrintLineSheet(string sIds, string sA, string future, string deactive)
        {
            var document = new Document(PageSize.A3, 0, 0, 40, 20);
            var output = new MemoryStream();
            var writer = PdfWriter.GetInstance(document, output);
            string ContentType = "application/pdf";
            // writer.PageEvent = new Platini.Models.ITextEvents();
            writer.CloseStream = false;
            document.Open();
            int? page = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["page"] != null ? Convert.ToInt32(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["page"].ToString()) : 1;
            int? TypeId = SiteConfiguration.CatID;
            var inMode = SiteConfiguration.Mode != null ? SiteConfiguration.Mode : "Order";
            string Ids = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["Ids"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["Ids"].ToString() : string.Empty;
            string pS = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["PageSize"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["PageSize"].ToString() : string.Empty;
            string search = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["search"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["search"].ToString() : null;
            string sort = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["sortBy"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["sortBy"].ToString() : null;
            int sortBy = 1;
            int.TryParse(sort, out sortBy);
            int? PageSize1 = null;
            int val = 0;
            bool All = !string.IsNullOrEmpty(sA) && int.TryParse(sA, out val) && val > 0;
            if (!string.IsNullOrEmpty(pS) && !All)
            {
                int.TryParse(pS, out val);
                PageSize1 = val;
            }
            else if (All)
                PageSize1 = 0;
            if (!All && !string.IsNullOrEmpty(sIds))
                TempData["SelectIds"] = sIds;
            var _model = LineSheetData(page, TypeId, Ids, PageSize1, !string.IsNullOrEmpty(future), !string.IsNullOrEmpty(deactive), search, sortBy);
            if (inMode == "View")
                document = PlatiniWebService.PdfCreater(document, writer, _model, false);
            else if (inMode == "Order")
                document = PlatiniWebService.OrderMode_Pdf(document, writer, _model, false);
            document.Close();
            byte[] pdfBytes = new byte[output.Position];
            output.Position = 0;
            output.Read(pdfBytes, 0, pdfBytes.Length);
            Response.AppendHeader("Content-Disposition", "inline;filename=LineSheet.pdf");
            return File(pdfBytes, ContentType);
        }


        public ActionResult TestPdfCreater(Guid Id, string Message, string hP)
        {
            var _model = new Cart();
            // string id = "f651cc78-f4e8-3f33-9473-6ff4b9a77fc4";
            //string id = "ee29266b-e0c7-3187-9c84-fe23a511e0ba";
            //Guid gid = new Guid(id);
            var list = CartData(Id, true, false);
            var document = new Document(PageSize.A3, 0, 0, 20, 20);
            var output = new MemoryStream();
            var writer = PdfWriter.GetInstance(document, output);
            string ContentType = "application/pdf";
            // writer.PageEvent = new Platini.Models.ITextEvents();
            writer.CloseStream = false;
            document.Open();
            document = PlatiniWebService.CartPdfCreater(document, writer, list, !string.IsNullOrEmpty(hP), Message);
            document.Close();
            byte[] pdfBytes = new byte[output.Position];
            output.Position = 0;
            output.Read(pdfBytes, 0, pdfBytes.Length);
            Response.AppendHeader("Content-Disposition", "inline;filename=Cart.pdf");
            return File(pdfBytes, ContentType);
        }

        public ActionResult Cart(string from)
        {
            bool isCustomer = SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower();
            ViewBag.isCustomer = SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.User.ToString().ToLower();
            int UserId = 0;
            bool isCookie = string.IsNullOrEmpty(SiteIdentity.UserId);
            decimal Price = 0.0m;
            int StatusId = 0;
            List<string> Errors = new List<string>();
            Guid lastOrder = Guid.Empty;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            int.TryParse(SiteIdentity.UserId, out UserId);
            if (Session["Order"] == null && Session["EditingOrder"] == null && Session["WasCleared"] == null && !isCookie)
            {
                if (db.Orders.Where(x => x.AccountId == UserId && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).Count() > 0)
                    lastOrder = db.Orders.Where(x => x.AccountId == UserId && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault().OrderId;
            }
            else if (Session["EditingOrder"] != null)
                lastOrder = (Guid)Session["EditingOrder"];
            else if (Session["Order"] != null)
                lastOrder = (Guid)Session["Order"];
            else if (isCookie)
            {
                var ordCookie = Request.Cookies["ordCookie"];
                isCustomer = true;
                if (ordCookie != null)
                {
                    try
                    {
                        var cookieCart = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                        lastOrder = cookieCart.OrderId;

                    }
                    catch
                    {
                    }
                }
            }
            if (lastOrder != Guid.Empty && !isCookie)
                Session["Order"] = lastOrder;
            ViewBag.CookieMode = isCookie;
            ViewBag.CustomerMode = isCustomer;
            var retModel = CartData(lastOrder, false, isCookie);
            if (retModel == null)
            {
                retModel = new Cart();
                retModel.CartOwner = new CartOwner();
                retModel.Clothes = new List<CartCloth>();
                retModel.Discount = 0;
                retModel.FinalAmount = 0;
                retModel.GrandTotal = 0;
                retModel.isSubmit = false;
                retModel.OrderId = Guid.Empty;
                retModel.OrdNum = "";
                retModel.Note = "";
                retModel.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                retModel.TotalQty = 0;
                retModel.UserId = 0;
                if (isCustomer)
                {
                    var account = db.Accounts.Find(UserId);
                    if (account != null)
                    {
                        retModel.UserId = UserId;
                        retModel.CartOwner = Data(account, null);
                        retModel.Discount = retModel.CartOwner.Discount;
                    }
                }
                retModel.Tags = db.OrderTags.Where(x => x.IsActive == true && x.IsDelete == false).ToList().Select(x => new SelectedListValues() { Id = x.OrderTagId, Value = x.Name, IsSelected = false });
                retModel.Shipping = db.ShipVias.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.ShipViaId, Value = x.Name, IsSelected = false });
                retModel.Terms = db.Terms.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.TermId, Value = x.Name, IsSelected = false });
            }
            if (!string.IsNullOrEmpty(from))
                ViewBag.LoadNew = true;
            else
                ViewBag.LoadNew = false;
            if (!retModel.CartOwner.isRetail)
                return PartialView(retModel);
            else
                return PartialView("CartRetail", retModel);
        }

        public ActionResult PrintCart(Guid Id, string Message, string hp)
        {
            var retModel = CartData(Id, true, false);
            if (retModel != null)
            {
                ViewBag.Message = Message;
                ViewBag.HidePice = !string.IsNullOrEmpty(hp);
                return View(retModel);
            }
            return RedirectToAction("Index");
        }

        public ActionResult PrintThisCart1(Guid Id)
        {
            string URL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/PrintCart/" + Id;
            byte[] toPdf = PlatiniWebService.CreatePdf(URL);
            string ContentType = "application/pdf";
            string FileName = "Cart.pdf";
            Response.AppendHeader("Content-Disposition", "inline;filename=" + FileName);
            return File(toPdf, ContentType);
        }

        public ActionResult PrintThisCart(Guid Id, string Message, string HidePrice)
        {

            byte[] toPdf = null;
            bool isCookie = string.IsNullOrEmpty(SiteIdentity.UserId);
            var list = CartData(Id, true, isCookie);
            var document = new Document(PageSize.A3, 0, 0, 20, 20);
            using (var output = new MemoryStream())
            using (var writer = PdfWriter.GetInstance(document, output))
            {
                writer.CloseStream = false;
                document.Open();
                document = PlatiniWebService.CartPdfCreater(document, writer, list, !string.IsNullOrEmpty(HidePrice), Message);
                document.Close();
                toPdf = new byte[output.Position];
                output.Position = 0;
                output.Read(toPdf, 0, toPdf.Length);
                document.Dispose();
            }
            if (toPdf != null)
            {
                string ContentType = "application/pdf";
                string FileName = string.Format("Cart-{0}.pdf", Id);
                Response.AppendHeader("Content-Disposition", "inline;filename=" + FileName);
                return File(toPdf, ContentType);
            }
            else
                return RedirectToAction("Index");
        }

        public ActionResult MailThisCart(Guid Id, string Ids, string Subject, string Message)
        {
            string URL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/PrintCart/" + Id;
            try
            {
                byte[] toPdf = null;
                var list = CartData(Id, true, false);
                var document = new Document(PageSize.A3, 0, 0, 20, 20);
                using (var output = new MemoryStream())
                using (var writer = PdfWriter.GetInstance(document, output))
                {
                    writer.CloseStream = false;
                    document.Open();
                    document = PlatiniWebService.CartPdfCreater(document, writer, list, false, "");
                    document.Close();
                    toPdf = new byte[output.Position];
                    output.Position = 0;
                    output.Read(toPdf, 0, toPdf.Length);
                    document.Dispose();
                }
                if (toPdf != null)
                {
                    var emails = Ids.Split(',').ToList();
                    if (emails.Count() > 0)
                    {
                        foreach (var email in emails)
                        {
                            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^([\w-\.]+@([\w-]+\.)+[\w-]{2,4})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                emails.Remove(email);
                        }
                    }
                    EmailManager.SendLineSheet(Subject, Message, toPdf, ConfigurationManager.AppSettings["SMTPEmail"].ToString(), emails.ToArray());
                    return Json("Success", JsonRequestBehavior.AllowGet);
                }
                else
                    return Json("The email could not be sent.  Please try again.", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("The email could not be sent.  Please try again.", JsonRequestBehavior.AllowGet);
            }
        }

        [NonAction]
        public Cart CartData(Guid Id, bool isPrint, bool isCookie)
        {
            decimal Price = 0.0m;
            DB.Order lastOrder = null;
            if (!isCookie)
                lastOrder = db.Orders.Find(Id);
            else
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                    }
                    catch
                    {
                        lastOrder = null;
                    }
                }
            }
            if (lastOrder != null)
            {
                var account = db.Accounts.Find(lastOrder.AccountId);
                var retModel = new Cart();
                retModel.CartOwner = new CartOwner();
                List<CustomerItemPrice> Prices = new List<CustomerItemPrice>(); ;
                if (account != null)
                {
                    Prices = account.CustomerItemPrices.ToList();
                    if (account != null)
                    {
                        if (account.IsActive == true && account.IsDelete == false)
                        {
                            retModel.CartOwner = Data(account, isPrint ? lastOrder.OrderId : (Guid?)null);
                        }
                    }
                }
                if (isCookie)
                {
                    retModel.CartOwner.isRetail = true;
                }
                retModel.UserId = lastOrder.AccountId;
                retModel.OrderId = lastOrder.OrderId;
                retModel.TagId = lastOrder.TagId.HasValue ? lastOrder.TagId.Value : db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                retModel.Tags = db.OrderTags.Where(x => x.IsActive == true && x.IsDelete == false).ToList().Select(x => new SelectedListValues() { Id = x.OrderTagId, Value = x.Name, IsSelected = false });
                retModel.Shipping = db.ShipVias.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.ShipViaId, Value = x.Name, IsSelected = false });
                retModel.Terms = db.Terms.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.TermId, Value = x.Name, IsSelected = false });
                retModel.TotalQty = lastOrder.OriginalQty.HasValue ? lastOrder.OriginalQty.Value : 0;
                //string PhoneNumber = string.Empty;
                //if (!string.IsNullOrEmpty(retModel.CartOwner.Phone))
                //{
                //    PhoneNumber = retModel.CartOwner.Phone.Remove(0, 2);
                //    if (PhoneNumber.Length > 7)
                //    {
                //        PhoneNumber = PhoneNumber.Remove(7, 1);
                //        retModel.CartOwner.Phone = String.Format("{0:(###) ###-####}", Convert.ToInt64(PhoneNumber));
                //    }
                //}

                var clothes = new List<Cloth>(lastOrder.OrderScales.Count + lastOrder.OrderSizes.Count);
                if (!isCookie)
                {
                    clothes.AddRange(lastOrder.OrderScales.Select(x => x.ClothesScale.Cloth));
                    clothes.AddRange(lastOrder.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                }
                else
                {
                    var tempC = lastOrder.OrderSizes.Select(x => x.ClothesId);
                    clothes.AddRange(db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && tempC.Contains(x.ClothesId)).ToList());
                }
                clothes = clothes.Distinct().ToList();
                var list = clothes.GroupBy(x => x.CategoryId);
                var mFitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                var mInseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                foreach (var item in list)
                {
                    var cartItem = new CartCloth();
                    cartItem.GroupName = GroupName(item.Key);
                    foreach (var cloth in item.ToList())
                    {
                        var contents = new CartContents();
                        contents.ClothesId = cloth.ClothesId;
                        contents.StyleNumber = cloth.StyleNumber;
                        var tempDate = new DateTime(1900, 1, 1);
                        contents.Delivery = (cloth.FutureDeliveryDate.HasValue ? (cloth.FutureDeliveryDate.Value > tempDate ? cloth.FutureDeliveryDate.Value.ToString("MM/dd/yyyy") : "AO") : "AO");
                        if (Prices.Count > 0)
                        {
                            contents.Price = Prices.Find(x => x.ClothesId == cloth.ClothesId) != null ? (Prices.Find(x => x.ClothesId == cloth.ClothesId).Price.HasValue ? Prices.Find(x => x.ClothesId == cloth.ClothesId).Price.Value : 0.0m) : 0.0m;
                        }
                        if (contents.Price == 0)
                        {
                            if (retModel.CartOwner.isRetail)
                            {
                                if (cloth.DiscountedMSRP != null && (cloth.DiscountedMSRP > 0 && cloth.DiscountedMSRP < cloth.MSRP))
                                    contents.Price = cloth.DiscountedMSRP.HasValue ? cloth.DiscountedMSRP.Value : 0.0m;
                                else
                                    contents.Price = cloth.MSRP.HasValue ? cloth.MSRP.Value : 0.0m;
                            }
                            else
                            {
                                if (cloth.DiscountedPrice != null && (cloth.DiscountedPrice > 0 && cloth.DiscountedPrice < cloth.MSRP))
                                    contents.Price = cloth.DiscountedPrice.HasValue ? cloth.DiscountedPrice.Value : 0.0m;
                                else
                                    contents.Price = cloth.Price.HasValue ? cloth.Price.Value : 0.0m;
                            }
                        }
                        if (cloth.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                            contents.Image = cloth.ClothesImages.OrderBy(x => x.SortOrder).FirstOrDefault(x => x.IsActive && !x.IsDelete).ImagePath;
                        else
                            contents.Image = "";

                        var scaleList = lastOrder.OrderScales.Where(x => x.ClothesScale.ClothesId == cloth.ClothesId);
                        foreach (var scale in scaleList)
                        {
                            if (scale.Quantity.HasValue ? (scale.Quantity.Value >= 0) : false)
                            {
                                var SP = new ContentDetails();
                                var fit = mFitList.FirstOrDefault(x => scale.ClothesScale.FitId.HasValue ? (x.Id == scale.ClothesScale.FitId.Value) : false);
                                var inseam = mInseamList.FirstOrDefault(x => scale.ClothesScale.InseamId.HasValue ? (x.Id == scale.ClothesScale.InseamId.Value) : false);
                                SP.Fit = fit != null ? fit.Value : "";
                                SP.Inseam = inseam != null ? inseam.Value : "";
                                SP.FitId = fit != null ? fit.Id : 0;
                                SP.InseamId = inseam != null ? inseam.Id : 0;
                                SP.Pack = new ClothesScaleClass();
                                SP.Pack.ClothesId = cloth.ClothesId;
                                SP.Pack.ClothesScaleId = scale.ClothesScaleId;
                                SP.Pack.OrderSSId = scale.OrderScaleId;
                                SP.Pack.InvQty = scale.ClothesScale.InvQty.HasValue ? scale.ClothesScale.InvQty.Value : 0;
                                SP.Pack.PurchasedQty = scale.Quantity.HasValue ? scale.Quantity.Value : 0;
                                SP.Pack.QuantSum = scale.ClothesScale.ClothesScaleSizes.Sum(x => x.Quantity);
                                SP.Pack.isConfirm = scale.IsConfirmed.HasValue ? scale.IsConfirmed.Value : false;
                                SP.Pack.Name = scale.ClothesScale.Name;
                                if (retModel.CartOwner.isRetail)
                                {
                                    var sSQPrePacks = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                    SP.Pack.ClothesScaleSizeClass = new List<ClothesScaleSizeClass>();
                                    foreach (var sizePP in sSQPrePacks)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = sizePP.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = sizePP.ClothesScaleId;
                                        scaleRow.SizeId = sizePP.SizeId;
                                        scaleRow.SizeName = sizePP.Size.Name;
                                        scaleRow.Quantity = sizePP.Quantity;
                                        SP.Pack.ClothesScaleSizeClass.Add(scaleRow);
                                    }
                                }
                                contents.SPs.Add(SP);
                            }
                        }
                        contents.SPs = contents.SPs.OrderBy(x => x.Pack.Name).ToList();

                        if (!isCookie)
                        {
                            var sizeList = lastOrder.OrderSizes.Where(x => x.ClothesScaleSize.ClothesScale.ClothesId == cloth.ClothesId).GroupBy(x => x.ClothesScaleSize.ClothesScale);
                            foreach (var size in sizeList)
                            {
                                var fit = mFitList.FirstOrDefault(x => size.Key.FitId.HasValue ? (x.Id == size.Key.FitId.Value) : false);
                                var inseam = mInseamList.FirstOrDefault(x => size.Key.InseamId.HasValue ? (x.Id == size.Key.InseamId.Value) : false);
                                int FitId = fit != null ? fit.Id : 0;
                                int InseamId = inseam != null ? inseam.Id : 0;
                                if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId))
                                {
                                    if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId && x.OpenSizes.Count > 0))
                                    {

                                    }
                                    else
                                    {
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                        foreach (var os in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = os.ClothesScaleId;
                                            scaleRow.OrderSSId = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).OrderSizeId : Guid.Empty;
                                            scaleRow.SizeId = os.SizeId;
                                            scaleRow.SizeName = os.Size.Name;
                                            scaleRow.Quantity = os.Quantity;
                                            if (retModel.CartOwner.isRetail)
                                            {
                                                scaleRow.RtlAvlbl = true;
                                                if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                    if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                                    else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                        SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                            }
                                            scaleRow.isConfirm = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.HasValue ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.Value : false) : false;
                                            scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).Quantity : 0;
                                            var OpenQty = db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity != null ? db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity : 0;
                                            scaleRow.TotalInventory = OpenQty + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * OpenQty));
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        contents.SPs.FirstOrDefault(x => x.FitId == FitId && x.InseamId == InseamId).OpenSizes.AddRange(openSizesForOS);

                                    }
                                }
                                else
                                {
                                    var SP = new ContentDetails();
                                    SP.Fit = fit != null ? fit.Value : "";
                                    SP.Inseam = inseam != null ? inseam.Value : "";
                                    SP.FitId = fit != null ? fit.Id : 0;
                                    SP.InseamId = inseam != null ? inseam.Id : 0;
                                    SP.Pack = null;
                                    var openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                    foreach (var os in sSQOpenSize)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = os.ClothesScaleId;
                                        scaleRow.OrderSSId = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).OrderSizeId : Guid.Empty;
                                        scaleRow.SizeId = os.SizeId;
                                        scaleRow.SizeName = os.Size.Name;
                                        scaleRow.Quantity = os.Quantity;
                                        if (retModel.CartOwner.isRetail)
                                        {
                                            scaleRow.RtlAvlbl = true;
                                            if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                                else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                    SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                        }
                                        scaleRow.isConfirm = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.HasValue ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.Value : false) : false;
                                        scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).Quantity : 0;
                                        var OpenQty = db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity != null ? db.ClothesScaleSizes.Where(x => x.ClothesScaleSizeId == scaleRow.ClothesScaleSizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault().Quantity : 0;
                                        scaleRow.TotalInventory = OpenQty + (cloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * OpenQty));
                                        openSizesForOS.Add(scaleRow);
                                    }
                                    SP.OpenSizes.AddRange(openSizesForOS);
                                    contents.SPs.Add(SP);
                                }
                            }
                        }
                        else
                        {
                            var sizeList = lastOrder.OrderSizes.Where(x => x.ClothesId == cloth.ClothesId).
                                 SelectMany(x => db.ClothesScaleSizes.Where(y => y.ClothesScaleSizeId == x.ClothesSizeId), (x, y) => new { x, y }).ToList().
                                 SelectMany(x => db.ClothesScales.Where(z => z.ClothesScaleId == x.y.ClothesScaleId), (x, z) => new { x.x, z }).GroupBy(x => x.z);
                            foreach (var size in sizeList)
                            {
                                var fit = mFitList.FirstOrDefault(x => size.Key.FitId.HasValue ? (x.Id == size.Key.FitId.Value) : false);
                                var inseam = mInseamList.FirstOrDefault(x => size.Key.InseamId.HasValue ? (x.Id == size.Key.InseamId.Value) : false);
                                int FitId = fit != null ? fit.Id : 0;
                                int InseamId = inseam != null ? inseam.Id : 0;
                                if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId))
                                {
                                    if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId && x.OpenSizes.Count > 0))
                                    {

                                    }
                                    else
                                    {
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                        foreach (var os in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = os.ClothesScaleId;
                                            scaleRow.OrderSSId = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.OrderSizeId : Guid.Empty;
                                            scaleRow.SizeId = os.SizeId;
                                            scaleRow.SizeName = os.Size.Name;
                                            scaleRow.Quantity = os.Quantity;
                                            if (retModel.CartOwner.isRetail)
                                            {
                                                scaleRow.RtlAvlbl = true;
                                                if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                    if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                                    else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                        SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                            }
                                            scaleRow.isConfirm = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.HasValue ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.Value : false) : false;
                                            scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.Quantity : 0;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        contents.SPs.FirstOrDefault(x => x.FitId == FitId && x.InseamId == InseamId).OpenSizes.AddRange(openSizesForOS);

                                    }
                                }
                                else
                                {
                                    var SP = new ContentDetails();
                                    SP.Fit = fit != null ? fit.Value : "";
                                    SP.Inseam = inseam != null ? inseam.Value : "";
                                    SP.FitId = fit != null ? fit.Id : 0;
                                    SP.InseamId = inseam != null ? inseam.Id : 0;
                                    SP.Pack = null;
                                    var openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                    foreach (var os in sSQOpenSize)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = os.ClothesScaleId;
                                        scaleRow.OrderSSId = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.OrderSizeId : Guid.Empty;
                                        scaleRow.SizeId = os.SizeId;
                                        scaleRow.SizeName = os.Size.Name;
                                        scaleRow.Quantity = os.Quantity;
                                        if (retModel.CartOwner.isRetail)
                                        {
                                            scaleRow.RtlAvlbl = true;
                                            if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                                else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                    SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                        }
                                        scaleRow.isConfirm = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.HasValue ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.Value : false) : false;
                                        scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.Quantity : 0;
                                        openSizesForOS.Add(scaleRow);
                                    }
                                    SP.OpenSizes.AddRange(openSizesForOS);
                                    contents.SPs.Add(SP);
                                }
                            }
                        }

                        for (int i = 0; i < contents.SPs.Count(); ++i)
                        {
                            contents.SPs[i].isConfirmed = !(contents.SPs[i].OpenSizes.Any(x => x.isConfirm == false && x.OrderSSId != Guid.Empty));
                            int temp = 0;
                            if (contents.SPs[i].Pack != null && !retModel.CartOwner.isRetail)
                            {
                                temp = contents.SPs[i].Pack.PurchasedQty.Value * contents.SPs[i].Pack.QuantSum.Value;
                                contents.SPs[i].isConfirmed = contents.SPs[i].isConfirmed && contents.SPs[i].Pack.isConfirm;
                            }
                            else
                                temp = 0;
                            if (contents.SPs[i].OpenSizes.Count == 0)
                            {
                                contents.SPs[i].Quantity = temp;
                                if (contents.SPs[i].Pack != null)
                                {
                                    int sFit = contents.SPs[i].FitId, sInseam = contents.SPs[i].InseamId;
                                    var oScale = db.ClothesScales.FirstOrDefault(x => x.ClothesId == contents.ClothesId && (x.Fit != null ? x.FitId == sFit : true) && (x.InseamId != null ? x.InseamId == sInseam : true) && x.IsOpenSize == true);
                                    if (oScale != null)
                                    {
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        int clothesScaleId = oScale.ClothesId;
                                        var sSQOpenSize = oScale.ClothesScaleSizes.Where(x => x.IsActive == true && x.IsDelete == false);
                                        foreach (var os in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = os.ClothesScaleId;
                                            scaleRow.OrderSSId = Guid.Empty;
                                            scaleRow.SizeId = os.SizeId;
                                            scaleRow.SizeName = os.Size.Name;
                                            scaleRow.Quantity = os.Quantity;
                                            scaleRow.isConfirm = false;
                                            if (retModel.CartOwner.isRetail)
                                            {
                                                scaleRow.RtlAvlbl = true;
                                                if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                    if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == sFit && (x.InseamId.HasValue ? x.InseamId.Value : 0) == sInseam && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                                    else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == sFit && (x.InseamId.HasValue ? x.InseamId.Value : 0) == sInseam && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                        SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                            }
                                            scaleRow.PurchasedQuantity = 0;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        contents.SPs[i].OpenSizes.AddRange(openSizesForOS);
                                    }
                                }
                            }
                            else
                            {
                                temp += contents.SPs[i].OpenSizes.Sum(x => x.PurchasedQuantity).Value;
                                contents.SPs[i].Quantity = temp;
                            }
                            contents.SPs[i].Total = temp * contents.Price;
                            bool checkfit = contents.SPs[i].FitId > 0;
                            bool checkins = contents.SPs[i].InseamId > 0;
                            contents.SPs[i].ShowPrepack = cloth.ClothesScales.Any(x => (checkfit ? x.FitId == contents.SPs[i].FitId : true) && (checkins ? x.InseamId == contents.SPs[i].InseamId : true) &&
                                x.IsActive == true && x.IsDelete == false && x.IsOpenSize == false && x.InvQty > 0);
                        }
                        Price += contents.SPs.Sum(x => x.Total);
                        cartItem.Contents.Add(contents);
                    }
                    retModel.Clothes.Add(cartItem);
                }

                lastOrder.GrandTotal = Price;
                if (retModel.CartOwner.UserId > 0)
                    lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * (retModel.CartOwner.Discount / 100)) + (lastOrder.ShippingCost.HasValue ? lastOrder.ShippingCost.Value : 0);
                else
                    lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * ((lastOrder.Discount.HasValue ? lastOrder.Discount.Value : 0) / 100)) + (lastOrder.ShippingCost.HasValue ? lastOrder.ShippingCost.Value : 0);
                lastOrder.FinalAmount = Math.Truncate(100 * (lastOrder.FinalAmount.HasValue ? lastOrder.FinalAmount.Value : 0)) / 100;
                
                if (!isPrint && !isCookie)
                    db.SaveChanges();
                retModel.OrdNum = lastOrder.OrderNumber;
                retModel.GrandTotal = Price;
                retModel.isSubmit = lastOrder.SubmittedOn.HasValue;
                retModel.ShippingAmount = lastOrder.ShippingCost.HasValue ? lastOrder.ShippingCost.Value : 0;
                if (retModel.CartOwner.UserId > 0)
                    retModel.Discount = retModel.CartOwner.Discount;
                else
                    retModel.Discount = lastOrder.Discount.HasValue ? lastOrder.Discount.Value : 0.0m;
                retModel.Note = lastOrder.Note;
                retModel.FinalAmount = lastOrder.FinalAmount.HasValue ? lastOrder.FinalAmount.Value : Price;
                return retModel;
            }
            return null;

        }

        [NonAction]
        public Cart CartDataWithOutPrice(Guid Id, bool isPrint, bool isCookie)
        {
            decimal Price = 0.0m;
            DB.Order lastOrder = null;
            if (!isCookie)
                lastOrder = db.Orders.Find(Id);
            else
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                    }
                    catch
                    {
                        lastOrder = null;
                    }
                }
            }
            if (lastOrder != null)
            {
                var account = db.Accounts.Find(lastOrder.AccountId);
                var retModel = new Cart();
                retModel.CartOwner = new CartOwner();
                List<CustomerItemPrice> Prices = new List<CustomerItemPrice>(); ;
                if (account != null)
                {
                    Prices = account.CustomerItemPrices.ToList();
                    if (account != null)
                    {
                        if (account.IsActive == true && account.IsDelete == false)
                        {
                            retModel.CartOwner = Data(account, isPrint ? lastOrder.OrderId : (Guid?)null);
                        }
                    }
                }
                if (isCookie)
                {
                    retModel.CartOwner.isRetail = true;
                }
                retModel.UserId = lastOrder.AccountId;
                retModel.OrderId = lastOrder.OrderId;
                retModel.TagId = lastOrder.TagId.HasValue ? lastOrder.TagId.Value : db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                retModel.Tags = db.OrderTags.Where(x => x.IsActive == true && x.IsDelete == false).ToList().Select(x => new SelectedListValues() { Id = x.OrderTagId, Value = x.Name, IsSelected = false });
                retModel.Shipping = db.ShipVias.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.ShipViaId, Value = x.Name, IsSelected = false });
                retModel.Terms = db.Terms.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.TermId, Value = x.Name, IsSelected = false });
                retModel.TotalQty = lastOrder.OriginalQty.HasValue ? lastOrder.OriginalQty.Value : 0;
                var clothes = new List<Cloth>(lastOrder.OrderScales.Count + lastOrder.OrderSizes.Count);
                if (!isCookie)
                {
                    clothes.AddRange(lastOrder.OrderScales.Select(x => x.ClothesScale.Cloth));
                    clothes.AddRange(lastOrder.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                }
                else
                {
                    var tempC = lastOrder.OrderSizes.Select(x => x.ClothesId);
                    clothes.AddRange(db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && tempC.Contains(x.ClothesId)).ToList());
                }
                clothes = clothes.Distinct().ToList();
                var list = clothes.GroupBy(x => x.CategoryId);
                var mFitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                var mInseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                foreach (var item in list)
                {
                    var cartItem = new CartCloth();
                    cartItem.GroupName = GroupName(item.Key);
                    foreach (var cloth in item.ToList())
                    {
                        var contents = new CartContents();
                        contents.ClothesId = cloth.ClothesId;
                        contents.StyleNumber = cloth.StyleNumber;
                        var tempDate = new DateTime(1900, 1, 1);
                        contents.Delivery = (cloth.FutureDeliveryDate.HasValue ? (cloth.FutureDeliveryDate.Value > tempDate ? cloth.FutureDeliveryDate.Value.ToString("MM/dd/yyyy") : "AO") : "AO");
                        if (Prices.Count > 0)
                        {
                            contents.Price = Prices.Find(x => x.ClothesId == cloth.ClothesId) != null ? (Prices.Find(x => x.ClothesId == cloth.ClothesId).Price.HasValue ? Prices.Find(x => x.ClothesId == cloth.ClothesId).Price.Value : 0.0m) : 0.0m;
                        }
                        if (contents.Price == 0)
                        {
                            if (retModel.CartOwner.isRetail)
                            {
                                if (cloth.DiscountedMSRP != null && (cloth.DiscountedMSRP > 0 && cloth.DiscountedMSRP < cloth.MSRP))
                                    contents.Price = cloth.DiscountedMSRP.HasValue ? cloth.DiscountedMSRP.Value : 0.0m;
                                else
                                    contents.Price = cloth.MSRP.HasValue ? cloth.MSRP.Value : 0.0m;
                            }
                            else
                            {
                                if (cloth.DiscountedPrice != null && (cloth.DiscountedPrice > 0 && cloth.DiscountedPrice < cloth.MSRP))
                                    contents.Price = cloth.DiscountedPrice.HasValue ? cloth.DiscountedPrice.Value : 0.0m;
                                else
                                    contents.Price = cloth.Price.HasValue ? cloth.Price.Value : 0.0m;
                            }
                        }
                        if (cloth.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                            contents.Image = cloth.ClothesImages.OrderBy(x => x.SortOrder).FirstOrDefault(x => x.IsActive && !x.IsDelete).ImagePath;
                        else
                            contents.Image = "";

                        var scaleList = lastOrder.OrderScales.Where(x => x.ClothesScale.ClothesId == cloth.ClothesId);
                        foreach (var scale in scaleList)
                        {
                            if (scale.Quantity.HasValue ? (scale.Quantity.Value > 0) : false)
                            {
                                var SP = new ContentDetails();
                                var fit = mFitList.FirstOrDefault(x => scale.ClothesScale.FitId.HasValue ? (x.Id == scale.ClothesScale.FitId.Value) : false);
                                var inseam = mInseamList.FirstOrDefault(x => scale.ClothesScale.InseamId.HasValue ? (x.Id == scale.ClothesScale.InseamId.Value) : false);
                                SP.Fit = fit != null ? fit.Value : "";
                                SP.Inseam = inseam != null ? inseam.Value : "";
                                SP.FitId = fit != null ? fit.Id : 0;
                                SP.InseamId = inseam != null ? inseam.Id : 0;
                                SP.Pack = new ClothesScaleClass();
                                SP.Pack.ClothesId = cloth.ClothesId;
                                SP.Pack.ClothesScaleId = scale.ClothesScaleId;
                                SP.Pack.OrderSSId = scale.OrderScaleId;
                                SP.Pack.InvQty = scale.ClothesScale.InvQty.HasValue ? scale.ClothesScale.InvQty.Value : 0;
                                SP.Pack.PurchasedQty = scale.Quantity.HasValue ? scale.Quantity.Value : 0;
                                SP.Pack.QuantSum = scale.ClothesScale.ClothesScaleSizes.Sum(x => x.Quantity);
                                SP.Pack.isConfirm = scale.IsConfirmed.HasValue ? scale.IsConfirmed.Value : false;
                                SP.Pack.Name = scale.ClothesScale.Name;
                                if (retModel.CartOwner.isRetail)
                                {
                                    var sSQPrePacks = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                    SP.Pack.ClothesScaleSizeClass = new List<ClothesScaleSizeClass>();
                                    foreach (var sizePP in sSQPrePacks)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = sizePP.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = sizePP.ClothesScaleId;
                                        scaleRow.SizeId = sizePP.SizeId;
                                        scaleRow.SizeName = sizePP.Size.Name;
                                        scaleRow.Quantity = sizePP.Quantity;
                                        SP.Pack.ClothesScaleSizeClass.Add(scaleRow);
                                    }
                                }
                                contents.SPs.Add(SP);
                            }
                        }
                        contents.SPs = contents.SPs.OrderBy(x => x.Pack.Name).ToList();

                        if (!isCookie)
                        {
                            var sizeList = lastOrder.OrderSizes.Where(x => x.ClothesScaleSize.ClothesScale.ClothesId == cloth.ClothesId).GroupBy(x => x.ClothesScaleSize.ClothesScale);
                            foreach (var size in sizeList)
                            {
                                var fit = mFitList.FirstOrDefault(x => size.Key.FitId.HasValue ? (x.Id == size.Key.FitId.Value) : false);
                                var inseam = mInseamList.FirstOrDefault(x => size.Key.InseamId.HasValue ? (x.Id == size.Key.InseamId.Value) : false);
                                int FitId = fit != null ? fit.Id : 0;
                                int InseamId = inseam != null ? inseam.Id : 0;
                                if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId))
                                {
                                    if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId && x.OpenSizes.Count > 0))
                                    {

                                    }
                                    else
                                    {
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                        foreach (var os in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = os.ClothesScaleId;
                                            scaleRow.OrderSSId = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).OrderSizeId : Guid.Empty;
                                            scaleRow.SizeId = os.SizeId;
                                            scaleRow.SizeName = os.Size.Name;
                                            scaleRow.Quantity = os.Quantity;
                                            if (retModel.CartOwner.isRetail)
                                            {
                                                scaleRow.RtlAvlbl = true;
                                                if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                    if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                                    else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                        SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                            }
                                            scaleRow.isConfirm = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.HasValue ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.Value : false) : false;
                                            scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).Quantity : 0;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        contents.SPs.FirstOrDefault(x => x.FitId == FitId && x.InseamId == InseamId).OpenSizes.AddRange(openSizesForOS);

                                    }
                                }
                                else
                                {
                                    var SP = new ContentDetails();
                                    SP.Fit = fit != null ? fit.Value : "";
                                    SP.Inseam = inseam != null ? inseam.Value : "";
                                    SP.FitId = fit != null ? fit.Id : 0;
                                    SP.InseamId = inseam != null ? inseam.Id : 0;
                                    SP.Pack = null;
                                    var openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                    foreach (var os in sSQOpenSize)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = os.ClothesScaleId;
                                        scaleRow.OrderSSId = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).OrderSizeId : Guid.Empty;
                                        scaleRow.SizeId = os.SizeId;
                                        scaleRow.SizeName = os.Size.Name;
                                        scaleRow.Quantity = os.Quantity;
                                        if (retModel.CartOwner.isRetail)
                                        {
                                            scaleRow.RtlAvlbl = true;
                                            if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                                else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                    SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                        }
                                        scaleRow.isConfirm = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.HasValue ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).IsConfirmed.Value : false) : false;
                                        scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.ClothesSizeId == os.ClothesScaleSizeId).Quantity : 0;
                                        openSizesForOS.Add(scaleRow);
                                    }
                                    SP.OpenSizes.AddRange(openSizesForOS);
                                    contents.SPs.Add(SP);
                                }
                            }
                        }
                        else
                        {
                            var sizeList = lastOrder.OrderSizes.Where(x => x.ClothesId == cloth.ClothesId).
                                 SelectMany(x => db.ClothesScaleSizes.Where(y => y.ClothesScaleSizeId == x.ClothesSizeId), (x, y) => new { x, y }).ToList().
                                 SelectMany(x => db.ClothesScales.Where(z => z.ClothesScaleId == x.y.ClothesScaleId), (x, z) => new { x.x, z }).GroupBy(x => x.z);
                            foreach (var size in sizeList)
                            {
                                var fit = mFitList.FirstOrDefault(x => size.Key.FitId.HasValue ? (x.Id == size.Key.FitId.Value) : false);
                                var inseam = mInseamList.FirstOrDefault(x => size.Key.InseamId.HasValue ? (x.Id == size.Key.InseamId.Value) : false);
                                int FitId = fit != null ? fit.Id : 0;
                                int InseamId = inseam != null ? inseam.Id : 0;
                                if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId))
                                {
                                    if (contents.SPs.Any(x => x.FitId == FitId && x.InseamId == InseamId && x.OpenSizes.Count > 0))
                                    {

                                    }
                                    else
                                    {
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                        foreach (var os in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = os.ClothesScaleId;
                                            scaleRow.OrderSSId = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.OrderSizeId : Guid.Empty;
                                            scaleRow.SizeId = os.SizeId;
                                            scaleRow.SizeName = os.Size.Name;
                                            scaleRow.Quantity = os.Quantity;
                                            if (retModel.CartOwner.isRetail)
                                            {
                                                scaleRow.RtlAvlbl = true;
                                                if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                    if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                                    else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                        SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                            }
                                            scaleRow.isConfirm = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.HasValue ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.Value : false) : false;
                                            scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.Quantity : 0;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        contents.SPs.FirstOrDefault(x => x.FitId == FitId && x.InseamId == InseamId).OpenSizes.AddRange(openSizesForOS);

                                    }
                                }
                                else
                                {
                                    var SP = new ContentDetails();
                                    SP.Fit = fit != null ? fit.Value : "";
                                    SP.Inseam = inseam != null ? inseam.Value : "";
                                    SP.FitId = fit != null ? fit.Id : 0;
                                    SP.InseamId = inseam != null ? inseam.Id : 0;
                                    SP.Pack = null;
                                    var openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
                                    foreach (var os in sSQOpenSize)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = os.ClothesScaleId;
                                        scaleRow.OrderSSId = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.OrderSizeId : Guid.Empty;
                                        scaleRow.SizeId = os.SizeId;
                                        scaleRow.SizeName = os.Size.Name;
                                        scaleRow.Quantity = os.Quantity;
                                        if (retModel.CartOwner.isRetail)
                                        {
                                            scaleRow.RtlAvlbl = true;
                                            if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                                else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                    SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                        }
                                        scaleRow.isConfirm = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? (size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.HasValue ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.IsConfirmed.Value : false) : false;
                                        scaleRow.PurchasedQuantity = size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId) != null ? size.FirstOrDefault(x => x.x.ClothesSizeId == os.ClothesScaleSizeId).x.Quantity : 0;
                                        openSizesForOS.Add(scaleRow);
                                    }
                                    SP.OpenSizes.AddRange(openSizesForOS);
                                    contents.SPs.Add(SP);
                                }
                            }
                        }

                        for (int i = 0; i < contents.SPs.Count(); ++i)
                        {
                            contents.SPs[i].isConfirmed = !(contents.SPs[i].OpenSizes.Any(x => x.isConfirm == false && x.OrderSSId != Guid.Empty));
                            int temp = 0;
                            if (contents.SPs[i].Pack != null && !retModel.CartOwner.isRetail)
                            {
                                temp = contents.SPs[i].Pack.PurchasedQty.Value * contents.SPs[i].Pack.QuantSum.Value;
                                contents.SPs[i].isConfirmed = contents.SPs[i].isConfirmed && contents.SPs[i].Pack.isConfirm;
                            }
                            else
                                temp = 0;
                            if (contents.SPs[i].OpenSizes.Count == 0)
                            {
                                contents.SPs[i].Quantity = temp;
                                if (contents.SPs[i].Pack != null)
                                {
                                    int sFit = contents.SPs[i].FitId, sInseam = contents.SPs[i].InseamId;
                                    var oScale = db.ClothesScales.FirstOrDefault(x => x.ClothesId == contents.ClothesId && (x.Fit != null ? x.FitId == sFit : true) && (x.InseamId != null ? x.InseamId == sInseam : true) && x.IsOpenSize == true);
                                    if (oScale != null)
                                    {
                                        var openSizesForOS = new List<ClothesScaleSizeClass>();
                                        int clothesScaleId = oScale.ClothesId;
                                        var sSQOpenSize = oScale.ClothesScaleSizes.Where(x => x.IsActive == true && x.IsDelete == false);
                                        foreach (var os in sSQOpenSize)
                                        {
                                            ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                            scaleRow.ClothesScaleSizeId = os.ClothesScaleSizeId;
                                            scaleRow.ClothesScaleId = os.ClothesScaleId;
                                            scaleRow.OrderSSId = Guid.Empty;
                                            scaleRow.SizeId = os.SizeId;
                                            scaleRow.SizeName = os.Size.Name;
                                            scaleRow.Quantity = os.Quantity;
                                            scaleRow.isConfirm = false;
                                            if (retModel.CartOwner.isRetail)
                                            {
                                                scaleRow.RtlAvlbl = true;
                                                if (!os.Quantity.HasValue || os.Quantity.Value <= 0)
                                                    if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == sFit && (x.InseamId.HasValue ? x.InseamId.Value : 0) == sInseam && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                                    else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == sFit && (x.InseamId.HasValue ? x.InseamId.Value : 0) == sInseam && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                        SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                            }
                                            scaleRow.PurchasedQuantity = 0;
                                            openSizesForOS.Add(scaleRow);
                                        }
                                        contents.SPs[i].OpenSizes.AddRange(openSizesForOS);
                                    }
                                }
                            }
                            else
                            {
                                temp += contents.SPs[i].OpenSizes.Sum(x => x.PurchasedQuantity).Value;
                                contents.SPs[i].Quantity = temp;
                            }
                            contents.SPs[i].Total = temp * contents.Price;
                            bool checkfit = contents.SPs[i].FitId > 0;
                            bool checkins = contents.SPs[i].InseamId > 0;
                            contents.SPs[i].ShowPrepack = cloth.ClothesScales.Any(x => (checkfit ? x.FitId == contents.SPs[i].FitId : true) && (checkins ? x.InseamId == contents.SPs[i].InseamId : true) &&
                                x.IsActive == true && x.IsDelete == false && x.IsOpenSize == false && x.InvQty > 0);
                        }
                        Price += contents.SPs.Sum(x => x.Total);
                        cartItem.Contents.Add(contents);
                    }
                    retModel.Clothes.Add(cartItem);
                }

                lastOrder.GrandTotal = Price;
                if (retModel.CartOwner.UserId > 0)
                    lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * (retModel.CartOwner.Discount / 100));
                else
                    lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * ((lastOrder.Discount.HasValue ? lastOrder.Discount.Value : 0) / 100));
                lastOrder.FinalAmount += (lastOrder.ShippingCost.HasValue ? lastOrder.ShippingCost.Value : 0);
                if (!isPrint && !isCookie)
                    db.SaveChanges();
                retModel.OrdNum = lastOrder.OrderNumber;
                retModel.GrandTotal = Price;
                retModel.isSubmit = lastOrder.SubmittedOn.HasValue;
                if (retModel.CartOwner.UserId > 0)
                    retModel.Discount = retModel.CartOwner.Discount;
                else
                    retModel.Discount = lastOrder.Discount.HasValue ? lastOrder.Discount.Value : 0.0m;
                retModel.Note = lastOrder.Note;
                retModel.FinalAmount = lastOrder.FinalAmount.HasValue ? lastOrder.FinalAmount.Value : Price;
                return retModel;
            }
            return null;

        }

        [HttpPost]
        public ActionResult CartPost(Cart Cart)
        {
            TempData["HCartValue"] = Request.Form["HCart"];
            if (Cart != null)
            {
                if (Cart.OrderId != Guid.Empty)
                {
                    Session.Remove("PaypalToken");
                    bool isCookie = string.IsNullOrEmpty(SiteIdentity.UserId);
                    DB.Order lastOrder = null;
                    bool loginRdr = false;
                    if (!isCookie)
                        lastOrder = db.Orders.Find(Cart.OrderId);
                    else
                    {
                        var ordCookie = Request.Cookies["ordCookie"];
                        if (ordCookie != null)
                        {
                            try
                            {
                                lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                            }
                            catch
                            {
                                lastOrder = null;
                            }
                        }
                    }
                    string name = "";
                    string salespersonemail = "";
                    bool reOpenForAccount = false;
                    var customer = db.Accounts.FirstOrDefault(x => x.AccountId == Cart.UserId && x.IsActive == true && x.IsDelete == false);
                    if (lastOrder != null)
                    {
                        if (isCookie && Cart.isSubmit)
                        {
                            Cart.isSubmit = false;
                            loginRdr = true;
                        }
                        if (customer != null)
                        {
                            if (Cart.CartOwner != null)
                            {
                                #region CustomerUpdate
                                reOpenForAccount = lastOrder.AccountId != customer.AccountId;
                                lastOrder.AccountId = customer.AccountId;
                                lastOrder.DateUpdated = DateTime.UtcNow;
                                db.SaveChanges();
                                if (Cart.CartOwner.Email != customer.Email)
                                    customer.Email = Cart.CartOwner.Email;
                                customer.Email = !string.IsNullOrEmpty(customer.Email) ? customer.Email : "";
                                var communication = db.Communications.Find(Cart.CartOwner.CommunicationId);
                                if (communication == null)
                                {
                                    communication = new Communication();
                                    communication.AccountId = customer.AccountId;
                                    communication.Phone = Cart.CartOwner.Phone;
                                    communication.Fax = Cart.CartOwner.Fax;
                                    communication.DateCreated = DateTime.UtcNow;
                                    communication.CommunicationId = 0;
                                }
                                else
                                {
                                    communication.Phone = Cart.CartOwner.Phone;
                                    communication.Fax = Cart.CartOwner.Fax;
                                }
                                communication.IsActive = true;
                                communication.IsDelete = false;
                                communication.DateUpdated = DateTime.UtcNow;
                                if (communication.CommunicationId == 0)
                                    db.Communications.Add(communication);
                                //db.SaveChanges();
                                try
                                {
                                    db.SaveChanges();
                                }
                                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                                {
                                    // Retrieve the error messages as a list of strings.
                                    var errorMessages = ex.EntityValidationErrors
                                            .SelectMany(x => x.ValidationErrors)
                                            .Select(x => x.ErrorMessage);

                                    // Join the list to a single string.
                                    var fullErrorMessage = string.Join("; ", errorMessages);

                                    // Combine the original exception message with the new one.
                                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                                    // Throw a new DbEntityValidationException with the improved exception message.
                                    throw new System.Data.Entity.Validation.DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                                }
                                var info = customer.CustomerOptionalInfoes.FirstOrDefault();
                                if (info == null)
                                {
                                    info = new CustomerOptionalInfo();
                                    info.AccountId = customer.AccountId;
                                    info.CustomerOptionalInfoId = 0;
                                    info.CustomerType = Cart.CartOwner.isRetail ? (int)Platini.Models.CustomerType.Retail : (int)Platini.Models.CustomerType.Wholesale;
                                    info.DateCreated = DateTime.UtcNow;
                                }
                                info.Discount = Cart.Discount;
                                if (Cart.CartOwner.TermId > 0)
                                {
                                    info.TermId = Cart.CartOwner.TermId;
                                    lastOrder.TermId = Cart.CartOwner.TermId;
                                }
                                if (Cart.CartOwner.ShipVia > 0)
                                {
                                    info.ShipViaId = Cart.CartOwner.ShipVia;
                                    lastOrder.ShipViaId = Cart.CartOwner.ShipVia;
                                }
                                info.DateUpdated = DateTime.UtcNow;
                                if (info.CustomerOptionalInfoId == 0)
                                    db.CustomerOptionalInfoes.Add(info);
                                var billAddress = customer.Addresses.FirstOrDefault(x => x.AddressId == Cart.CartOwner.BillingAddress.AddressId && x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);
                                if (billAddress == null)
                                {
                                    billAddress = new DB.Address();
                                    billAddress.AddressId = 0;
                                    billAddress.AccountId = customer.AccountId;
                                    billAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
                                    billAddress.DateCreated = DateTime.UtcNow;
                                }
                                billAddress.Street = Cart.CartOwner.BillingAddress.Line1;
                                if (!string.IsNullOrEmpty(Cart.CartOwner.BillingAddress.Line2))
                                {
                                    var bilArr = Cart.CartOwner.BillingAddress.Line2.Split(',');
                                    if (bilArr.Count() > 2)
                                        billAddress.Pincode = bilArr[2].Trim();
                                    if (bilArr.Count() > 1)
                                        billAddress.State = bilArr[1].Trim();
                                    if (bilArr.Count() > 0)
                                        billAddress.City = bilArr[0].Trim();
                                }
                                billAddress.IsActive = true;
                                billAddress.IsDelete = false;
                                billAddress.DateUpdated = DateTime.UtcNow;
                                if (billAddress.AddressId == 0)
                                    db.Addresses.Add(billAddress);
                                var shippingAddress = customer.Addresses.FirstOrDefault(x => x.AddressId == Cart.CartOwner.ShippingAddress.AddressId && x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
                                if (shippingAddress == null)
                                {
                                    shippingAddress = new DB.Address();
                                    shippingAddress.AddressId = 0;
                                    shippingAddress.AccountId = customer.AccountId;
                                    shippingAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
                                    shippingAddress.DateCreated = DateTime.UtcNow;
                                }
                                shippingAddress.Street = Cart.CartOwner.ShippingAddress.Line1;
                                shippingAddress.ShipTo = Cart.CartOwner.ShippingAddress.To;
                                shippingAddress.Pincode = Cart.CartOwner.ShippingAddress.ZipCode;
                                if (!string.IsNullOrEmpty(Cart.CartOwner.ShippingAddress.Line2))
                                {
                                    var shipArr = Cart.CartOwner.ShippingAddress.Line2.Split(',');
                                    if (shipArr.Count() > 1)
                                        shippingAddress.State = shipArr[1].Trim();
                                    if (shipArr.Count() > 0)
                                        shippingAddress.City = shipArr[0].Trim();
                                }
                                shippingAddress.IsActive = true;
                                shippingAddress.IsDelete = false;
                                shippingAddress.DateUpdated = DateTime.UtcNow;
                                if (shippingAddress.AddressId == 0)
                                    db.Addresses.Add(shippingAddress);
                                if (Cart.CartOwner.SalesPersonId > 0 && Cart.UserId > 0)
                                {
                                    var salesPerson = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == Cart.UserId);
                                    if (salesPerson == null)
                                    {
                                        salesPerson = new CustomerSalesPerson();
                                        salesPerson.AccountId = Cart.UserId;
                                        salesPerson.DateCreated = DateTime.UtcNow;
                                        salesPerson.IsSalesPersonContact = 1;
                                    }
                                    salesPerson.SalesPersonId = Cart.CartOwner.SalesPersonId;
                                    salesPerson.DateUpdated = DateTime.UtcNow;
                                    if (salesPerson.CustomerSalesPersonId == 0)
                                        db.CustomerSalesPersons.Add(salesPerson);
                                }
                                db.SaveChanges();
                                lastOrder.AddressId = shippingAddress.AddressId;
                                lastOrder.CommunicationId = communication.CommunicationId;
                                #endregion
                            }
                        }
                        if (Cart.Clothes != null)
                        {
                            #region ClothUpdate & ReduceInventory
                            foreach (var clothgroup in Cart.Clothes)
                            {
                                foreach (var cloth in clothgroup.Contents)
                                {
                                    var dbCloth = db.Clothes.Find(cloth.ClothesId);
                                    if (dbCloth != null)
                                    {
                                        if (!Cart.CartOwner.isRetail ? cloth.Price != dbCloth.Price : cloth.Price != dbCloth.MSRP)
                                        {
                                            if (customer != null)
                                            {
                                                var newPrice = customer.CustomerItemPrices.FirstOrDefault(x => x.ClothesId == dbCloth.ClothesId);
                                                if (newPrice == null)
                                                {
                                                    newPrice = new CustomerItemPrice();
                                                    newPrice.CustomerItemPriceId = 0;
                                                    newPrice.ClothesId = dbCloth.ClothesId;
                                                    newPrice.AccountId = customer.AccountId;
                                                    newPrice.DateCreated = DateTime.UtcNow;
                                                }
                                                newPrice.Price = cloth.Price;
                                                newPrice.DateUpdated = DateTime.UtcNow;
                                                if (newPrice.CustomerItemPriceId == 0)
                                                    db.CustomerItemPrices.Add(newPrice);
                                                db.SaveChanges();
                                            }
                                        }
                                        if (cloth.SPs.Any())
                                        {
                                            foreach (var SP in cloth.SPs)
                                            {
                                                if (SP.Pack != null)
                                                {
                                                    var ordPack = lastOrder.OrderScales.FirstOrDefault(x => x.ClothesScaleId == SP.Pack.ClothesScaleId);
                                                    if (ordPack != null)
                                                    {
                                                        var clothPack = db.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == SP.Pack.ClothesScaleId);
                                                        if (clothPack != null)
                                                        {
                                                            if (SP.Pack.PurchasedQty <= clothPack.InvQty)
                                                            {
                                                                ordPack.Quantity = SP.Pack.PurchasedQty;
                                                                if (Cart.isSubmit)
                                                                {
                                                                    clothPack.InvQty = clothPack.InvQty - ordPack.Quantity;
                                                                    clothPack.DateUpdated = DateTime.UtcNow;
                                                                    dbCloth.OriginalQty -= clothPack.ClothesScaleSizes.Sum(x => x.Quantity) * ordPack.Quantity;
                                                                }
                                                            }
                                                            ordPack.DateUpdated = DateTime.UtcNow;
                                                        }
                                                        db.SaveChanges();
                                                    }
                                                }
                                                if (SP.OpenSizes != null)
                                                {
                                                    foreach (var size in SP.OpenSizes)
                                                    {
                                                        var ordSize = lastOrder.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesScaleSizeId);
                                                        var clothSize = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleSizeId == size.ClothesScaleSizeId);
                                                        if (ordSize != null)
                                                        {
                                                            if (clothSize != null)
                                                            {
                                                                if (SiteIdentity.Roles == RolesEnum.Warehouse.ToString() || SiteIdentity.Roles == RolesEnum.Admin.ToString() || SiteIdentity.Roles == RolesEnum.SuperAdmin.ToString())
                                                                {
                                                                    if (size.PurchasedQuantity != null)
                                                                        ordSize.Quantity = size.PurchasedQuantity;
                                                                }
                                                                else
                                                                {
                                                                    if (size.PurchasedQuantity <= clothSize.Quantity || Cart.CartOwner.isRetail)
                                                                        ordSize.Quantity = size.PurchasedQuantity;
                                                                }
                                                            }
                                                        }
                                                        else if (size.PurchasedQuantity.HasValue ? size.PurchasedQuantity.Value > 0 : false)
                                                        {
                                                            if (clothSize != null)
                                                            {
                                                                ordSize = new OrderSize();
                                                                ordSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                                                ordSize.ClothesSizeId = size.ClothesScaleSizeId;
                                                                ordSize.OrderId = lastOrder.OrderId;
                                                                ordSize.DateCreated = ordSize.DateUpdated = DateTime.UtcNow;
                                                                ordSize.ClothesId = clothSize.ClothesScale.ClothesId;
                                                                if (size.PurchasedQuantity <= clothSize.Quantity || Cart.CartOwner.isRetail)
                                                                    ordSize.Quantity = size.PurchasedQuantity;

                                                                if (!isCookie)
                                                                    db.OrderSizes.Add(ordSize);
                                                                else
                                                                    lastOrder.OrderSizes.Add(ordSize);

                                                            }
                                                        }
                                                        else
                                                        {
                                                            //Recode
                                                            if (clothSize != null)
                                                            {
                                                                ordSize = new OrderSize();
                                                                ordSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                                                ordSize.ClothesSizeId = size.ClothesScaleSizeId;
                                                                ordSize.OrderId = lastOrder.OrderId;
                                                                ordSize.DateCreated = ordSize.DateUpdated = DateTime.UtcNow;
                                                                ordSize.ClothesId = clothSize.ClothesScale.ClothesId;
                                                                ordSize.Quantity = 0;
                                                                if (!isCookie)
                                                                    db.OrderSizes.Add(ordSize);
                                                                else
                                                                    lastOrder.OrderSizes.Add(ordSize);
                                                            }
                                                        }
                                                        if (Cart.isSubmit && ordSize != null && !Cart.CartOwner.isRetail)
                                                        {
                                                            clothSize.Quantity = clothSize.Quantity - ordSize.Quantity;
                                                            clothSize.DateUpdated = DateTime.UtcNow;
                                                            dbCloth.OriginalQty -= ordSize.Quantity;
                                                        }
                                                        if (!isCookie)
                                                            db.SaveChanges();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                        lastOrder.Note = Cart.Note;
                        lastOrder.Discount = Cart.Discount;
                        lastOrder.OriginalQty = 0;
                        if (!isCookie)
                        {
                            if (db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                                lastOrder.OriginalQty = db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
                            if (db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                                lastOrder.OriginalQty += db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                            Session["Order"] = lastOrder.OrderId;
                        }
                        else
                        {
                            if (lastOrder.OrderSizes.Any())
                                lastOrder.OriginalQty = lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                            Response.Cookies["ordCookie"].Value = JsonConvert.SerializeObject(lastOrder);
                            Response.Cookies["ordCookie"].Expires = DateTime.Now.AddMonths(1);

                        }
                        if (Cart.isSubmit && !Cart.CartOwner.isRetail)
                        {
                            lastOrder.SubmittedOn = DateTime.UtcNow;
                            lastOrder.StatusId = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "new").OrderStatusId;
                            lastOrder.IsSentToQuickBook = false;
                            lastOrder.OrderNumber = SiteConfiguration.OrderNumber();
                            lastOrder.GrandTotal = CalcSum(lastOrder.OrderId);
                            lastOrder.TagId = Cart.TagId;
                            lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * ((lastOrder.Discount.HasValue ? lastOrder.Discount.Value : 0) / 100));
                            if (customer != null)
                            {
                                if (customer.Companies.Any(x => x.IsActive == true && x.IsDelete == false))
                                    name = customer.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Name;
                                else
                                    name = customer.FirstName + " " + customer.LastName;
                                if (customer.CustomerSalesPersons.Any())
                                {
                                    var sales = db.Accounts.Find(customer.CustomerSalesPersons.FirstOrDefault().SalesPersonId);
                                    if (sales != null)
                                        salespersonemail = sales.Email;
                                }
                            }
                            EmailManager.SendOrderEmail(lastOrder.OrderId, lastOrder.OrderNumber, lastOrder.AccountId.ToString(), name, salespersonemail);
                            EmailManager.SendOrderEmailToCustomer(lastOrder.OrderId, lastOrder.OrderNumber, (customer != null ? customer.Email : ""), lastOrder.AccountId.ToString());
                            TempData["PageMessage"] = "The Order was submitted successfully.";
                            Session.Remove("Order");
                            Session.Remove("EditingOrder");
                            Session["WasCleared"] = 1;
                        }
                        lastOrder.DateUpdated = DateTime.UtcNow;
                        if (loginRdr)
                        {
                            return RedirectToAction("Login");
                        }
                        if (!isCookie)
                            db.SaveChanges();
                        if (reOpenForAccount)
                            TempData["CartSuccess"] = "ghi";
                        if (Cart.CartOwner.isRetail && Cart.isSubmit)
                        {
                            TempData["CheckLogin"] = 1;
                            return RedirectToAction("Billing");
                        }
                    }
                }
                else if (Cart.UserId > 0)
                {
                    Session.Remove("WasCleared");
                    int UserId = 0;
                    int StatusId = 0;
                    var account = db.Accounts.Find(Cart.UserId);
                    if (account != null)
                    {
                        int.TryParse(SiteIdentity.UserId, out UserId);
                        var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
                        if (status != null)
                            StatusId = status.OrderStatusId;
                        var lastOrder = db.Orders.Where(x => (x.AccountId == account.AccountId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
                        if (lastOrder == null)
                        {
                            lastOrder = new DB.Order();
                            lastOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            lastOrder.OrderNumber = string.Empty;
                            lastOrder.AccountId = account.AccountId;
                            if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower())
                            {
                                var sp = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == account.AccountId);
                                if (sp != null)
                                    lastOrder.EmployeeId = sp.SalesPersonId;
                            }
                            if (!lastOrder.EmployeeId.HasValue)
                                lastOrder.EmployeeId = UserId;
                            lastOrder.CreatedOn = DateTime.UtcNow;
                            lastOrder.DateCreated = lastOrder.DateUpdated = DateTime.UtcNow;
                            lastOrder.StatusId = StatusId;                            
                            lastOrder.ShippingCost = 0;
                            decimal disc = 0.0m;
                            if (UserId > 0)
                            {
                                var ci = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == UserId);
                                if (ci != null)
                                    disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                            }
                            lastOrder.Discount = disc;
                            lastOrder.IsDelete = false;
                            lastOrder.IsSentToQuickBook = false;
                            lastOrder.TagId = Cart.TagId;
                            db.Orders.Add(lastOrder);
                            db.SaveChanges();
                            Session["Order"] = lastOrder.OrderId;
                            TempData["CartSuccess"] = "def";                        
                        }
                        else
                        {
                            TempData["CartSuccess"] = "def";
                            Session["Order"] = lastOrder.OrderId;
                        }
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateCart(Cart Cart)
        {
            TempData["HCartValue"] = Request.Form["HCart"];
            if (Cart != null)
            {
                if (Cart.OrderId != Guid.Empty)
                {
                    bool isCookie = string.IsNullOrEmpty(SiteIdentity.UserId);
                    DB.Order lastOrder = null;                   
                    if (!isCookie)
                        lastOrder = db.Orders.Find(Cart.OrderId);
                    else
                    {
                        var ordCookie = Request.Cookies["ordCookie"];
                        if (ordCookie != null)
                        {
                            try
                            {
                                lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                            }
                            catch
                            {
                                lastOrder = null;
                            }
                        }
                    }                                  
                    var customer = db.Accounts.FirstOrDefault(x => x.AccountId == Cart.UserId && x.IsActive == true && x.IsDelete == false);
                    if (lastOrder != null)
                    {                       
                        if (customer != null)
                        {
                            if (Cart.CartOwner != null)
                            {
                                #region CustomerUpdate
                                lastOrder.AccountId = customer.AccountId;
                                lastOrder.DateUpdated = DateTime.UtcNow;
                                db.SaveChanges();
                                if (Cart.CartOwner.Email != customer.Email)
                                    customer.Email = Cart.CartOwner.Email;
                                var communication = db.Communications.Find(Cart.CartOwner.CommunicationId);
                                if (communication == null)
                                {
                                    communication = new Communication();
                                    communication.AccountId = customer.AccountId;
                                    communication.Phone = Cart.CartOwner.Phone;
                                    communication.Fax = Cart.CartOwner.Fax;
                                    communication.DateCreated = DateTime.UtcNow;
                                    communication.CommunicationId = 0;
                                }
                                else
                                {
                                    communication.Phone = Cart.CartOwner.Phone;
                                    communication.Fax = Cart.CartOwner.Fax;
                                }
                                communication.IsActive = true;
                                communication.IsDelete = false;
                                communication.DateUpdated = DateTime.UtcNow;
                                if (communication.CommunicationId == 0)
                                    db.Communications.Add(communication);
                                db.SaveChanges();
                                var info = customer.CustomerOptionalInfoes.FirstOrDefault();
                                if (info == null)
                                {
                                    info = new CustomerOptionalInfo();
                                    info.AccountId = customer.AccountId;
                                    info.CustomerOptionalInfoId = 0;
                                    info.CustomerType = Cart.CartOwner.isRetail ? (int)Platini.Models.CustomerType.Retail : (int)Platini.Models.CustomerType.Wholesale;
                                    info.DateCreated = DateTime.UtcNow;
                                }
                                info.Discount = Cart.Discount;
                                if (Cart.CartOwner.TermId > 0)
                                {
                                    info.TermId = Cart.CartOwner.TermId;
                                    lastOrder.TermId = Cart.CartOwner.TermId;
                                }
                                if (Cart.CartOwner.ShipVia > 0)
                                {
                                    info.ShipViaId = Cart.CartOwner.ShipVia;
                                    lastOrder.ShipViaId = Cart.CartOwner.ShipVia;
                                }
                                info.DateUpdated = DateTime.UtcNow;
                                if (info.CustomerOptionalInfoId == 0)
                                    db.CustomerOptionalInfoes.Add(info);
                                var billAddress = customer.Addresses.FirstOrDefault(x => x.AddressId == Cart.CartOwner.BillingAddress.AddressId && x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);
                                if (billAddress == null)
                                {
                                    billAddress = new DB.Address();
                                    billAddress.AddressId = 0;
                                    billAddress.AccountId = customer.AccountId;
                                    billAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
                                    billAddress.DateCreated = DateTime.UtcNow;
                                }
                                billAddress.Street = Cart.CartOwner.BillingAddress.Line1;
                                if (!string.IsNullOrEmpty(Cart.CartOwner.BillingAddress.Line2))
                                {
                                    var bilArr = Cart.CartOwner.BillingAddress.Line2.Split(',');
                                    if (bilArr.Count() > 2)
                                        billAddress.Pincode = bilArr[2].Trim();
                                    if (bilArr.Count() > 1)
                                        billAddress.State = bilArr[1].Trim();
                                    if (bilArr.Count() > 0)
                                        billAddress.City = bilArr[0].Trim();
                                }
                                billAddress.IsActive = true;
                                billAddress.IsDelete = false;
                                billAddress.DateUpdated = DateTime.UtcNow;
                                if (billAddress.AddressId == 0)
                                    db.Addresses.Add(billAddress);
                                var shippingAddress = customer.Addresses.FirstOrDefault(x => x.AddressId == Cart.CartOwner.ShippingAddress.AddressId && x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
                                if (shippingAddress == null)
                                {
                                    shippingAddress = new DB.Address();
                                    shippingAddress.AddressId = 0;
                                    shippingAddress.AccountId = customer.AccountId;
                                    shippingAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
                                    shippingAddress.DateCreated = DateTime.UtcNow;
                                }
                                shippingAddress.Street = Cart.CartOwner.ShippingAddress.Line1;
                                shippingAddress.ShipTo = Cart.CartOwner.ShippingAddress.To;
                                shippingAddress.Pincode = Cart.CartOwner.ShippingAddress.ZipCode;
                                if (!string.IsNullOrEmpty(Cart.CartOwner.ShippingAddress.Line2))
                                {
                                    var shipArr = Cart.CartOwner.ShippingAddress.Line2.Split(',');
                                    if (shipArr.Count() > 1)
                                        shippingAddress.State = shipArr[1].Trim();
                                    if (shipArr.Count() > 0)
                                        shippingAddress.City = shipArr[0].Trim();
                                }
                                shippingAddress.IsActive = true;
                                shippingAddress.IsDelete = false;
                                shippingAddress.DateUpdated = DateTime.UtcNow;
                                if (shippingAddress.AddressId == 0)
                                    db.Addresses.Add(shippingAddress);
                                if (Cart.CartOwner.SalesPersonId > 0 && Cart.UserId > 0)
                                {
                                    var salesPerson = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == Cart.UserId);
                                    if (salesPerson == null)
                                    {
                                        salesPerson = new CustomerSalesPerson();
                                        salesPerson.AccountId = Cart.UserId;
                                        salesPerson.DateCreated = DateTime.UtcNow;
                                        salesPerson.IsSalesPersonContact = 1;
                                    }
                                    salesPerson.SalesPersonId = Cart.CartOwner.SalesPersonId;
                                    salesPerson.DateUpdated = DateTime.UtcNow;
                                    if (salesPerson.CustomerSalesPersonId == 0)
                                        db.CustomerSalesPersons.Add(salesPerson);
                                }
                                db.SaveChanges();
                                lastOrder.AddressId = shippingAddress.AddressId;
                                lastOrder.CommunicationId = communication.CommunicationId;
                                #endregion
                            }
                        }
                        if (Cart.Clothes != null)
                        {
                            #region ClothUpdate & ReduceInventory
                            foreach (var clothgroup in Cart.Clothes)
                            {
                                foreach (var cloth in clothgroup.Contents)
                                {
                                    var dbCloth = db.Clothes.Find(cloth.ClothesId);
                                    if (dbCloth != null)
                                    {
                                        if (!Cart.CartOwner.isRetail ? cloth.Price != dbCloth.Price : cloth.Price != dbCloth.MSRP)
                                        {
                                            if (customer != null)
                                            {
                                                var newPrice = customer.CustomerItemPrices.FirstOrDefault(x => x.ClothesId == dbCloth.ClothesId);
                                                if (newPrice == null)
                                                {
                                                    newPrice = new CustomerItemPrice();
                                                    newPrice.CustomerItemPriceId = 0;
                                                    newPrice.ClothesId = dbCloth.ClothesId;
                                                    newPrice.AccountId = customer.AccountId;
                                                    newPrice.DateCreated = DateTime.UtcNow;
                                                }
                                                newPrice.Price = cloth.Price;
                                                newPrice.DateUpdated = DateTime.UtcNow;
                                                if (newPrice.CustomerItemPriceId == 0)
                                                    db.CustomerItemPrices.Add(newPrice);
                                                db.SaveChanges();
                                            }
                                        }
                                        if (cloth.SPs.Any())
                                        {
                                            foreach (var SP in cloth.SPs)
                                            {
                                                if (SP.Pack != null)
                                                {
                                                    var ordPack = lastOrder.OrderScales.FirstOrDefault(x => x.ClothesScaleId == SP.Pack.ClothesScaleId);
                                                    if (ordPack != null)
                                                    {
                                                        var clothPack = db.ClothesScales.FirstOrDefault(x => x.ClothesScaleId == SP.Pack.ClothesScaleId);
                                                        if (clothPack != null)
                                                        {
                                                            if (SP.Pack.PurchasedQty <= clothPack.InvQty)
                                                            {
                                                                ordPack.Quantity = SP.Pack.PurchasedQty;
                                                                if (Cart.isSubmit)
                                                                {
                                                                    clothPack.InvQty = clothPack.InvQty - ordPack.Quantity;
                                                                    clothPack.DateUpdated = DateTime.UtcNow;
                                                                    dbCloth.OriginalQty -= clothPack.ClothesScaleSizes.Sum(x => x.Quantity) * ordPack.Quantity;
                                                                }
                                                            }
                                                            ordPack.DateUpdated = DateTime.UtcNow;
                                                        }
                                                        db.SaveChanges();
                                                    }
                                                }
                                                if (SP.OpenSizes != null)
                                                {
                                                    foreach (var size in SP.OpenSizes)
                                                    {
                                                        var ordSize = lastOrder.OrderSizes.FirstOrDefault(x => x.ClothesSizeId == size.ClothesScaleSizeId);
                                                        var clothSize = db.ClothesScaleSizes.FirstOrDefault(x => x.ClothesScaleSizeId == size.ClothesScaleSizeId);
                                                        if (ordSize != null)
                                                        {
                                                            if (clothSize != null)
                                                            {
                                                                if (size.PurchasedQuantity <= clothSize.Quantity || Cart.CartOwner.isRetail)
                                                                    ordSize.Quantity = size.PurchasedQuantity;
                                                            }
                                                        }
                                                        else if (size.PurchasedQuantity.HasValue ? size.PurchasedQuantity.Value > 0 : false)
                                                        {
                                                            if (clothSize != null)
                                                            {
                                                                ordSize = new OrderSize();
                                                                ordSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                                                ordSize.ClothesSizeId = size.ClothesScaleSizeId;
                                                                ordSize.OrderId = lastOrder.OrderId;
                                                                ordSize.DateCreated = ordSize.DateUpdated = DateTime.UtcNow;
                                                                ordSize.ClothesId = clothSize.ClothesScale.ClothesId;
                                                                if (size.PurchasedQuantity <= clothSize.Quantity || Cart.CartOwner.isRetail)
                                                                    ordSize.Quantity = size.PurchasedQuantity;
                                                                if (!isCookie)
                                                                    db.OrderSizes.Add(ordSize);
                                                                else
                                                                    lastOrder.OrderSizes.Add(ordSize);
                                                            }
                                                        }
                                                        if (Cart.isSubmit && ordSize != null && !Cart.CartOwner.isRetail)
                                                        {
                                                            clothSize.Quantity = clothSize.Quantity - ordSize.Quantity;
                                                            clothSize.DateUpdated = DateTime.UtcNow;
                                                            dbCloth.OriginalQty -= ordSize.Quantity;
                                                        }
                                                        if (!isCookie)
                                                            db.SaveChanges();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                        lastOrder.Note = Cart.Note;
                        lastOrder.Discount = Cart.Discount;
                        lastOrder.OriginalQty = 0;
                        if (!isCookie)
                        {
                            if (db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                                lastOrder.OriginalQty = db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
                            if (db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                                lastOrder.OriginalQty += db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                            Session["Order"] = lastOrder.OrderId;
                        }
                        else
                        {
                            if (lastOrder.OrderSizes.Any())
                                lastOrder.OriginalQty = lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                            Response.Cookies["ordCookie"].Value = JsonConvert.SerializeObject(lastOrder);
                            Response.Cookies["ordCookie"].Expires = DateTime.Now.AddMonths(1);

                        }                       
                        lastOrder.DateUpdated = DateTime.UtcNow;                        
                        if (!isCookie)
                            db.SaveChanges();                                           
                    }
                }
            }
            return Json("1", JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        public decimal? CalcSum(Guid OrderId)
        {
            var order = db.Orders.Find(OrderId);
            var account = db.Accounts.Find(order.AccountId);
            bool isRetail = account.CustomerOptionalInfoes.FirstOrDefault() != null ? account.CustomerOptionalInfoes.FirstOrDefault().CustomerType == (int)Platini.Models.CustomerType.Retail : true;
            var priceList = account.CustomerItemPrices.Where(x => x.Price.HasValue).ToList().Select(x => new { Id = x.ClothesId, Price = x.Price });
            var scalePriceList = order.OrderScales.Select(x => x.ClothesScale.Cloth).SelectMany(x => priceList.Where(y => y.Id == x.ClothesId).DefaultIfEmpty(), (x, y) => new { Price = (y != null) ? y.Price : (isRetail ? x.MSRP : x.Price), Id = x.ClothesId });
            var sizePriceList = order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth).SelectMany(x => priceList.Where(y => y.Id == x.ClothesId).DefaultIfEmpty(), (x, y) => new { Price = (y != null) ? y.Price : (isRetail ? x.MSRP : x.Price), Id = x.ClothesId });
            var scalePrice = order.OrderScales.Sum(x => (x.Quantity) * (x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity)) * (scalePriceList.FirstOrDefault(z => z.Id == x.ClothesScale.ClothesId).Price));
            var sizePrice = order.OrderSizes.Sum(x => x.Quantity * (sizePriceList.FirstOrDefault(y => y.Id == x.ClothesScaleSize.ClothesScale.ClothesId).Price));
            return scalePrice + sizePrice;
        }

        public ActionResult DeleteOrder(Guid Id)
        {
            var delOrder = db.Orders.Find(Id);
            if (delOrder != null)
            {
                int StatusId = 0;
                var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
                if (status != null)
                    StatusId = status.OrderStatusId;
                if (delOrder.StatusId == StatusId)
                {
                    if (delOrder.OrderScales.Any())
                    {
                        var scaleList = delOrder.OrderScales.ToList();
                        foreach (var scale in scaleList)
                        {
                            var ttd = new TableToDelete();
                            ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttd.TableName = "OrderScale";
                            ttd.TableKey = "OrderScaleId";
                            ttd.TableValue = scale.OrderScaleId.ToString();
                            ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttd);
                            db.OrderScales.Remove(scale);
                        }
                        db.SaveChanges();

                    }
                    if (delOrder.OrderSizes.Any())
                    {
                        var sizeList = delOrder.OrderSizes.ToList();
                        foreach (var size in sizeList)
                        {
                            var ttd = new TableToDelete();
                            ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttd.TableName = "OrderSize";
                            ttd.TableKey = "OrderSizeId";
                            ttd.TableValue = size.OrderSizeId.ToString();
                            ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttd);
                            db.OrderSizes.Remove(size);
                        }
                        db.SaveChanges();
                    }
                    var ttdo = new TableToDelete();
                    ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                    ttdo.TableName = "Order";
                    ttdo.TableKey = "OrderId";
                    ttdo.TableValue = delOrder.OrderId.ToString();
                    ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                    db.TableToDeletes.Add(ttdo);
                    db.Orders.Remove(delOrder);
                    Session.Remove("Order");
                    Session.Remove("EditingOrder");
                    Session["WasCleared"] = 1;
                    db.SaveChanges();
                    TempData["PageMessage"] = "The Order was deleted succesfully.";
                }
            }
            return RedirectToAction("Index");
        }

        [NonAction]
        public string GroupName(int TypeId)
        {
            string GroupName = string.Empty;
            var cType = db.Categories.Find(TypeId);
            if (cType != null)
            {
                var subCat = db.Categories.Find(cType.ParentId);
                if (subCat != null)
                {
                    var cat = db.Categories.Find(subCat.ParentId);
                    if (cat != null)
                        if (cat.ParentId == 0)
                            GroupName = cat.Name + " ";
                }
                GroupName += cType.Name;
            }
            return GroupName;
        }

        public ActionResult Users()
        {
            var List = db.Accounts.Where(x => x.IsActive == true && x.IsDelete == false && (x.RoleId == (int)RolesEnum.Customer || x.RoleId == (int)RolesEnum.User)).ToList();
            var retList = new List<SelectedListValues>();
            bool ShowLess = SiteIdentity.IsAdmin.ToLower() != "true";
            int UserId = 0;
            int.TryParse(SiteIdentity.UserId, out UserId);
            foreach (var item in List)
            {
                if (ShowLess)
                    if (db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == item.AccountId && x.SalesPersonId == UserId) == null)
                        continue;
                var ret = new SelectedListValues();
                ret.Id = item.AccountId;
                if (item.Companies.Any(x => x.IsActive == true && x.IsDelete == false))
                {
                    ret.Value = item.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Name;
                }
                if (string.IsNullOrEmpty(ret.Value))
                {
                    ret.Value = item.FirstName + " " + item.LastName;
                }
                retList.Add(ret);
            }
            retList = retList.OrderBy(x => x.Value).ToList();
            //retList.Insert(retList.Count, new SelectedListValues() { Id = 0, Value = "New Customer" });
            return Json(retList.Select(x => new { label = x.Value, value = x.Id }), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Salesmen()
        {
            var retList = db.Accounts.Where(x => x.IsActive == true && x.IsDelete == false && x.RoleId == (int)RolesEnum.SalesPerson).ToList().Select(x => new { value = x.AccountId, label = x.FirstName + " " + x.LastName }).OrderBy(x => x.label).ToList();
            return Json(retList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Clothes()
        {
            var retList = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false).ToList().Select(x => new SelectedListValues() { Id = x.ClothesId, Value = x.StyleNumber });
            return Json(retList.Select(x => new { label = x.Value, value = x.Id }), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteQuantity(string PackId, string SizeIds)
        {
            Guid OrderId = Guid.Empty;
            bool isCookie = string.IsNullOrEmpty(SiteIdentity.UserId);
            DB.Order lastOrder = null;
            if (isCookie)
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                    }
                    catch
                    {
                        lastOrder = null;
                    }
                }
            }
            if (!string.IsNullOrEmpty(PackId))
            {
                Guid PackOrdId;
                Guid.TryParse(PackId, out PackOrdId);
                var pack = db.OrderScales.Find(PackOrdId);
                if (pack != null)
                {
                    OrderId = pack.OrderId;
                    var ttd = new TableToDelete();
                    ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                    ttd.TableName = "OrderScale";
                    ttd.TableKey = "OrderScaleId";
                    ttd.TableValue = pack.OrderScaleId.ToString();
                    ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                    db.TableToDeletes.Add(ttd);
                    db.OrderScales.Remove(pack);
                    db.SaveChanges();
                }
            }
            if (!string.IsNullOrEmpty(SizeIds))
            {
                string[] Ids = SizeIds.Split(',');
                foreach (var Id in Ids)
                {
                    Guid SizeOrdId;
                    Guid.TryParse(Id, out SizeOrdId);
                    OrderSize size = null;
                    if (!isCookie)
                        size = db.OrderSizes.Find(SizeOrdId);
                    else
                        size = lastOrder.OrderSizes.FirstOrDefault(x => x.OrderSizeId == SizeOrdId);
                    if (size != null)
                    {
                        OrderId = size.OrderId;
                        if (!isCookie)
                        {
                            var ttd = new TableToDelete();
                            ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttd.TableName = "OrderSize";
                            ttd.TableKey = "OrderSizeId";
                            ttd.TableValue = size.OrderSizeId.ToString();
                            ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttd);
                            db.OrderSizes.Remove(size);
                        }
                        else
                            lastOrder.OrderSizes.Remove(size);
                    }
                }
                if (!isCookie)
                    db.SaveChanges();
            }
            if (!isCookie)
                lastOrder = db.Orders.Find(OrderId);
            if (lastOrder != null)
            {
                var count = lastOrder.OrderScales.Count + lastOrder.OrderSizes.Count;
                if (count == 0)
                {
                    if (!isCookie)
                    {
                        var ttdo = new TableToDelete();
                        ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttdo.TableName = "Order";
                        ttdo.TableKey = "OrderId";
                        ttdo.TableValue = lastOrder.OrderId.ToString();
                        ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttdo);
                        db.Orders.Remove(lastOrder);
                        Session.Remove("Order");
                        Session.Remove("EditingOrder");
                    }
                    else
                        Response.Cookies["ordCookie"].Expires = DateTime.Now.AddDays(-1);
                }
                else
                {
                    lastOrder.OriginalQty = 0;
                    if (lastOrder.OrderScales.Any())
                        lastOrder.OriginalQty = lastOrder.OrderScales.Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
                    if (lastOrder.OrderSizes.Any())
                        lastOrder.OriginalQty += lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                    if (isCookie)
                    {
                        Response.Cookies["ordCookie"].Value = JsonConvert.SerializeObject(lastOrder);
                        Response.Cookies["ordCookie"].Expires = DateTime.Now.AddMonths(1);
                    }
                    else
                    {
                        lastOrder.GrandTotal = CalcSum(lastOrder.OrderId);
                        lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * (lastOrder.Discount / 100));
                        lastOrder.FinalAmount += (lastOrder.ShippingCost.HasValue ? lastOrder.ShippingCost.Value : 0);
                        db.SaveChanges();
                    }
                }
                if (!isCookie)
                    db.SaveChanges();
            }
            TempData["CartSuccess"] = "abc";
            return Json("1", JsonRequestBehavior.AllowGet);
            //RedirectToAction("Cart");
        }

        [HttpGet]
        public ActionResult GetCloth(int Id)
        {
            var cloth = db.Clothes.Find(Id);
            if (cloth != null)
            {
                var retModel = new DetailViewClass();
                retModel.ClothesId = cloth.ClothesId;
                retModel.StyleNumber = cloth.StyleNumber;
                retModel.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                return PartialView("UploadImages", retModel);
            }
            return RedirectToAction("Index");
        }

        public ActionResult GetMultiCloth(string Ids)
        {
            if (!string.IsNullOrEmpty(Ids))
            {
                var values = Ids.Split(',').Select(s => int.Parse(s));
                var retModel = new List<DetailViewClass>();
                foreach (var id in values)
                {
                    var cloth = db.Clothes.Find(id);
                    if (cloth != null)
                    {
                        var Model = new DetailViewClass();
                        Model.ClothesId = cloth.ClothesId;
                        Model.StyleNumber = cloth.StyleNumber;
                        Model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                        retModel.Add(Model);
                    }
                }
                return PartialView("UploadMulti", retModel);
            }
            return RedirectToAction("Index");
        }

        public ActionResult Settings()
        {
            WebsiteSettingModel web = new WebsiteSettingModel();
            var webSetting = db.WebsiteSettings.Where(x => x.SettingKey.ToLower() == "settings").FirstOrDefault();
            if (webSetting != null)
            {
                web.WebsiteSettingId = webSetting.WebsiteSettingId;
                web.SettingKey = webSetting.SettingKey;
                web.SettingValue = webSetting.SettingValue;
                web.CheckKey = !string.IsNullOrEmpty(webSetting.SettingValue) ? (webSetting.SettingValue != "0" ? true : false) : false;
            }
            //var dbClothes = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false).ToList();
            //List<string> Name = new List<string>();
            //foreach (var item in dbClothes)
            //{                
            //    int cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
            //        + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
            //    if (cQty == 0)
            //        Name.Add(item.StyleNumber);
            //}
            //ViewBag.Name = Name;
            return View(web);
        }

        [HttpPost]
        public ActionResult DeactivateClothes(string Ids)
        {
            if (!string.IsNullOrEmpty(Ids))
            {
                var List = JsonConvert.DeserializeObject<List<int>>(Ids);
                foreach (var id in List)
                {
                    var cloth = db.Clothes.Find(id);
                    if (cloth != null)
                    {
                        cloth.IsActive = false;
                        cloth.DateUpdated = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                }
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AutoDeactivateAllClothes(bool check)
        {
            var webSetting = db.WebsiteSettings.Where(x => x.SettingKey.ToLower() == "settings").FirstOrDefault();
            if (webSetting != null)
            {
                webSetting.SettingValue = check ? "1" : "0";
                db.SaveChanges();
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteClothes(IEnumerable<int> Ids)
        {
            if (Ids != null)
            {
                var clothes = db.Clothes.Where(x => Ids.Contains(x.ClothesId) && x.IsActive == false).ToList();
                if (clothes.Count > 0)
                {
                    clothes.ForEach(x => { x.IsDelete = true; x.DateUpdated = DateTime.UtcNow; });
                    db.SaveChanges();
                }
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddNewTemplate(int Id, int gid)
        {
            var dbCloth = db.Clothes.Find(Id);
            if (dbCloth != null)
            {
                var newCloth = new Cloth();
                newCloth.StyleNumber = DateTime.Now.Ticks.ToString();
                newCloth.CategoryId = dbCloth.CategoryId;
                newCloth.BrandId = dbCloth.BrandId;
                newCloth.ClothesDescription = dbCloth.ClothesDescription;
                newCloth.Color = dbCloth.Color;
                newCloth.DateCreated = newCloth.DateUpdated = newCloth.DateChanged = DateTime.UtcNow;
                newCloth.FutureDeliveryDate = dbCloth.FutureDeliveryDate;
                newCloth.IsActive = true;
                newCloth.IsDelete = false;
                newCloth.MSRP = dbCloth.MSRP;
                newCloth.Price = dbCloth.Price;
                newCloth.ProductCost = dbCloth.ProductCost;
                newCloth.SizeGroupId = dbCloth.SizeGroupId;
                newCloth.SortOrder = dbCloth.SortOrder;
                db.Clothes.Add(newCloth);
                db.SaveChanges();
                var scaleList = dbCloth.ClothesScales.OrderBy(x => x.ClothesScaleId).ToList();
                foreach (var scale in scaleList)
                {
                    var newScale = new ClothesScale();
                    newScale.ClothesId = newCloth.ClothesId;
                    newScale.FitId = scale.FitId;
                    newScale.InseamId = scale.InseamId;
                    newScale.InvQty = scale.InvQty;
                    newScale.IsActive = true;
                    newScale.IsDelete = false;
                    newScale.IsOpenSize = scale.IsOpenSize;
                    newScale.Name = scale.Name;
                    newScale.DateCreated = newScale.DateUpdated = DateTime.UtcNow;
                    db.ClothesScales.Add(newScale);
                    db.SaveChanges();
                    var sizeList = scale.ClothesScaleSizes.OrderBy(x => x.ClothesScaleSizeId).ToList();
                    foreach (var size in sizeList)
                    {
                        var newSize = new ClothesScaleSize();
                        newSize.ClothesScaleId = newScale.ClothesScaleId;
                        newSize.SizeId = size.SizeId;
                        newSize.Quantity = size.Quantity;
                        newSize.IsActive = true;
                        newSize.IsDelete = false;
                        newSize.DateCreated = newSize.DateUpdated = DateTime.UtcNow;
                        db.ClothesScaleSizes.Add(newSize);
                        db.SaveChanges();
                    }
                }
                var sendCloth = db.Clothes.Find(newCloth.ClothesId);
                if (sendCloth != null)
                {
                    var dbFitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                    var dbInseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                    var model = new LineSheetViewClass();
                    model.ClothesId = sendCloth.ClothesId;
                    model.StyleNumber = sendCloth.StyleNumber;
                    model.Color = sendCloth.Color;
                    model.MSRP = sendCloth.MSRP.HasValue ? sendCloth.MSRP.Value : 0.0m;
                    model.Price = sendCloth.Price.HasValue ? sendCloth.Price.Value : 0.0m;
                    model.Cost = sendCloth.ProductCost.HasValue ? sendCloth.ProductCost.Value : 0.0m;
                    model.Images = sendCloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                    model.CategoryId = sendCloth.CategoryId;

                    var sizeGroupId = sendCloth.SizeGroupId;
                    model.isFuture = sendCloth.FutureDeliveryDate.HasValue ? (sendCloth.FutureDeliveryDate.Value != DateTime.MinValue) : false;
                    model.fDate = model.isFuture ? sendCloth.FutureDeliveryDate.Value.ToString() : DateTime.MinValue.ToString();
                    List<int?> FitList = sendCloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                    List<int?> InseamList = sendCloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();

                    model.FitList = dbFitList.Where(x => FitList.Contains(x.Id)).ToList();
                    model.InseamList = dbInseamList.Where(x => InseamList.Contains(x.Id)).ToList();

                    if (FitList.Count() == 0)
                        FitList.Add(null);

                    if (InseamList.Count() == 0)
                        InseamList.Add(null);

                    foreach (var fitid in FitList)
                    {
                        foreach (var inseamid in InseamList)
                        {
                            var ScaleList = db.ClothesScales.Where(x => x.ClothesId == sendCloth.ClothesId).ToList();
                            if (ScaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid).Any())
                            {
                                var clothesScales = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid);
                                foreach (var scale in clothesScales)
                                {
                                    var openSizesForOS = new List<ClothesScaleSizeClass>();
                                    var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                    foreach (var item in sSQOpenSize)
                                    {
                                        ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                        scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                        scaleRow.ClothesScaleId = item.ClothesScaleId;
                                        scaleRow.SizeId = item.SizeId;
                                        scaleRow.SizeName = item.Size.Name;
                                        scaleRow.Quantity = item.Quantity;
                                        openSizesForOS.Add(scaleRow);
                                    }
                                    var availableOpenSizeItem = new ClothesScaleClass();
                                    availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizesForOS);
                                    availableOpenSizeItem.InseamId = scale.InseamId;
                                    availableOpenSizeItem.FitId = scale.FitId;
                                    availableOpenSizeItem.IsOpenSize = scale.IsOpenSize;
                                    availableOpenSizeItem.Name = scale.Name;
                                    availableOpenSizeItem.InvQty = scale.InvQty;
                                    if (fitid.HasValue)
                                        availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                    if (inseamid.HasValue)
                                        availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                    availableOpenSizeItem.ClothesId = scale.ClothesId;
                                    availableOpenSizeItem.ClothesScaleId = scale.ClothesScaleId;
                                    model.ClothesScale.Add(availableOpenSizeItem);
                                }
                            }
                        }
                    }
                    model.ClothesScale = model.ClothesScale.OrderBy(x => x.FitId).ThenByDescending(x => x.IsOpenSize).ThenBy(x => x.InseamId).ToList();
                    ViewData.TemplateInfo.HtmlFieldPrefix = "[" + gid + "]";
                    return PartialView("LineSheetTemplatePartial", model);
                }
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddNewScale(int id, int cid, int gid)
        {
            var cloth = db.Clothes.FirstOrDefault(x => x.ClothesId == id);
            var list = db.Sizes.Where(x => x.SizeGroupId == cloth.SizeGroupId).Select(x => new ClothesScaleSizeClass { ClothesScaleId = 0, ClothesScaleSizeId = 0, SizeId = x.SizeId, Quantity = 0, SizeName = x.Name }).ToList();
            var model = new ClothesScaleClass();
            model.ClothesScaleSizeClass = list;
            model.Name = string.Empty;
            model.InvQty = 0;
            model.IsOpenSize = false;
            model.FitId = -1;
            model.InseamId = -1;
            model.ClothesId = id;
            if (cloth.ClothesScales.Where(x => x.FitId > 0).Any())
            {
                model.selectedFitId = -1;
                model.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                model.FitId = model.FitList.FirstOrDefault().Id;
            }
            if (cloth.ClothesScales.Where(x => x.InseamId > 0).Any())
            {
                model.selectedInseamId = -1;
                model.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                model.InseamId = model.FitList.FirstOrDefault().Id;
            }
            ViewData.TemplateInfo.HtmlFieldPrefix = "[" + cid + "].ClothesScale[" + gid + "]";
            return PartialView(model);
        }

        public string ShowSlider()
        {
            int UserId = 0;
            int StatusId = 0;
            DB.Order lastOrder = null;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            int.TryParse(SiteIdentity.UserId, out UserId);
            if (Session["Order"] == null && Session["EditingOrder"] == null)
                lastOrder = db.Orders.Where(x => (x.AccountId == UserId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
            else if (Session["EditingOrder"] != null)
                lastOrder = db.Orders.Find((Guid)Session["EditingOrder"]);
            else if (Session["Order"] != null)
                lastOrder = db.Orders.Find((Guid)Session["Order"]);
            if (lastOrder != null)
                return "Success";
            else
                return string.Empty;
        }

        [NonAction]
        public bool AddCustomerToQuickBook(int CustomerId)
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
                if (QuickBookStrings.IsSandBox())
                    context.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl;
                context.IppConfiguration.Logger.RequestLog.EnableRequestResponseLogging = true;
                context.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = QuickBookStrings.GetLoc(Type, CustomerId.ToString());
                var service = new DataService(context);
                var customer = new Customer();
                var objCustomer = new Customer();
                var objterm = new Intuit.Ipp.Data.Term();
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
                    objCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompanyName.Trim() + "'").FirstOrDefault();
                    var dbcustomerOptionalInfo = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == regstAccount.AccountId);
                    var dbterms = db.Terms.Where(x => x.TermId == dbcustomerOptionalInfo.TermId && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
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
                    var terms = new QueryService<Intuit.Ipp.Data.Term>(context);
                    string termName = dbterms != null ? (dbterms.Name != null ? dbterms.Name : string.Empty) : "COD";
                    objterm = terms.ExecuteIdsQuery("Select * From Term Where Name='" + termName + "'").FirstOrDefault();
                    #region Comment
                    //if (objterm == null)
                    //{
                    //    objterm = new Intuit.Ipp.Data.Term();
                    //    objterm.Name = dbterms.Name;
                    //    var resultString = System.Text.RegularExpressions.Regex.Match(dbterms.Name, @"\d+").Value;
                    //    if (!string.IsNullOrEmpty(resultString))
                    //        int.Parse(resultString);
                    //    else
                    //    {
                    //      resultString=  "0";
                    //      int.Parse(resultString);
                    //    }
                    //    objterm.domain = "QBO";

                    //    //ItemsChoiceType
                    //    objterm.AnyIntuitObjects = new object[] { (object)resultString };
                    //    //objterm.MetaData.CreateTime = DateTime.UtcNow;
                    //    //objterm.MetaData.LastUpdatedTime = DateTime.UtcNow;
                    //    service.Add(objterm);
                    //}
                    #endregion
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
                            objCustomer.ShipAddr = new PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State, Country = shippingAddress.Country };
                        objCustomer.Fax = new TelephoneNumber() { AreaCode = "01", FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Fax : "" };
                        if (objterm != null)
                            objCustomer.SalesTermRef = new ReferenceType() { name = objterm.Name, Value = objterm.Id };
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
                        if (objterm != null)
                            objCustomer.SalesTermRef = new ReferenceType() { name = objterm.Name, Value = objterm.Id };
                        objCustomer.ResaleNum = dbcustomerOptionalInfo.BusinessReseller;
                        service.Add(objCustomer);
                    }
                    QuickBookStrings.DeleteFiles(Type, CustomerId.ToString());
                    return true;
                }
            }
            catch
            {
                var newFailure = new QuickBookFailureRecord();
                newFailure.FailureFrom = FailureFrom.Customer.ToString();
                newFailure.FailureFromId = CustomerId;
                newFailure.FailureOriginalValue = CompanyName;
                newFailure.ErrorDetails = QuickBookStrings.FailureText(Type, CustomerId.ToString());
                newFailure.FailureOriginalValue = CompanyName;
                newFailure.FailureDate = DateTime.UtcNow;
                db.QuickBookFailureRecords.Add(newFailure);
                db.SaveChanges();
            }
            return false;
        }

        [HttpPost]
        public ActionResult SendMail(string ids, string Subject, string Message, string RefUrl, bool sA, bool hI, string sI, string iM, string future, string deactive)
        {
            //string URL = Request.UrlReferrer.OriginalString;
            try
            {
                //string ContentType = "application/pdf";
                //Response.AppendHeader("Content-Disposition", "inline;filename=LineSheet.pdf");
                //return File(pdfBytes, ContentType);
                //byte[] toPdf = PlatiniWebService.CreatePdf(URL);

                var document = new Document(PageSize.A3, 0, 0, 60, 0);
                var output = new MemoryStream();
                var writer = PdfWriter.GetInstance(document, output);

                writer.CloseStream = false;
                document.Open();
                int? page = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["page"] != null ? Convert.ToInt32(HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["page"].ToString()) : 1;
                int? TypeId = SiteConfiguration.CatID;
                var inMode = !string.IsNullOrEmpty(iM) ? iM : SiteConfiguration.Mode;
                string Ids = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["Ids"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["Ids"].ToString() : string.Empty;
                string pS = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["PageSize"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["PageSize"].ToString() : string.Empty;
                string search = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["search"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["search"].ToString() : null;
                string sort = HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["sortBy"] != null ? HttpUtility.ParseQueryString(Request.UrlReferrer.Query)["sortBy"].ToString() : null;
                int sortBy = 1;
                int.TryParse(sort, out sortBy);
                int? PageSize1 = null;
                int val = 0;
                if (!string.IsNullOrEmpty(pS) && !sA)
                {
                    int.TryParse(pS, out val);
                    PageSize1 = val;
                }
                else if (sA)
                    PageSize1 = 0;
                if (!sA && !string.IsNullOrEmpty(sI))
                    TempData["SelectIds"] = sI;
                var _model = LineSheetData(page, TypeId, Ids, PageSize1, !string.IsNullOrEmpty(future), !string.IsNullOrEmpty(deactive), search, sortBy);
                if (inMode == "View" || inMode == "Edit")
                    document = PlatiniWebService.PdfCreater(document, writer, _model, hI);
                else if (inMode == "Order")
                    document = PlatiniWebService.OrderMode_Pdf(document, writer, _model, hI);
                document.Close();
                byte[] toPdf = new byte[output.Position];
                output.Position = 0;
                output.Read(toPdf, 0, toPdf.Length);

                if (toPdf != null)
                {
                    var emails = ids.Split(',').ToList();
                    if (emails.Count() > 0)
                    {
                        foreach (var email in emails)
                        {
                            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^([\w-\.]+@([\w-]+\.)+[\w-]{2,4})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                emails.Remove(email);
                        }
                    }
                    EmailManager.SendLineSheet(Subject, Message, toPdf, ConfigurationManager.AppSettings["SMTPEmail"].ToString(), emails.ToArray());
                }
                else
                    return Json("The email could not be sent.  Please try again.", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("The email could not be sent.  Please try again.", JsonRequestBehavior.AllowGet);
            }
            return Json("Success", JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetCustomerData(int Id)
        {
            var customer = db.Accounts.Find(Id);
            if (customer != null)
            {
                if (customer.IsActive == true && customer.IsDelete == false)
                {
                    return Json(Data(customer, null), JsonRequestBehavior.AllowGet);
                }
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
        [NonAction]
        public CartOwner Data(Platini.DB.Account customer, Guid? printOrder)
        {
            var model = new CartOwner() { CommunicationId = 0, TermId = 0, ShipVia = 0 };
            model.UserId = customer.AccountId;
            model.Buyer = customer.FirstName + " " + customer.LastName;
            model.Email = customer.Email;
            model.CompanyName = string.Empty;
            if (customer.Companies.FirstOrDefault() != null)
                model.CompanyName = customer.Companies.FirstOrDefault().Name;
            model.BillingAddress = new AddressText() { AddressId = 0 };
            model.ShippingAddress = new AddressText() { AddressId = 0 };
            if (customer.Addresses.Any(x => x.IsActive == true && x.IsDelete == false))
            {
                var Address = customer.Addresses.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.AddressTypeId == (int)AddressTypeEnum.BillingAddress);
                if (Address != null)
                {
                    model.BillingAddress.Line1 = Address.Street;
                    model.BillingAddress.AddressId = Address.AddressId;
                    model.BillingAddress.Line2 = string.Format("{0}, {1}, {2}", Address.City, Address.State, Address.Pincode);
                }
                if (Session["Order"] != null || Session["EditingOrder"] != null || printOrder.HasValue)
                {
                    Guid lastOrder = Guid.Empty;
                    if (Session["Order"] != null)
                        lastOrder = (Guid)Session["Order"];
                    if (Session["EditingOrder"] != null)
                        lastOrder = (Guid)Session["EditingOrder"];
                    if (printOrder.HasValue)
                        lastOrder = printOrder.Value;
                    var order = db.Orders.FirstOrDefault(x => x.OrderId == lastOrder);
                    if (order != null)
                    {
                        if (order.AddressId.HasValue)
                            Address = db.Addresses.FirstOrDefault(x => x.AddressId == order.AddressId.Value);
                        if (order.TermId.HasValue)
                            model.TermId = order.TermId.Value;
                        if (order.ShipViaId.HasValue)
                            model.ShipVia = order.ShipViaId.Value;
                    }
                }
                Address = null;
                Address = customer.Addresses.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false && x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress);
                if (Address != null)
                {
                    model.ShippingAddress.AddressId = Address.AddressId;
                    model.ShippingAddress.Line1 = Address.Street;
                    model.ShippingAddress.Line2 = string.Format("{0}, {1}", Address.City, Address.State);
                    model.ShippingAddress.To = Address.ShipTo;
                    model.ShippingAddress.ZipCode = Address.Pincode;
                }
            }
            model.Fax = model.Phone = string.Empty;
            if (customer.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false) != null)
            {
                var phone = customer.Communications.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false);
                model.CommunicationId = phone.CommunicationId;
                model.Fax = phone.Fax;
                model.Phone = phone.Phone;
            }
            model.isRetail = customer.RoleId == (int)RolesEnum.Customer || customer.RoleId == (int)RolesEnum.User;
            if (customer.CustomerOptionalInfoes.FirstOrDefault() != null)
            {
                var info = customer.CustomerOptionalInfoes.FirstOrDefault();
                if (model.TermId == 0)
                    model.TermId = info.TermId.HasValue ? info.TermId.Value : 0;
                if (model.ShipVia == 0)
                    model.ShipVia = info.ShipViaId.HasValue ? info.ShipViaId.Value : 0;
                model.Discount = info.Discount.HasValue ? info.Discount.Value : 0.0m;
                model.isRetail = info.CustomerType == (int)Platini.Models.CustomerType.Retail;
            }
            var salesPerson = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == model.UserId);
            model.SalesPerson = "Default";
            if (salesPerson != null)
            {
                model.SalesPersonId = salesPerson.SalesPersonId;
                model.SalesPerson = db.Accounts.FirstOrDefault(x => x.AccountId == model.SalesPersonId).Email;
            }
            return model;
        }

        public ActionResult RegistrationAtCart()
        {
            CustomerClass customer = new CustomerClass();
            customer.IsActive = true;
            customer.SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson && x.IsActive == true && x.IsDelete == false).Select(y =>
                new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName, IsSelected = false }).ToList();
            customer.TermList = db.Terms.Where(x => x.IsActive == true && x.IsDelete == false)
                .Select(y => new SelectedListValues { Id = y.TermId, Value = y.Name, IsSelected = false }).ToList();
            customer.BillingAddress = new DB.Address { AddressTypeId = (int)AddressTypeEnum.BillingAddress };
            customer.ShippingAddress = new DB.Address { AddressTypeId = (int)AddressTypeEnum.ShippingAddress };
            customer.Wholesale = true;
            ViewBag.SameAsBillingValue = "true";
            ViewBag.CloseBox = false;
            return PartialView("RegistrationPartial", customer);
        }

        [HttpPost]
        public ActionResult RegistrationAtCart(CustomerClass model)
        {
            ViewBag.CloseBox = false;
            if (SiteIdentity.IsAdmin == "FALSE" && SiteIdentity.Roles == "SalesPerson")
            {
                model.SelectedSalesPerson = Convert.ToInt32(SiteIdentity.UserId);
            }
            model.SalesPersonList = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.SalesPerson).Select(y =>
                new SelectedListValues { Id = y.AccountId, Value = y.FirstName + " " + y.LastName, IsSelected = false }).ToList();
            model.TermList = db.Terms.Where(x => x.IsActive == true && x.IsDelete == false)
                .Select(y => new SelectedListValues { Id = y.TermId, Value = y.Name, IsSelected = false }).ToList();

            ModelState.Remove("AccountId");
            ModelState.Remove("SelectedSalesPerson");
            ModelState.Remove("Email");
            if (db.Accounts.Select(x => x.UserName).Contains(model.Username))
            {
                ModelState.AddModelError("Username", "Record with this User Name already exist.");
                return View("RegistrationPartial", model);
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var dbAccount = new Platini.DB.Account();
                    dbAccount.InjectClass(model);
                    dbAccount.IsActive = model.IsActive;
                    dbAccount.IsDelete = false;
                    dbAccount.RoleId = (int)RolesEnum.Customer;
                    dbAccount.DateCreated = DateTime.UtcNow;
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
                        model.CountryCode = model.CountryCode.Insert(len , "-");
                        model.PhoneNo = model.CountryCode;
                    }
                    var dbCommunication = new Communication
                    {
                        Phone = model.PhoneNo,
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
                    db.CustomerOptionalInfoes.Add(dbcustomerOptionalInfo);
                    db.SaveChanges();

                    if (model.BillingAddress.City != null)
                    {
                        var dbAddress = new DB.Address();
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
                        var dbAddress = new DB.Address();
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
                        var dbAddress = new DB.Address();
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
                        AddCustomerToQuickBook(dbAccount.AccountId);
                    ViewBag.CloseBox = true;
                    ViewBag.Id = id;
                    try
                    {
                        bool check = EmailManager.SendWelcomeEmail(model.Email, cName, model.Username, model.Password);
                        check = EmailManager.SendAdminEmail(cName, model.FirstName + " " + model.LastName, model.PhoneNo, model.Email, dbAccount.AccountId.ToString(), model.BillingAddress.City);
                    }
                    catch { }
                }
                catch
                {
                }
            }
            return PartialView("RegistrationPartial", model);
        }

        public ActionResult SeachStyleNumber(string styleNumber)
        {
            if (!string.IsNullOrEmpty(styleNumber))
            {
                styleNumber = styleNumber.Replace(' ', ',');
                //var retlist = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && x.StyleNumber.Contains(styleNumber)).ToList();
                //if (retlist.Count == 1)
                //    return RedirectToAction("Detail", new { id = retlist.FirstOrDefault().ClothesId });
                TempData["Search"] = styleNumber;
                return RedirectToAction("SeeAll", new { id = styleNumber });
            }
            return RedirectToAction("Index");
        }

        public ActionResult NewArrivals()
        {
            ViewBag.modevalue = "n";
            bool loadMSRP = true;
            if (!string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                {
                    int Type = 0;
                    string type = SiteIdentity.Type;
                    int.TryParse(type, out Type);
                    loadMSRP = Type == (int)Platini.Models.CustomerType.Retail;
                }
                else
                    loadMSRP = false;
            }
            var result = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && x.Clearance == 0 && (x.FutureDeliveryDate == null || x.FutureDeliveryDate == DateTime.MinValue)).OrderByDescending(x => x.DateChanged).Take(50).ToList();
            ClothList cl = new ClothList();
            cl.List = new List<ClothListItem>();

            if (result != null)
            {
                foreach (var item in result)
                {
                    ClothListItem cli = new ClothListItem();
                    cli.StyleNumber = item.StyleNumber;
                    var image = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                    if (image != null)
                    {
                        cli.ImagePath = image.ImagePath;
                    }
                    else
                    {
                        cli.ImagePath = "NO_IMAGE.jpg";
                    }
                    if (loadMSRP)
                        cli.Price = item.MSRP;
                    else
                        cli.Price = item.Price;
                    cli.ClothesId = item.ClothesId;
                    cli.Clearance = 0;
                    cl.List.Add(cli);
                }
            }

            return PartialView("ClothListViewPartial", cl);
        }

        public ActionResult Clearance()
        {
            ViewBag.modevalue = "c";
            bool loadMSRP = true;
            if (!string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                {
                    int Type = 0;
                    string type = SiteIdentity.Type;
                    int.TryParse(type, out Type);
                    loadMSRP = Type == (int)Platini.Models.CustomerType.Retail;
                }
                else
                    loadMSRP = false;
            }
            var result = db.Clothes.Where(x => x.Clearance == 1 && x.IsActive == true && x.IsDelete == false).OrderByDescending(x => x.DateChanged).ToList();
            ClothList cl = new ClothList();
            cl.List = new List<ClothListItem>();
            if (result != null)
            {
                foreach (var item in result)
                {
                    ClothListItem cli = new ClothListItem();
                    cli.StyleNumber = item.StyleNumber;
                    var image = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                    if (image != null)
                    {
                        cli.ImagePath = image.ImagePath;
                    }
                    else
                    {
                        cli.ImagePath = "NO_IMAGE.jpg";
                    }
                    if (loadMSRP)
                        cli.Price = item.MSRP;
                    else
                        cli.Price = item.Price;
                    cli.Clearance = 1;
                    cli.ClothesId = item.ClothesId;
                    cl.List.Add(cli);
                }
            }

            return PartialView("ClothListViewPartial", cl);
        }

        [HttpGet]
        public ActionResult GetRelatedProducts(int clotheId)
        {
            var relatedProductDialogModel = new RelatedProductClass();
            relatedProductDialogModel.Categories = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == 0).ToList().OrderBy(x => x.SortOrder).ToList();
            relatedProductDialogModel.ClothesId = clotheId;
            relatedProductDialogModel.StyleNumber = db.Clothes.Single(x => x.ClothesId == clotheId).StyleNumber;
            var prodList1 = db.RelatedClothes.Where(x => x.ClothesId == clotheId && x.IsActive == true && x.IsDelete == false).Select(x => x.RelClothesId);
            var prodList2 = db.RelatedClothes.Where(x => x.RelClothesId == clotheId && x.IsActive == true && x.IsDelete == false).Select(x => x.ClothesId);
            var prodList = new List<int>();
            prodList.AddRange(prodList1);
            prodList.AddRange(prodList2);
            prodList = prodList.FindAll(x => x != clotheId).Distinct().ToList();
            relatedProductDialogModel.RelatedProducts = db.Clothes.Where(x => prodList.Contains(x.ClothesId) && x.IsActive == true && x.IsDelete == false).Select(y => new RelatedProductListItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, SubCategoryName = db.Categories.FirstOrDefault(x => x.CategoryId == y.Category.ParentId).Name, CategoryName = db.Categories.FirstOrDefault(x => x.CategoryId == db.Categories.FirstOrDefault(q => q.CategoryId == y.Category.ParentId).ParentId).Name, StyleNumber = y.StyleNumber }).ToList();
            return PartialView("RelatedProduct", relatedProductDialogModel);
        }

        public ActionResult DetailNew(int id, string c)
        {
            var model = new DetailViewClass();

            model.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            model.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();

            if (db.Clothes.Where(x => x.ClothesId == id).Any())
            {
                var cloth = db.Clothes.Find(id);
                model.ClothesId = id;
                model.StyleNumber = cloth.StyleNumber;
                model.Color = cloth.Color;
                model.Description = cloth.ClothesDescription;
                model.MSRP = cloth.MSRP.HasValue ? cloth.MSRP.Value : 0.0m;
                model.Price = cloth.Price.HasValue ? cloth.Price.Value : 0.0m;
                model.Cost = cloth.ProductCost.HasValue ? cloth.ProductCost.Value : 0.0m;
                model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                model.CategoryId = cloth.CategoryId;
                model.Note = cloth.AdminNote;
                if (cloth.Clearance == 1)
                {
                    ViewBag.modevalue = "c";
                }
                else
                {
                    ViewBag.modevalue = "";
                }

                var sizeGroupId = cloth.SizeGroupId;
                model.isFuture = cloth.FutureDeliveryDate.HasValue ? (cloth.FutureDeliveryDate.Value != DateTime.MinValue) : false;
                var temp = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && x.CategoryId == cloth.CategoryId
                    && (model.isFuture ? (x.FutureDeliveryDate.HasValue ? x.FutureDeliveryDate.Value != DateTime.MinValue : true) : true)).
                    ToList().OrderBy(x => x.ClothesId);
                if (temp.Any())
                {
                    model.NextStyle = ReferenceEquals(temp.FirstOrDefault(x => x.ClothesId > cloth.ClothesId), null) ? 0 : temp.FirstOrDefault(x => x.ClothesId > cloth.ClothesId).ClothesId;
                    model.PrevStyle = ReferenceEquals(temp.FirstOrDefault(x => x.ClothesId < cloth.ClothesId), null) ? 0 : temp.FirstOrDefault(x => x.ClothesId < cloth.ClothesId).ClothesId;
                }

                List<int?> FitList = cloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                List<int?> InseamList = cloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();

                model.FitList = model.FitList.Where(x => FitList.Contains(x.Id)).ToList();
                model.InseamList = model.InseamList.Where(x => InseamList.Contains(x.Id)).ToList();

                if (FitList.Count() == 0)
                    FitList.Add(null);

                if (InseamList.Count() == 0)
                    InseamList.Add(null);

                foreach (var fitid in FitList)
                {
                    foreach (var inseamid in InseamList)
                    {
                        var scaleList = db.ClothesScales.Where(x => x.ClothesId == id).ToList();
                        if (scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid).Any())
                        {
                            var clothesScales = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true);
                            foreach (var scale in clothesScales)
                            {
                                var openSizesForOS = new List<ClothesScaleSizeClass>();
                                var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                foreach (var item in sSQOpenSize)
                                {
                                    ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                    scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                    scaleRow.ClothesScaleId = item.ClothesScaleId;
                                    scaleRow.SizeId = item.SizeId;
                                    scaleRow.SizeName = item.Size.Name;
                                    scaleRow.Quantity = item.Quantity;
                                    openSizesForOS.Add(scaleRow);
                                }
                                var availableOpenSizeItem = new ClothesScaleClass();
                                availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizesForOS);
                                availableOpenSizeItem.InseamId = scale.InseamId;
                                availableOpenSizeItem.FitId = scale.FitId;
                                availableOpenSizeItem.IsOpenSize = true;
                                availableOpenSizeItem.Name = scale.Name;
                                availableOpenSizeItem.InvQty = scale.InvQty;
                                if (fitid.HasValue)
                                    availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                if (inseamid.HasValue)
                                    availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                availableOpenSizeItem.ClothesId = scale.ClothesId;
                                availableOpenSizeItem.ClothesScaleId = scale.ClothesScaleId;
                                model.AvailableOpenSizes.Add(availableOpenSizeItem);
                            }

                            clothesScales = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == false);
                            foreach (var scale in clothesScales)
                            {
                                var openSizesForPP = new List<ClothesScaleSizeClass>();
                                var sSQPrePacks = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                foreach (var item in sSQPrePacks)
                                {
                                    ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                    scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                    scaleRow.ClothesScaleId = item.ClothesScaleId;
                                    scaleRow.SizeId = item.SizeId;
                                    scaleRow.SizeName = item.Size.Name;
                                    scaleRow.Quantity = item.Quantity;
                                    openSizesForPP.Add(scaleRow);
                                }
                                var availablePrePacksItem = new ClothesScaleClass();
                                availablePrePacksItem.ClothesScaleSizeClass.AddRange(openSizesForPP);
                                availablePrePacksItem.InseamId = scale.InseamId;
                                availablePrePacksItem.FitId = scale.FitId;
                                availablePrePacksItem.IsOpenSize = false;
                                if (fitid.HasValue)
                                    availablePrePacksItem.selectedFitId = fitid.Value;
                                if (inseamid.HasValue)
                                    availablePrePacksItem.selectedInseamId = inseamid.Value;
                                availablePrePacksItem.Name = scale.Name;
                                availablePrePacksItem.InvQty = scale.InvQty;
                                if (fitid.HasValue)
                                    availablePrePacksItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                if (inseamid.HasValue)
                                    availablePrePacksItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                availablePrePacksItem.ClothesId = scale.ClothesId;
                                availablePrePacksItem.ClothesScaleId = scale.ClothesScaleId;
                                model.AvailablePrePacks.Add(availablePrePacksItem);
                            }
                        }
                        else
                        {

                        }
                    }
                }
                var Category = db.Categories.Find(cloth.CategoryId);
                var SubCategory = db.Categories.Find(Category.ParentId);
                SiteConfiguration.MainID = SubCategory.ParentId;
                SiteConfiguration.SubID = Category.ParentId;
                SiteConfiguration.CatID = cloth.CategoryId;
                List<RelatedProductsItem> dvcList = new List<RelatedProductsItem>();
                dvcList = db.RelatedClothes.Where(x => x.ClothesId == model.ClothesId && x.IsActive == true && x.IsDelete == false).Select(y => new RelatedProductsItem { ClothesId = y.RelClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.Cloth1.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.RelClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.RelClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = db.Clothes.FirstOrDefault(x => x.ClothesId == y.RelClothesId).Price }).ToList();
                model.RelatedProducts = dvcList;
                dvcList = db.RelatedColors.Where(x => x.ClothesId == model.ClothesId && x.IsActive == true && x.IsDelete == false).Select(y => new RelatedProductsItem { ClothesId = y.RelClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.Cloth1.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.RelClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.RelClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = db.Clothes.FirstOrDefault(x => x.ClothesId == y.RelClothesId).Price }).ToList();
                model.RelatedColors = dvcList;
                List<RelatedProductsItem> masterList = null;
                if (!string.IsNullOrEmpty(c))
                    masterList = db.Clothes.Where(x => x.CategoryId == model.CategoryId && x.IsActive == true && x.IsDelete == false).OrderBy(x => x.Clearance).ThenByDescending(x => x.DateChanged).Select(y => new RelatedProductsItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = y.Price }).ToList();
                else
                {
                    var cIds = db.Categories.Where(x => x.IsActive == true && x.IsDelete == false && x.ParentId == SubCategory.CategoryId).ToList().Select(x => x.CategoryId);
                    masterList = db.Clothes.Where(x => cIds.Contains(x.CategoryId) && x.IsActive == true && x.IsDelete == false).OrderBy(x => x.Clearance).ThenByDescending(x => x.DateChanged).Select(y => new RelatedProductsItem { ClothesId = y.ClothesId, SubCategoryTypeName = y.Category.Name, StyleNumber = y.StyleNumber, ImagePath = db.ClothesImages.Any(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete) ? db.ClothesImages.Where(x => x.ClothesId == y.ClothesId && x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath : "", Price = y.Price }).ToList();

                }
                var index = masterList.IndexOf(masterList.FirstOrDefault(x => x.ClothesId == model.ClothesId));
                var list1 = masterList.Take(index).ToList();
                var list2 = masterList.Skip(index + 1).ToList();
                model.MoreProducts.AddRange(list2);
                model.MoreProducts.AddRange(list1);
            }
            return View(model);
        }

        //public ActionResult PlaceOrderforCloth(DetailViewClass Cloth)
        //{

        //}

        public ActionResult EditClothe(DetailViewClass Cloth)
        {
            List<string> Errors = new List<string>();
            if (Cloth != null)
            {

                var existCloth = db.Clothes.FirstOrDefault(x => x.ClothesId == Cloth.ClothesId && x.IsDelete == false);
                if (existCloth != null)
                {
                    //existCloth.StyleNumber = !string.IsNullOrEmpty(Cloth.StyleNumber) ? Cloth.StyleNumber : existCloth.StyleNumber;
                    //existCloth.ProductCost = Cloth.Cost;
                    //existCloth.Price = Cloth.Price;
                    //existCloth.MSRP = Cloth.MSRP;
                    //existCloth.Color = Cloth.Color;
                    //existCloth.Clearance = Cloth.Clearance;
                    //existCloth.DateUpdated = DateTime.UtcNow;
                    //db.SaveChanges();
                    if (Cloth.AvailableOpenSizes != null)
                    {
                        foreach (var item in Cloth.AvailablePrePacks)
                        {
                            if (item != null)
                            {
                                var pack = db.ClothesScales.Find(item.ClothesScaleId);
                                if (pack != null)
                                {
                                    pack.Name = item.Name;
                                    pack.InvQty = item.InvQty;
                                    foreach (var size in item.ClothesScaleSizeClass)
                                    {
                                        if (size != null)
                                        {
                                            var dbSize = db.ClothesScaleSizes.Find(size.ClothesScaleSizeId);
                                            if (dbSize != null)
                                            {
                                                dbSize.Quantity = size.Quantity.HasValue ? size.Quantity.Value : dbSize.Quantity;
                                                dbSize.DateUpdated = DateTime.UtcNow;
                                            }
                                        }
                                    }
                                    pack.DateUpdated = DateTime.UtcNow;
                                }
                                db.SaveChanges();
                            }
                        }
                    }
                    if (Cloth.AvailableOpenSizes != null)
                    {
                        foreach (var item in Cloth.AvailableOpenSizes)
                        {
                            if (item != null)
                            {
                                var pack = db.ClothesScales.Find(item.ClothesScaleId);
                                if (pack != null)
                                {
                                    foreach (var size in item.ClothesScaleSizeClass)
                                    {
                                        if (size != null)
                                        {
                                            var dbSize = db.ClothesScaleSizes.Find(size.ClothesScaleSizeId);
                                            if (dbSize != null)
                                            {
                                                dbSize.Quantity = size.Quantity.HasValue ? size.Quantity.Value : dbSize.Quantity;
                                                dbSize.DateUpdated = DateTime.UtcNow;
                                            }
                                        }
                                    }
                                    pack.DateUpdated = DateTime.UtcNow;
                                }
                                db.SaveChanges();
                            }
                        }
                    }
                    var scales = db.ClothesScales.Where(x => x.ClothesId == Cloth.ClothesId && x.IsActive == true && x.IsDelete == false).ToList();
                    int quant = (scales.Where(x => x.IsOpenSize == true).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                         + (scales.Where(x => x.IsOpenSize == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                    if (!existCloth.OriginalQty.HasValue)
                        existCloth.OriginalQty = quant;
                    existCloth.AdjustQty = quant - existCloth.OriginalQty.Value;
                    existCloth.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                }
                else
                    Errors.Add("Cloth not found");
                if (Errors.Count > 0)
                    TempData["PageMessage"] = string.Join(". ", Errors);
                return RedirectToAction("Detail", new { id = existCloth.StyleNumber });

            }
            TempData["PageMessage"] = "No cloth received.";
            return RedirectToAction("Index");

        }

        public ActionResult MassUploadImages(int TypeId)
        {
            ViewBag.Ids = new List<int>();
            if (TypeId > 0)
            {
                ViewBag.Ids = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && x.CategoryId == TypeId).ToList().
                    Where(x => x.ClothesImages.Count(y => y.IsActive == true && y.IsDelete == false) == 0).OrderByDescending(x => x.ClothesId).
                    ThenBy(x => x.Clearance).ThenBy(x => x.SortOrder).ThenByDescending(x => x.DateCreated).Select(x => x.ClothesId).ToList();
                ViewBag.TypeId = TypeId;
                return View();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult mFileUpload(IEnumerable<HttpPostedFileBase> files)
        {

            foreach (HttpPostedFileBase file in files)
            {
                var image = SaveUpdateClothesImage2(file);
                if (image != null)
                    return Json(image);
            }
            return Json("error");
        }

        public ActionResult MassCloth(int Id, bool isLast)
        {
            var cloth = db.Clothes.Find(Id);
            if (cloth != null)
            {
                var model = new ClothImgUpload()
                {
                    ClothesId = cloth.ClothesId,
                    StyleNumber = cloth.StyleNumber,
                    isLast = isLast
                };
                model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).Select(
                        x => new imgList() { Name = x.ImageName, Path = x.ImagePath, SO = (x.SortOrder.HasValue ? x.SortOrder.Value : int.MaxValue), aId = x.ClothesImageId }).OrderBy(x => x.SO).ToList();
                return PartialView("MassClothPartial", model);
            }
            return Json("Not Found", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AssignImages(string Ids, int Id)
        {
            var cloth = db.Clothes.Find(Id);
            if (!string.IsNullOrEmpty(Ids) && cloth != null)
            {
                string style = cloth.StyleNumber;
                var GuidList = Ids.Split(',').Select(s => Guid.Parse(s)).ToList();
                var imageList = GetImageList();
                var assignList = imageList.Where(x => GuidList.Contains(x.Id)).ToList();
                if (assignList.Any())
                {
                    foreach (var img in assignList)
                    {
                        var clothesImage = new ClothesImage();
                        clothesImage.ClothesId = cloth.ClothesId;
                        clothesImage.ImageName = img.Name;
                        clothesImage.ImagePath = img.Path;
                        clothesImage.SortOrder = img.SO;
                        clothesImage.IsActive = true;
                        clothesImage.IsDelete = false;
                        clothesImage.DateCreated = clothesImage.DateUpdated = DateTime.UtcNow;
                        db.ClothesImages.Add(clothesImage);
                        db.SaveChanges();
                        imageList.Remove(img);
                    }
                    Session["ImageList"] = imageList;
                    bool updated = false;
                    int existLength = cloth.StyleNumber.Length;
                    var mainImage = db.ClothesImages.Where(x => x.ClothesId == cloth.ClothesId && x.IsActive && !x.IsDelete && !string.IsNullOrEmpty(x.ImageName) && x.ImageName.Length < existLength).OrderBy(x => x.ImageName.Length).FirstOrDefault();
                    if (mainImage != null)
                    {
                        mainImage.SortOrder = 0;
                        var existcloth = db.Clothes.FirstOrDefault(x => x.ClothesId != cloth.ClothesId && x.StyleNumber == mainImage.ImageName);
                        if (existcloth == null)
                            cloth.StyleNumber = mainImage.ImageName;
                        else
                            cloth.StyleNumber = mainImage.ImageName + "_" + cloth.ClothesId;
                        cloth.DateUpdated = DateTime.UtcNow;
                        updated = true;
                        style = cloth.StyleNumber;
                        db.SaveChanges();
                        if (QuickBookStrings.UseQuickBook())
                            AddProductToQuickBook(cloth.ClothesId);
                    }
                    if (updated)
                    {
                        db.ClothesImages.Where(x => x.ClothesId == cloth.ClothesId && x.IsActive && !x.IsDelete).ToList().ForEach(x => { x.SortOrder = x.SortOrder + 1; x.DateUpdated = DateTime.UtcNow; });
                        db.SaveChanges();
                    }
                    assignList = db.ClothesImages.Where(x => x.ClothesId == cloth.ClothesId && x.IsActive && !x.IsDelete).ToList().Select(
                        x => new imgList() { Name = x.ImageName, Path = x.ImagePath, SO = (x.SortOrder.HasValue ? x.SortOrder.Value : int.MaxValue), aId = x.ClothesImageId }).OrderBy(x => x.SO).ToList();
                    return Json(new { Images = assignList, StyleNumber = style }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { Images = assignList, StyleNumber = style }, JsonRequestBehavior.AllowGet);
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        public imgList SaveUpdateClothesImage2(HttpPostedFileBase upPic)
        {
            string fileName;
            System.Drawing.Imaging.ImageFormat format;
            if (VerifyImage(upPic.ContentType.ToLower(), out fileName, out format))
            {
                int SO = 0;
                var List = GetImageList();
                if (List.Any())
                    SO = List.Last().SO;
                var clothesImage = new imgList();
                clothesImage.Name = upPic.FileName.Split('.').FirstOrDefault();
                clothesImage.Path = fileName;
                clothesImage.SO = ++SO;
                List.Add(clothesImage);
                Session["ImageList"] = List;
                System.Drawing.Image.FromStream(upPic.InputStream).Save(Server.MapPath("~/Library/Uploads/" + fileName), format);
                var arrWeb = ImgHelper.ResizeImage(upPic.InputStream, 500, 750, "White", 90, format);
                var arrMob = ImgHelper.ResizeImage(upPic.InputStream, 150, 225, "White", 90, format);
                using (var ms = new MemoryStream(arrWeb))
                {
                    System.Drawing.Image.FromStream(ms).Save(Server.MapPath("~/Library/Uploads/WebThumb/" + fileName), format);
                }
                using (var ms = new MemoryStream(arrMob))
                {
                    System.Drawing.Image.FromStream(ms).Save(Server.MapPath("~/Library/Uploads/MobileThumb/" + fileName), format);
                }

                return clothesImage;
            }
            return null;
        }

        [NonAction]
        public List<imgList> GetImageList()
        {
            if (Session["ImageList"] != null)
                return (List<imgList>)Session["ImageList"];
            else
                return new List<imgList>();
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        public JsonResult SubmitEmail(string email)
        {
            var data = db.Accounts.FirstOrDefault(x => x.Email == email && x.IsDelete == false && x.IsActive == true);
            if (data != null)
            {
                string password = Encoding.ASCII.GetString(data.Password);

                if (EmailManager.SendForgotEmail(email, password))
                {
                    return Json(new { Response = "Success" }, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { Response = "Failure" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetScales(int Id)
        {
            var cloth = db.Clothes.Find(Id);
            bool fitInseam = false;
            bool hasInventory = false;
            bool Admin = false;
            bool isAvailable = false;
            if (cloth != null)
            {
                isAvailable = true;
                string category = cloth.Category.Name.ToLower();
                fitInseam = cloth.ClothesScales.Any(x => x.IsActive == true && x.IsDelete == false && x.InseamId.HasValue && x.FitId.HasValue);
                fitInseam = fitInseam ? fitInseam : ((category == "bottom" || category == "pants + shorts" || category == "suits" || category == "shorts" ||
                    category == "pants" || category=="jeans") ? true : false);
                bool opensizeInventory = cloth.ClothesScales.Any(x => x.IsActive == true && x.IsDelete == false && x.IsOpenSize.Value == true && x.ClothesScaleSizes.Any(y => y.Quantity > 0));
                bool prepackInventory = cloth.ClothesScales.Any(x => x.IsActive == true && x.IsDelete == false && x.IsOpenSize.Value == false && !string.IsNullOrEmpty(x.Name) && x.InvQty > 0
                   && x.ClothesScaleSizes.Any(y => y.Quantity > 0));
                hasInventory = opensizeInventory || prepackInventory;
                Admin = SiteIdentity.Roles == RolesEnum.Admin.ToString() || SiteIdentity.Roles == RolesEnum.SuperAdmin.ToString();
            }
            return Json(new { aV = isAvailable, fI = fitInseam, hI = hasInventory, iA = Admin }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddByStyleNo(int Id)
        {
            Session.Remove("PaypalToken");
            Session.Remove("WasCleared");
            string result = "-1";
            bool created = false;
            var cloth = db.Clothes.Find(Id);
            int UserId = 0;
            int StatusId = 0;
            DB.Order lastOrder = null;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            int.TryParse(SiteIdentity.UserId, out UserId);
            bool isCookie = UserId == 0;
            bool loadRetail = isCookie;
            if (!loadRetail)
                loadRetail = SiteIdentity.Type == ((int)Platini.Models.CustomerType.Retail).ToString();
            if (Session["Order"] == null && Session["EditingOrder"] == null && !isCookie)
                lastOrder = db.Orders.Where(x => (x.AccountId == UserId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
            else if (Session["EditingOrder"] != null)
                lastOrder = db.Orders.Find((Guid)Session["EditingOrder"]);
            else if (Session["Order"] != null)
                lastOrder = db.Orders.Find((Guid)Session["Order"]);
            else
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        lastOrder = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                    }
                    catch
                    {
                        lastOrder = null;
                    }
                }
            }
            if (lastOrder == null)
            {
                lastOrder = new DB.Order();
                lastOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                lastOrder.OrderNumber = string.Empty;
                lastOrder.AccountId = UserId;
                if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower())
                {
                    var sp = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == UserId);
                    if (sp != null)
                        lastOrder.EmployeeId = sp.SalesPersonId;
                }
                if (!lastOrder.EmployeeId.HasValue)
                    lastOrder.EmployeeId = UserId;
                lastOrder.CreatedOn = DateTime.UtcNow;
                lastOrder.DateCreated = lastOrder.DateUpdated = DateTime.UtcNow;
                lastOrder.StatusId = StatusId;
                //lastOrder.ShippingCost = loadRetail ? 7 : 0;
                lastOrder.ShippingCost = 0;
                decimal disc = 0.0m;
                if (UserId > 0)
                {
                    var ci = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == UserId);
                    if (ci != null)
                        disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                }
                lastOrder.Discount = disc;
                lastOrder.IsDelete = false;
                lastOrder.IsSentToQuickBook = false;
                if (!isCookie)
                    db.Orders.Add(lastOrder);
                lastOrder.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                if (!isCookie)
                {
                    db.SaveChanges();
                    Session["Order"] = lastOrder.OrderId;
                }
                created = true;
            }
            if (cloth != null && lastOrder != null)
            {

                bool isAddded = false;
                var clothScales = cloth.ClothesScales.Where(x => x.IsActive == true && x.IsDelete == false).ToList();
                var scaleIds = clothScales.Where(x => x.IsOpenSize == false && !string.IsNullOrEmpty(x.Name) && x.InvQty > 0 && x.ClothesScaleSizes.Sum(y => y.Quantity) > 0).Select(x => x.ClothesScaleId);

                if (lastOrder.OrderScales.Any(x => scaleIds.Contains(x.ClothesScaleId)) && !loadRetail)
                    result = "0";
                else if (loadRetail)
                {
                    var scaleIdsOpen = clothScales.Where(x => x.IsOpenSize == true).Select(x => x.ClothesScaleId);
                    var sizeIds = db.ClothesScaleSizes.Where(x => scaleIdsOpen.Contains(x.ClothesScaleId)).ToList().Select(x => x.ClothesScaleSizeId);
                    if (lastOrder.OrderSizes.Any(x => sizeIds.Contains(x.ClothesSizeId)))
                        result = "0";
                    else
                    {
                        var opensizeList = clothScales.FirstOrDefault(x => x.IsOpenSize == true).ClothesScaleSizes;
                        foreach (var size in opensizeList)
                        {
                            bool RtlAvlbl = true;
                            if (!size.Quantity.HasValue || size.Quantity.Value <= 0)
                                if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == (size.ClothesScale.FitId.HasValue ? size.ClothesScale.FitId.Value : 0) && (x.InseamId.HasValue ? x.InseamId.Value : 0) == (size.ClothesScale.InseamId.HasValue ? size.ClothesScale.InseamId.Value : 0) && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == size.ClothesScale.ClothesId).Count() <= 0)
                                    RtlAvlbl = false;
                                else if (db.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == (size.ClothesScale.FitId.HasValue ? size.ClothesScale.FitId.Value : 0) && (x.InseamId.HasValue ? x.InseamId.Value : 0) == (size.ClothesScale.InseamId.HasValue ? size.ClothesScale.InseamId.Value : 0) && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == size.ClothesScale.ClothesId).ToList().
                                    SelectMany(x => db.ClothesScaleSizes.Where(y => y.SizeId == size.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                    RtlAvlbl = false;
                            if (RtlAvlbl)
                            {
                                var newSize = new OrderSize();
                                newSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                newSize.OrderId = lastOrder.OrderId;
                                newSize.ClothesId = size.ClothesScale.ClothesId;
                                newSize.ClothesSizeId = size.ClothesScaleSizeId;
                                newSize.Quantity = isAddded ? 0 : 1;
                                newSize.PackedQty = 0;
                                newSize.IsConfirmed = false;
                                newSize.DateCreated = newSize.DateUpdated = DateTime.UtcNow;
                                if (!isCookie)
                                {
                                    db.OrderSizes.Add(newSize);
                                    db.SaveChanges();
                                }
                                else
                                    lastOrder.OrderSizes.Add(newSize);
                                isAddded = true;
                            }
                        }
                        result = "1";
                    }
                }
                else
                {

                    var clothScaleList = clothScales.Where(x => x.IsOpenSize == false && !string.IsNullOrEmpty(x.Name) && x.InvQty > 0 && x.IsActive == true && x.IsDelete == false).ToList();
                    if (clothScaleList != null)
                    {
                        foreach (var scale in clothScaleList)
                        {
                            var newPack = new OrderScale();
                            newPack.OrderScaleId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            newPack.OrderId = lastOrder.OrderId;
                            newPack.ClothesId = scale.ClothesId;
                            newPack.ClothesScaleId = scale.ClothesScaleId;
                            newPack.Quantity = 0;
                            newPack.PackedQty = 0;
                            newPack.IsConfirmed = false;
                            newPack.DateCreated = newPack.DateUpdated = DateTime.UtcNow;
                            db.OrderScales.Add(newPack);
                            db.SaveChanges();
                        }
                    }
                    var lst = cloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).ToList();
                    if (lst != null)
                    {
                        foreach (var os in lst)
                        {
                            foreach (var size in os.ClothesScaleSizes)
                            {
                                var newSize = new OrderSize();
                                newSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                newSize.OrderId = lastOrder.OrderId;
                                newSize.ClothesId = os.ClothesId;
                                newSize.ClothesSizeId = size.ClothesScaleSizeId;
                                newSize.Quantity = size.Quantity.HasValue ? (size.Quantity.Value > 0 ? (int?)0 : null) : null;
                                newSize.PackedQty = 0;
                                newSize.IsConfirmed = false;
                                newSize.DateCreated = newSize.DateUpdated = DateTime.UtcNow;
                                db.OrderSizes.Add(newSize);
                                db.SaveChanges();
                            }
                        }
                    }
                    result = "1";
                }
            }
            result = lastOrder == null ? "2" : result;
            if (result == "1")
            {
                if (isCookie)
                {
                    if (lastOrder.OrderSizes.Any())
                        lastOrder.OriginalQty = lastOrder.OrderSizes.Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                    HttpCookie ordCookie = new HttpCookie("ordCookie");
                    ordCookie.Path = "/"; 
                    ordCookie.Value = JsonConvert.SerializeObject(lastOrder);
                    ordCookie.Expires = DateTime.Now.AddMonths(1);
                    if (created)
                        Response.Cookies.Add(ordCookie);
                    else
                    {
                        //Response.Cookies.Set(ordCookie);
                        Response.Cookies["ordCookie"].Value = ordCookie.Value;
                        Response.Cookies["ordCookie"].Expires = ordCookie.Expires;
                    }
                }
                TempData["CartSuccess"] = "abc";
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DetailPartial(int id, string from, string part, string pagetype = "")
        {
            var model = new DetailViewClass();

            model.FitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
            model.InseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
            ViewBag.LoadPart = part;
            bool loadRetail = false;
            if (string.IsNullOrEmpty(SiteIdentity.UserId))
                loadRetail = true;
            else
            {
                int userId = int.Parse(SiteIdentity.UserId);
                var user = db.Accounts.Find(userId);
                if (user != null)
                {
                    if (user.RoleId == (int)RolesEnum.Customer || user.RoleId == (int)RolesEnum.User)
                    {
                        if (user.CustomerOptionalInfoes.Count > 0)
                            loadRetail = user.CustomerOptionalInfoes.FirstOrDefault().CustomerType == (int)Platini.Models.CustomerType.Retail;
                        else
                            loadRetail = true;
                    }
                }
            }
            if (db.Clothes.Where(x => x.ClothesId == id).Any())
            {
                var cloth = db.Clothes.Find(id);
                model.ClothesId = id;
                model.StyleNumber = cloth.StyleNumber;
                model.Color = cloth.Color;
                model.MSRP = cloth.MSRP.HasValue ? cloth.MSRP.Value : 0.0m;
                model.Price = cloth.Price.HasValue ? cloth.Price.Value : 0.0m;
                model.DiscountedMSRP = cloth.DiscountedMSRP.HasValue ? cloth.DiscountedMSRP : 0.0m;
                model.DiscountedPrice = cloth.DiscountedPrice.HasValue ? cloth.DiscountedPrice : 0.0m;
                model.Cost = cloth.ProductCost.HasValue ? cloth.ProductCost.Value : 0.0m;
                model.Images = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).ToList();
                model.CategoryId = cloth.CategoryId;
                var sizeGroupId = cloth.SizeGroupId;
                model.isFuture = cloth.FutureDeliveryDate.HasValue ? (cloth.FutureDeliveryDate.Value != DateTime.MinValue) : false;
                List<int?> FitList = cloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).OrderBy(x => x.FitId).Select(y => y.FitId).Distinct().ToList();
                List<int?> InseamList = cloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).OrderBy(x => x.InseamId).Select(y => y.InseamId).Distinct().ToList();
                model.FitList = model.FitList.Where(x => FitList.Contains(x.Id)).OrderBy(x => x.Id).ToList();
                model.InseamList = model.InseamList.Where(x => InseamList.Contains(x.Id)).OrderBy(x => x.Id).ToList();

                if (FitList.Count() == 0)
                    FitList.Add(null);

                if (InseamList.Count() == 0)
                    InseamList.Add(null);

                foreach (var fitid in FitList)
                {
                    foreach (var inseamid in InseamList)
                    {
                        var scaleList = db.ClothesScales.Where(x => x.ClothesId == id).ToList();
                        if (scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid).Any())
                        {
                            var clothesScales = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == false).OrderBy(x => x.InseamId);
                            foreach (var scale in clothesScales)
                            {
                                var openSizesForPP = new List<ClothesScaleSizeClass>();
                                var sSQPrePacks = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                foreach (var item in sSQPrePacks)
                                {
                                    ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                    scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                    scaleRow.ClothesScaleId = item.ClothesScaleId;
                                    scaleRow.SizeId = item.SizeId;
                                    scaleRow.SizeName = item.Size.Name;
                                    scaleRow.Quantity = item.Quantity;
                                    openSizesForPP.Add(scaleRow);
                                }
                                var availablePrePacksItem = new ClothesScaleClass();
                                availablePrePacksItem.ClothesScaleSizeClass.AddRange(openSizesForPP);
                                availablePrePacksItem.InseamId = scale.InseamId;
                                availablePrePacksItem.FitId = scale.FitId;
                                availablePrePacksItem.IsOpenSize = false;
                                if (fitid.HasValue)
                                    availablePrePacksItem.selectedFitId = fitid.Value;
                                if (inseamid.HasValue)
                                    availablePrePacksItem.selectedInseamId = inseamid.Value;
                                availablePrePacksItem.Name = scale.Name;
                                availablePrePacksItem.InvQty = scale.InvQty;
                                if (fitid.HasValue)
                                    availablePrePacksItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                if (inseamid.HasValue)
                                    availablePrePacksItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                availablePrePacksItem.ClothesId = scale.ClothesId;
                                availablePrePacksItem.ClothesScaleId = scale.ClothesScaleId;
                                model.AvailablePrePacks.Add(availablePrePacksItem);
                            }
                            clothesScales = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true).OrderBy(x => x.InseamId);
                            foreach (var scale in clothesScales)
                            {
                                var openSizesForOS = new List<ClothesScaleSizeClass>();
                                var sSQOpenSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
                                foreach (var item in sSQOpenSize)
                                {
                                    ClothesScaleSizeClass scaleRow = new ClothesScaleSizeClass();
                                    scaleRow.ClothesScaleSizeId = item.ClothesScaleSizeId;
                                    scaleRow.ClothesScaleId = item.ClothesScaleId;
                                    scaleRow.SizeId = item.SizeId;
                                    scaleRow.SizeName = item.Size.Name;
                                    scaleRow.Quantity = item.Quantity;
                                    scaleRow.RtlAvlbl = true;
                                    if (!scaleRow.Quantity.HasValue || scaleRow.Quantity <= 0)
                                        if (!model.AvailablePrePacks.Any(x => x.FitId == fitid && x.InseamId == inseamid && x.InvQty > 0))
                                            scaleRow.RtlAvlbl = false;
                                        else if (model.AvailablePrePacks.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.InvQty > 0).SelectMany(x =>
                                            x.ClothesScaleSizeClass.Where(y => y.SizeId == scaleRow.SizeId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
                                            scaleRow.RtlAvlbl = false;
                                    openSizesForOS.Add(scaleRow);
                                }
                                var availableOpenSizeItem = new ClothesScaleClass();
                                availableOpenSizeItem.ClothesScaleSizeClass.AddRange(openSizesForOS);
                                availableOpenSizeItem.InseamId = scale.InseamId;
                                availableOpenSizeItem.FitId = scale.FitId;
                                availableOpenSizeItem.IsOpenSize = true;
                                availableOpenSizeItem.Name = scale.Name;
                                availableOpenSizeItem.InvQty = scale.InvQty;
                                if (fitid.HasValue)
                                    availableOpenSizeItem.FitName = db.Fits.Single(x => x.FitId == fitid).Name;
                                if (inseamid.HasValue)
                                    availableOpenSizeItem.InseamName = db.Inseams.Single(x => x.InseamId == inseamid).Name;
                                availableOpenSizeItem.ClothesId = scale.ClothesId;
                                availableOpenSizeItem.ClothesScaleId = scale.ClothesScaleId;
                                model.AvailableOpenSizes.Add(availableOpenSizeItem);
                            }
                        }
                        else
                        {

                        }
                    }
                }
                var Category = db.Categories.Find(cloth.CategoryId);
                var SubCategory = db.Categories.Find(Category.ParentId);
                SiteConfiguration.MainID = SubCategory.ParentId;
                SiteConfiguration.SubID = Category.ParentId;
                SiteConfiguration.CatID = cloth.CategoryId;
                if (pagetype == "cart")
                    model.PageType = "cart";
            }
            if (!string.IsNullOrEmpty(from))
                return PartialView("DetailPartial", model);
            if (!loadRetail)
                return PartialView("DetailPartialNew", model);
            else
                return PartialView("DetailPartialRetail", model);
        }

        [HttpPost]
        public ActionResult DetailPartial(DetailViewClass Items)
        {
            bool created = false;
            int UserId = 0;
            int StatusId = 0;

            int.TryParse(SiteIdentity.UserId, out UserId);
            List<string> Errors = new List<string>();
            DB.Order lastOrder = null;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            if (Session["Order"] == null && Session["EditingOrder"] == null)
            {
                lastOrder = db.Orders.Where(x => (x.AccountId == UserId) && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();

                Session["Order"] = lastOrder.OrderId;
            }
            else if (Session["EditingOrder"] != null)
            {
                var OrderId = (Guid)Session["EditingOrder"];
                lastOrder = db.Orders.Find(OrderId);

            }
            else if (Session["Order"] != null)
            {
                var OrderId = (Guid)Session["Order"];
                lastOrder = db.Orders.Find(OrderId);
            }
            if (lastOrder == null)
            {
                lastOrder = new DB.Order();
                lastOrder.OrderId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                lastOrder.OrderNumber = string.Empty;
                lastOrder.AccountId = UserId;
                if (SiteIdentity.Roles.ToLower() == RolesEnum.Customer.ToString().ToLower())
                {
                    var sp = db.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == UserId);
                    if (sp != null)
                        lastOrder.EmployeeId = sp.SalesPersonId;
                }
                if (!lastOrder.EmployeeId.HasValue)
                    lastOrder.EmployeeId = UserId;
                lastOrder.CreatedOn = DateTime.UtcNow;
                lastOrder.DateCreated = lastOrder.DateUpdated = DateTime.UtcNow;
                lastOrder.StatusId = StatusId;
                lastOrder.ShippingCost = 0;
                decimal disc = 0.0m;
                if (UserId > 0)
                {
                    var ci = db.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == UserId);
                    if (ci != null)
                        disc = ci.Discount.HasValue ? ci.Discount.Value : disc;
                }
                lastOrder.Discount = disc;
                lastOrder.IsDelete = false;
                lastOrder.IsSentToQuickBook = false;
                lastOrder.TagId = db.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                db.Orders.Add(lastOrder);
                db.SaveChanges();
                created = true;
            }
            if (Items != null)
            {
                if (Items.AvailablePrePacks != null)
                {
                    foreach (var item in Items.AvailablePrePacks)
                    {
                        if (item != null)
                        {
                            if (item.ClothesScaleId > 0)// && (item.InvQty.HasValue ? item.InvQty.Value > 0 : false))
                            {
                                var pack = db.ClothesScales.Find(item.ClothesScaleId);
                                var ordPack = db.OrderScales.FirstOrDefault(x => x.ClothesScaleId == item.ClothesScaleId && x.OrderId == lastOrder.OrderId);
                                if (pack != null)
                                {
                                    item.InvQty = item.InvQty.HasValue ? (item.InvQty.Value > 0 ? item.InvQty.Value : 1) : 1;
                                    if (pack.InvQty >= item.InvQty && pack.IsActive == true && pack.IsDelete == false)
                                    {
                                        if (ordPack == null)
                                        {
                                            ordPack = new OrderScale();
                                            ordPack.OrderScaleId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                            ordPack.OrderId = lastOrder.OrderId;
                                            ordPack.Quantity = item.InvQty;
                                            ordPack.ClothesScaleId = pack.ClothesScaleId;
                                            ordPack.ClothesId = pack.ClothesId;
                                            ordPack.PackedQty = 0;
                                            ordPack.IsConfirmed = false;
                                            ordPack.DateCreated = ordPack.DateUpdated = DateTime.UtcNow;
                                            db.OrderScales.Add(ordPack);
                                        }
                                        else
                                        {
                                            ordPack.Quantity = item.InvQty;
                                            ordPack.DateUpdated = DateTime.UtcNow;
                                        }
                                        db.SaveChanges();
                                    }
                                    else
                                        Errors.Add(string.Format("You cannot order quantity {0} for pack {1}", item.InvQty, pack.Name));
                                }
                                else
                                    Errors.Add("Pack was not found");
                            }
                        }
                    }
                }
                if (Items.AvailableOpenSizes != null)
                {
                    foreach (var item in Items.AvailableOpenSizes)
                    {
                        if (item != null)
                        {
                            if (item.ClothesScaleSizeClass != null)
                            {
                                foreach (var size in item.ClothesScaleSizeClass)
                                {
                                    if (size != null)
                                    {
                                        if (size.ClothesScaleSizeId > 0 && (size.Quantity.HasValue ? size.Quantity > 0 : false))
                                        {
                                            var cSize = db.ClothesScaleSizes.Find(size.ClothesScaleSizeId);
                                            if (cSize != null)
                                            {
                                                if (cSize.Quantity >= size.Quantity && cSize.IsActive == true && cSize.IsDelete == false)
                                                {
                                                    var oSize = db.OrderSizes.FirstOrDefault(x => x.OrderId == lastOrder.OrderId && x.ClothesSizeId == cSize.ClothesScaleSizeId);
                                                    if (oSize == null)
                                                    {
                                                        oSize = new OrderSize();
                                                        oSize.OrderSizeId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                                        oSize.OrderId = lastOrder.OrderId;
                                                        oSize.ClothesSizeId = cSize.ClothesScaleSizeId;
                                                        oSize.Quantity = size.Quantity;
                                                        oSize.ClothesId = cSize.ClothesScale.ClothesId;
                                                        oSize.PackedQty = 0;
                                                        oSize.IsConfirmed = false;
                                                        oSize.DateCreated = oSize.DateUpdated = DateTime.UtcNow;
                                                        db.OrderSizes.Add(oSize);
                                                    }
                                                    else
                                                    {
                                                        oSize.Quantity = size.Quantity;
                                                        oSize.DateUpdated = DateTime.UtcNow;
                                                    }
                                                    db.SaveChanges();
                                                }
                                                else
                                                    Errors.Add(string.Format("You cannot order quantity {0} for size {1}", size.Quantity, cSize.Size.Name));
                                            }
                                            else
                                                Errors.Add("Size was not found");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            lastOrder.DateUpdated = DateTime.UtcNow;
            lastOrder.OriginalQty = 0;
            if (db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                lastOrder.OriginalQty = db.OrderScales.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0) * (x.Quantity.HasValue ? x.Quantity.Value : 0));
            if (db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).ToList().Any())
                lastOrder.OriginalQty += db.OrderSizes.Where(x => x.OrderId == lastOrder.OrderId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
            db.SaveChanges();
            Session["Order"] = lastOrder.OrderId;
            return Json("success", JsonRequestBehavior.AllowGet);
        }


        public ActionResult SeeAll(string Id)
        {
            var menu = new Menu();
            ViewBag.PageMessage = TempData["PageMessage"];
            if (Id != null)
            {
                Id = Id.Trim().ToLower();
                int id = 0;
                if (TempData["Search"] != null)
                {
                    TempData["Force"] = 1;
                    TempData["Search"] = null;
                    menu.Value = Id;
                }
                else
                {
                    bool loadRetailer = true;
                    if (!string.IsNullOrEmpty(SiteIdentity.UserId))
                    {
                        if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                        {
                            int Type = 0;
                            string type = SiteIdentity.Type;
                            int.TryParse(type, out Type);
                            loadRetailer = Type == (int)Platini.Models.CustomerType.Retail;
                        }
                        else
                            loadRetailer = false;
                    }
                    if (int.TryParse(Id, out id) && id > 0)
                    {
                        var category = db.Categories.FirstOrDefault(i => i.CategoryId == id && i.IsActive == true && i.IsDelete == false);
                        if (category != null)
                        {
                            menu.Id = id;
                            menu.Value = category.Name;
                        }
                        return View(menu);
                    }
                    else
                    {
                        if ((Id == "f" || Id == "d"))
                            if (!loadRetailer)
                                menu.Value = Id;
                            else
                                return RedirectToAction("Index");
                        else
                            menu.Value = Id;
                    }
                }
                return View(menu);
            }

            return RedirectToAction("Index");
        }

        public ActionResult GetClothesByIdOrMode(string Id, string level, string isEdit)
        {
            if (Id != null)
            {
                Id = Id.Trim().ToLower();
                int id = 0;
                bool loadMSRP = true;
                bool isCustomer = true;
                bool isAdmin = false;
                ViewBag.C = level;
                bool forceSearch = TempData["Force"] != null;
                if (forceSearch)
                    TempData["Force"] = null;
                if (!string.IsNullOrEmpty(SiteIdentity.UserId))
                {
                    if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.User.ToString())
                    {
                        int Type = 0;
                        string type = SiteIdentity.Type;
                        int.TryParse(type, out Type);
                        loadMSRP = Type == (int)Platini.Models.CustomerType.Retail;
                    }
                    else
                    {
                        isCustomer = loadMSRP = false;
                    }
                    isAdmin = (SiteIdentity.Roles == RolesEnum.Admin.ToString() || SiteIdentity.Roles == RolesEnum.SuperAdmin.ToString());
                }
                var webSetting = db.WebsiteSettings.Where(x => x.SettingKey.ToLower() == "settings").FirstOrDefault();
                bool check = false;
                if (webSetting != null)
                {
                    check = !string.IsNullOrEmpty(webSetting.SettingValue) ? (webSetting.SettingValue != "0" ? true : false) : false;
                }
                ClothList retlist = new ClothList();
                var list = new List<DB.Cloth>();
                var tempDate = new DateTime(1900, 1, 1);
                if (int.TryParse(Id, out id) && id > 0 && !forceSearch)
                {
                    //var list = db.Clothes.Where(i => (i.CategoryId == id || i.Category.ParentId == id) && (i.FutureDeliveryDate == null || i.FutureDeliveryDate == DateTime.MinValue || i.FutureDeliveryDate == tempDate) && i.IsActive == true && i.IsDelete == false).OrderBy(x => x.Clearance).
                    //ThenByDescending(x => x.DateChanged).ToList(); //Live
                    //var list = db.Clothes.Where(i => (i.CategoryId == id || i.Category.ParentId == id) && (i.FutureDeliveryDate == null || i.FutureDeliveryDate == DateTime.MinValue || i.FutureDeliveryDate == tempDate) && i.IsActive == true && i.IsDelete == false).OrderBy(x => x.Clearance).
                    //   ThenByDescending(x => x.DateChanged).ThenBy(x => x.SortOrder).ToList();
                    list = db.Clothes.Where(i => (i.CategoryId == id || i.Category.ParentId == id) && (i.FutureDeliveryDate == null || i.FutureDeliveryDate == DateTime.MinValue || i.FutureDeliveryDate == tempDate) && i.IsActive == true && i.IsDelete == false && (isCustomer && loadMSRP ? i.MSRP > 0 : true)).OrderBy(x => x.SortOrder).
                        ThenByDescending(x => x.DateChanged).ThenBy(x => x.Clearance).ToList();

                    var category = db.Categories.Find(id);
                    if (category.CategoryLevel == 2)
                    {
                        var SubCategory = db.Categories.Find(category.ParentId);
                        SiteConfiguration.MainID = SubCategory.ParentId;
                        SiteConfiguration.SubID = category.ParentId;
                        SiteConfiguration.CatID = id;
                    }
                    else
                    {
                        SiteConfiguration.MainID = category.ParentId;
                        SiteConfiguration.SubID = id;
                        SiteConfiguration.CatID = 0;
                    }
                    ViewBag.Mode = "";
                    ViewBag.EditMode = isEdit;
                }
                else if (Id == "n" && !forceSearch)
                {
                    ViewBag.Mode = Id;
                    list = db.Clothes.Where(i => i.Clearance == 0 && (i.FutureDeliveryDate == null || i.FutureDeliveryDate == DateTime.MinValue || i.FutureDeliveryDate == tempDate) && i.IsActive == true && i.IsDelete == false).
                        OrderByDescending(x => x.DateChanged).Take(50).ToList();
                }
                else if (Id == "c" && !forceSearch)
                {
                    ViewBag.Mode = Id;
                    list = db.Clothes.Where(i => i.Clearance == 1 && i.IsActive == true && i.IsDelete == false).
                       OrderByDescending(x => x.DateChanged).ToList();
                }
                else if (Id == "d" && !forceSearch)
                {
                    ViewBag.Mode = Id;
                    list = db.Clothes.Where(i => i.IsActive == false && i.IsDelete == false).
                        OrderByDescending(x => x.DateChanged).ToList();
                }
                else if (Id == "f" && !forceSearch)
                {
                    ViewBag.Mode = Id;
                    list = db.Clothes.Where(i => i.IsActive == true && i.IsDelete == false && i.FutureDeliveryDate.HasValue && i.FutureDeliveryDate > tempDate).OrderBy(x => x.FutureDeliveryDate).OrderBy(x => x.Clearance).
                       OrderByDescending(x => x.DateChanged).ToList();
                }
                else
                {
                    ViewBag.Mode = "";
                    ViewBag.SearchMode = isAdmin ? "1" : "";
                    if (isCustomer)
                    {
                        if (db.Clothes.FirstOrDefault(x => x.StyleNumber.ToLower() == Id && x.IsActive == false) != null)
                        {
                            ViewBag.Message = "This product is no longer in inventory";
                            retlist.List = new List<ClothListItem>();
                            return PartialView("ClothesPartialNew", retlist);
                        }
                    }
                    var cList = Id.Split(',').Select(x => x.Trim().ToLower()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    if (cList.Count > 0 && Id.Contains(","))
                        cList.Add(Id.Replace(',', ' ').ToLower());
                    var mapList = new List<string>();
                    foreach (var constr in cList)
                    {
                        var map = db.TagMappings.FirstOrDefault(x => x.Singular == constr && !string.IsNullOrEmpty(x.Plurals));
                        if (map != null)
                            mapList.AddRange(map.Plurals.Split(',').Select(x => x.Trim().ToLower()).Where(x => !string.IsNullOrWhiteSpace(x)));
                    }
                    cList.AddRange(mapList.Distinct());
                    ViewBag.SearchCred = string.Join(",", cList);
                    if (isCustomer)
                    {
                        list = db.Clothes.Where(i => i.IsActive == true && i.IsDelete == false && (i.FutureDeliveryDate == null || i.FutureDeliveryDate == DateTime.MinValue || i.FutureDeliveryDate == tempDate) && (loadMSRP ? i.MSRP > 0 : i.Price > 0)).ToList();
                    }
                    else
                        list = db.Clothes.Where(i => i.IsActive == true && i.IsDelete == false).ToList();

                    var testList = list.SelectMany(x => db.Categories.Where(y => y.CategoryId == x.Category.ParentId), (x, y) => new { x, sub = y.Name.ToLower(), subid = y.ParentId }).ToList().
                        SelectMany(x => db.Categories.Where(y => y.CategoryId == x.subid), (x, y) => new { cloth = x.x, x.sub, cat = y.Name.ToLower() }).ToList();

                    list = testList.Where(x => (!string.IsNullOrEmpty(x.cloth.Color) ? cList.Any(y => y.Contains(x.cloth.Color.ToLower()) || x.cloth.Color.ToLower().Contains(y)) : false) ||
                        (!string.IsNullOrEmpty(x.cloth.StyleNumber) ? cList.Any(y => y.Contains(x.cloth.StyleNumber.ToLower()) || x.cloth.StyleNumber.ToLower().Contains(y)) : false)
                        || (!string.IsNullOrEmpty(x.cloth.Category.Name) ? cList.Any(y => y == x.cloth.Category.Name.ToLower()) : false)
                        || (cList.Any(y => y == x.sub)) || (cList.Any(y => y == x.cat)) ||
                        (!string.IsNullOrEmpty(x.cloth.Tags) ? x.cloth.Tags.Split(',').Distinct().SelectMany(y => cList.Where(z => !string.IsNullOrEmpty(y) && (z.Contains(y.ToLower()) || y.Contains(z.ToLower()))), (y, z) => new { y, z }).Count() > 0 : false)
                        || (!string.IsNullOrEmpty(x.cloth.ClothesDescription) ? (cList.Any(y => x.cloth.ClothesDescription.ToLower().Contains(y))) : false)
                        ).Select(x => x.cloth).ToList();

                    list = list.OrderBy(x => x.Category.Name).ThenByDescending(x => x.DateChanged).ToList();
                }
                if (check)
                {
                    List<DB.Cloth> removeClothes = new List<DB.Cloth>().InjectFrom(list);
                    foreach (var item in removeClothes)
                    {
                        int cQty = (item.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                        + (item.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                        if (cQty <= 0)
                        {
                            var itemL = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault();
                            list.Remove(itemL);
                        }
                    }
                }
                //if (loadMSRP)
                //    retlist.List = list.Select(x => new ClothListItem { ClothesId = x.ClothesId, FutureDeliveryDate = x.FutureDeliveryDate, Price = x.MSRP, DiscountedPrice = x.DiscountedMSRP, StyleNumber = x.StyleNumber, Clearance = x.Clearance.HasValue ? x.Clearance.Value : 2 }).ToList();
                //else
                //    retlist.List.InjectFrom(list);
                //foreach (var item in retlist.List)
                //{
                //    var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                //    item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
                //}
                //return PartialView("ClothesPartialNew", retlist);

                if (loadMSRP)
                {
                    retlist.List = list.Select(x => new ClothListItem { ClothesId = x.ClothesId, FutureDeliveryDate = x.FutureDeliveryDate, Price = x.MSRP, DiscountedPrice = x.DiscountedMSRP, StyleNumber = x.StyleNumber, Clearance = x.Clearance.HasValue ? x.Clearance.Value : 2 }).ToList();
                    foreach (var item in retlist.List)
                    {
                        var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                        item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
                    }
                }
                else
                {
                    foreach (var item in list)
                    {
                        var image = item.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                        retlist.List.Add(new ClothListItem
                        {
                            ClothesId = item.ClothesId,
                            StyleNumber = item.StyleNumber,
                            ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg",
                            IsActive = item.IsActive,
                            Price = item.Price,
                            DiscountedPrice = item.DiscountedPrice,
                            Clearance = item.Clearance,
                            FutureDeliveryDate = item.FutureDeliveryDate
                        });
                    }
                }
                return PartialView("ClothesPartialNew", retlist);
            }
            return PartialView("ClothesPartialNew", new ClothList());
        }

        [NonAction]
        public string GetCategoryName(int Id, int level)
        {
            var cat = db.Categories.Find(Id);
            if (level == 2)
            {
                if (cat != null)
                    return cat.Name.ToLower();
            }
            else
            {
                if (cat != null)
                {
                    var parent = db.Categories.Find(cat.ParentId);
                    if (parent != null)
                        return parent.Name;
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult SignUpLetter(string signUpEmail)
        {
            string msg = "Please enter your email";
            if (!string.IsNullOrEmpty(signUpEmail))
            {
                signUpEmail = signUpEmail.Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(signUpEmail, @"^([\w-\.]+@([\w-]+\.)+[\w-]{2,4})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    msg = "The email address entered is not in a valid format";
                else
                {
                    var sign = db.NewsLetterSignUps.FirstOrDefault(x => x.Email == signUpEmail);
                    if (sign == null)
                    {
                        sign = new NewsLetterSignUp();
                        sign.Email = signUpEmail;
                        sign.IsActive = true;
                        sign.IsDelete = false;
                        sign.DateCreated = sign.DateUpdated = DateTime.UtcNow;
                        db.NewsLetterSignUps.Add(sign);
                        db.SaveChanges();
                        msg = "";
                    }
                }
            }
            if (!string.IsNullOrEmpty(msg))
                TempData["PageMessage"] = msg;
            if (Request.UrlReferrer != null)
            {
                if (!string.IsNullOrEmpty(Request.UrlReferrer.AbsolutePath))
                    return Redirect(Request.UrlReferrer.AbsolutePath);
            }
            return RedirectToAction("SeeAll", new { @Id = 6 });
        }

        [HttpPost]
        public ActionResult SaveNote(string newNote, int cloth)
        {
            var dbCloth = db.Clothes.Find(cloth);
            if (dbCloth != null)
            {
                dbCloth.AdminNote = newNote;
                dbCloth.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
                return RedirectToAction("Detail", new { Id = dbCloth.StyleNumber });
            }
            return RedirectToAction("Index");
        }

        public ActionResult Dashboard()
        {
            var retModel = new DashBoard();
            retModel.showDashboard = !string.IsNullOrEmpty(SiteIdentity.UserId) && (SiteIdentity.Roles.ToLower() == RolesEnum.SuperAdmin.ToString().ToLower() || SiteIdentity.Roles.ToLower() == RolesEnum.Admin.ToString().ToLower());
            if (retModel.showDashboard)
            {
                retModel.uCount = SiteConfiguration.GetApp(HttpContext.ApplicationInstance.Application).Count;
                retModel.iCount = db.Accounts.Where(x => x.IsActive == false && x.IsDelete == false).ToList().Where(x => x.CustomerOptionalInfoes.Count > 0).Select(x => x.CustomerOptionalInfoes.FirstOrDefault()).Count(x => x.CustomerType == (int)Platini.Models.CustomerType.Wholesale);
                retModel.showDollar = SiteIdentity.Roles.ToLower() == RolesEnum.SuperAdmin.ToString().ToLower();
                DateTime toDate = DateTime.UtcNow.AddHours(-24);
                DateTime fromDate = DateTime.UtcNow;
                retModel.vCount = db.VisitorLogs.Where(x => (x.DateUpdated.HasValue ? (x.DateUpdated.Value <= fromDate && x.DateUpdated.Value >= toDate) : false) && !x.AccountId.HasValue).Count();
                if (retModel.showDollar)
                {
                    retModel.TotAmt = db.Orders.Where(x => x.ShippedOn.HasValue ? (x.ShippedOn.Value <= fromDate && x.ShippedOn >= toDate) : false).
                        ToList().Sum(x => (x.FinalAmount.HasValue ? x.FinalAmount.Value : 0));
                    retModel.OrdCount = db.Orders.Where(x => x.ShippedOn.HasValue ? (x.ShippedOn.Value <= fromDate && x.ShippedOn >= toDate) : false).Count();
                }
            }
            return PartialView(retModel);
        }

        [HttpPost]
        public ActionResult UpdateClearance(int clr, int cId)
        {
            var cloth = db.Clothes.Find(cId);
            if ((clr > -1 && clr < 2) && cloth != null)
            {
                cloth.Clearance = clr;
                if (clr == 1)
                {
                    cloth.SortOrder = int.MaxValue;
                }
                cloth.DateUpdated = DateTime.UtcNow;
                db.SaveChanges();
            }
            return Json("");
        }

        [HttpPost]
        public ActionResult UpdateClearances(List<ClearanceModel> values)
        {
            foreach (var item in values)
            {
                var cloth = db.Clothes.Find(item.Id);
                if ((item.Value > -1 && item.Value < 2) && cloth != null)
                {
                    cloth.Clearance = item.Value;
                    if (item.Value == 1)
                    {
                        cloth.SortOrder = int.MaxValue;
                    }
                    cloth.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                }
            }
            return Json("");
        }

        public ActionResult LoginPartial(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            HttpCookie FindCookie = Request.Cookies["Platini"];
            if (FindCookie != null)
            {
                if (FindCookie.Values["cUserName"] != null && FindCookie.Values["cPassword"] != null)
                {
                    LoginModel model = new LoginModel();
                    model.UserName = FindCookie.Values["cUserName"].ToString();
                    model.Password = Cryptography.Decrypt(FindCookie.Values["cPassword"].ToString());
                    model.RememberMe = true;
                    model.isPartial = true;
                    return PartialView(model);
                }
            }
            return PartialView(new LoginModel() { isPartial = true });
        }
        [HttpPost]
        public ActionResult SaveClothesSort(List<SortOrders> arr)
        {
            if (arr != null)
            {
                var values = arr.Select(x => int.Parse(x.id));
                var clothes = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && values.Contains(x.ClothesId)).ToList();
                clothes.ForEach(x => { x.SortOrder = arr.FirstOrDefault(y => y.id == x.ClothesId.ToString()).so; x.DateUpdated = DateTime.UtcNow; });
                db.SaveChanges();
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Billing()
        {
            int UserId = 0;
            int.TryParse(SiteIdentity.UserId, out UserId);
            var account = db.Accounts.Find(UserId);
            if (account != null)
            {
                if (TempData["CheckLogin"] != null && account.LastLoginDate < DateTime.UtcNow.AddDays(-1))
                {
                    TempData["CheckLogin"] = null;
                    FormsAuthentication.SignOut();
                    SiteConfiguration.Clear();
                    SiteIdentity.UserId = "";
                    TempData["PageMessage"] = "Please re-verify your credentials.";
                    return RedirectToAction("Login");
                }

                DB.Order lastOrder = null;
                if (Session["Order"] != null)
                    lastOrder = db.Orders.Find((Guid)Session["Order"]);
                if (lastOrder != null)
                {

                    var sum = CalcSum(lastOrder.OrderId);
                    if (sum != 0)
                    {
                        var model = new BillInfo();
                        model.OrderId = lastOrder.OrderId;
                        model.UserId = lastOrder.AccountId;

                        model.ST = string.Format("${0:0.00}", (lastOrder.GrandTotal - (lastOrder.GrandTotal * (lastOrder.Discount.HasValue ? lastOrder.Discount.Value / 100 : 0))));
                        if (lastOrder.ShippingCost > 0)
                            model.GS = string.Format("${0:0.00}", (lastOrder.ShippingCost.Value));
                        else
                            model.GS = "Free";
                        model.GT = string.Format("${0:0.00}", lastOrder.FinalAmount);

                        model.QT = (lastOrder.OriginalQty ?? 0).ToString();

                        var cAddress = account.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);
                        model.BillingAddress.AddressType = (int)AddressTypeEnum.BillingAddress;
                        if (cAddress != null)
                        {
                            model.BillingAddress.Address1 = cAddress.Street;
                            model.BillingAddress.City = cAddress.City;
                            model.BillingAddress.State = cAddress.State;
                            model.BillingAddress.Country = cAddress.Country;
                            model.BillingAddress.Zip = cAddress.Pincode;
                            model.BillingAddress.AddressId = cAddress.AddressId;
                            model.BillingAddress.AddressType = cAddress.AddressTypeId;
                        }
                        model.ShippingAddress.AddressType = (int)AddressTypeEnum.ShippingAddress;
                        if (lastOrder.AddressId.HasValue)
                        {
                            model.ShippingAddress.Address1 = lastOrder.Address.Street;
                            model.ShippingAddress.City = lastOrder.Address.City;
                            model.ShippingAddress.State = lastOrder.Address.State;
                            model.ShippingAddress.Country = lastOrder.Address.Country;
                            model.ShippingAddress.Zip = lastOrder.Address.Pincode;
                            model.ShippingAddress.AddressId = lastOrder.Address.AddressId;
                            model.ShippingAddress.AddressType = lastOrder.Address.AddressTypeId;
                        }
                        else
                        {
                            cAddress = account.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
                            if (cAddress != null)
                            {
                                model.ShippingAddress.Address1 = cAddress.Street;
                                model.ShippingAddress.City = cAddress.City;
                                model.ShippingAddress.State = cAddress.State;
                                model.ShippingAddress.Country = cAddress.Country;
                                model.ShippingAddress.Zip = cAddress.Pincode;
                                model.ShippingAddress.AddressId = cAddress.AddressId;
                                model.ShippingAddress.AddressType = cAddress.AddressTypeId;
                            }
                        }
                        model.Cards = new List<SelectedStringValues>() { 
                        new SelectedStringValues(){Id="visa",Value="VISA"},
                        new SelectedStringValues(){Id="mastercard",Value="MasterCard"},
                        new SelectedStringValues(){Id="amex",Value="American Express"},
                        new SelectedStringValues(){Id="discover",Value="Discover"},
                    };
                        model.SavedCards = account.Transactions.Where(x => x.TransactionType == "paypal" && x.Status == (int)TransactionStatus.Completed && x.IsPayPalActive == true).
                            OrderByDescending(x => x.DateUpdated).Select(x => new SelectedListValues()
                            {
                                Id = x.TransactionId,
                                Value = x.CardNumber
                            }).ToList();

                        int i = 0;
                        foreach (var item in System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames)
                        {
                            if (!string.IsNullOrEmpty(item))
                                model.Months.Add(new SelectedListValues() { Id = ++i, Value = item });
                        }
                        for (i = 0; i < 20; i++)
                        {
                            model.Years.Add(new SelectedListValues() { Id = DateTime.Now.Year + i, Value = (DateTime.Now.Year + i).ToString() });
                        }
                        ViewBag.PageMessage = TempData["PageMessage"];
                        return View("Billing", model);
                    }
                    TempData["PageMessage"] = "Add something in your cart first";
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Billing(BillInfo ret)
        {
            string cnumber = string.Empty;
            string paypalCardId = string.Empty;
            bool isToken = ret.SavedCard > 0;
            if (isToken)
            {
                ModelState.Remove("CardFName");
                ModelState.Remove("CardLName");
                ModelState.Remove("CardNumber");
                ModelState.Remove("CVV");
                ModelState.Remove("CardType");
                ModelState.Remove("ExpMonth");
                ModelState.Remove("ExpYear");
            }
            else if (!string.IsNullOrEmpty(ret.CardNumber))
            {
                ModelState.Remove("SavedCard");
                cnumber = SiteConfiguration.ModTest(ret.CardNumber);
                if (ret.CardType == "amex" && ret.CVV.Length < 4)
                    ModelState.AddModelError("CVV", "Length must be 4");
            }
            if (ModelState.IsValid && (!string.IsNullOrEmpty(cnumber) || isToken))
            {
                var bAddress = SaveThisAddress(ret.BillingAddress, (int)AddressTypeEnum.BillingAddress, ret.UserId);

                var sAddress = SaveThisAddress(ret.ShippingAddress, (int)AddressTypeEnum.ShippingAddress, ret.UserId);

                var order = db.Orders.Find(ret.OrderId);
                if (order != null)
                {
                    order.AddressId = sAddress.AddressId;
                    order.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                }

                var transaction = db.Transactions.FirstOrDefault(x => x.OrderId == ret.OrderId && x.AccountId == ret.UserId && x.TransactionType == "paypal" && x.Status == (int)TransactionStatus.Pending);
                if (transaction == null)
                {
                    transaction = new DB.Transaction();
                    transaction.AccountId = ret.UserId;
                    transaction.OrderId = ret.OrderId;
                    transaction.TransactionType = TransactionType.Paypal.ToString().ToLower();
                    transaction.DateCreated = DateTime.UtcNow;
                }
                if (!isToken)
                {
                    string cvv = ret.CVV;
                    if (ret.CVV.Length > 2)
                        cvv = cvv.Substring(0, (ret.CardType == "amex" ? 4 : 3)).Substring(cvv.Length - 1).PadLeft(cvv.Length, 'X');
                    transaction.CardNumber = cnumber;
                    transaction.CVV = cvv;
                    transaction.CardType = ret.CardType.ToLower();
                }
                else
                {
                    var oldTransaction = db.Transactions.Find(ret.SavedCard);
                    if (oldTransaction != null)
                    {
                        transaction.CardNumber = oldTransaction.CardNumber;
                        transaction.CVV = oldTransaction.CVV;
                        transaction.CardType = oldTransaction.CardType;
                        paypalCardId = Cryptography.Decrypt(oldTransaction.PayPalId);
                    }
                }
                transaction.Status = (int)TransactionStatus.Pending;
                transaction.IsActive = true;
                transaction.IsDelete = false;
                transaction.DateUpdated = DateTime.UtcNow;
                if (transaction.TransactionId == 0)
                    db.Transactions.Add(transaction);
                db.SaveChanges();

                var account = db.Accounts.Find(ret.UserId);
                try
                {
                    if (!isToken)
                    {
                        CreditCard cc;
                        var txnResult = DoDirectCardPayment(ret.CardFName, ret.CardLName, ret.CardNumber, ret.CVV, ret.CardType, ret.ExpMonth, ret.ExpYear, ret.OrderId, ret.BillingAddress, ret.ShippingAddress, account.Email, order.FinalAmount.Value, out cc);
                        if (txnResult != null && !string.IsNullOrEmpty(txnResult.state) && txnResult.state == "approved" && cc != null)
                        {
                            var sct = CreditCard.Create(new PaypalOps().Api, cc);
                            Session["Txn"] = new TransactionResult() { Success = true, TransactionId = txnResult.transactions[0].related_resources[0].sale.id, OrderId = order.OrderId };
                            transaction.TransactionNumber = txnResult.transactions[0].related_resources[0].sale.id;
                            transaction.Status = (int)TransactionStatus.Completed;
                            transaction.DateUpdated = DateTime.UtcNow;
                            transaction.PayPalId = Cryptography.Encrypt(sct.id);
                            transaction.IsPayPalActive = true;
                            db.SaveChanges();
                            return RedirectToAction("Success");
                        }
                    }
                    else
                    {
                        var txnResult = DoSavedCardPayment(paypalCardId, order.OrderId, ret.ShippingAddress, account.Email, order.FinalAmount.Value);
                        if (txnResult != null && !string.IsNullOrEmpty(txnResult.state) && txnResult.state == "approved")
                        {

                            Session["Txn"] = new TransactionResult() { Success = true, TransactionId = txnResult.transactions[0].related_resources[0].sale.id, OrderId = order.OrderId };
                            transaction.TransactionNumber = txnResult.transactions[0].related_resources[0].sale.id;
                            transaction.Status = (int)TransactionStatus.Completed;
                            transaction.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();
                            return RedirectToAction("Success");
                        }
                    }
                }
                catch (PayPal.PaymentsException ex)
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<PaypalResponse>(ex.Response);
                        foreach (var reason in resp.details)
                        {
                            ViewBag.PageMessage += reason.field + " - " + reason.issue + "\n";
                        }
                    }
                    catch
                    {
                        ViewBag.PageMessage = "Unknown error occured while parsing";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.PageMessage = ex.Message;
                }
            }
            if (string.IsNullOrEmpty(cnumber) && !isToken)
                ViewBag.PageMessage = "Incorrect Credit Card Number format.";

            var lastOrder = db.Orders.Find(ret.OrderId);
            if (lastOrder != null)
            {
                ret.ST = string.Format("${0:0.00}", (lastOrder.GrandTotal - (lastOrder.GrandTotal * (lastOrder.Discount.HasValue ? lastOrder.Discount.Value / 100 : 0))));
                if (lastOrder.ShippingCost > 0)
                    ret.GS = string.Format("${0:0.00}", lastOrder.ShippingCost.Value);
                else
                    ret.GS = "Free";
                ret.GT = string.Format("${0:0.00}", lastOrder.FinalAmount);
                ret.QT = (lastOrder.OriginalQty ?? 0).ToString();
            }

            var xaccount = db.Accounts.Find(ret.UserId);
            if (xaccount != null)
            {
                ret.SavedCards = xaccount.Transactions.Where(x => x.TransactionType == "paypal" && x.Status == (int)TransactionStatus.Completed && x.IsPayPalActive == true).
                       OrderByDescending(x => x.DateUpdated).Select(x => new SelectedListValues()
                       {
                           Id = x.TransactionId,
                           Value = x.CardNumber
                       }).ToList();
            }
            else
                ret.SavedCard = 0;
            ret.Cards = new List<SelectedStringValues>() { 
                        new SelectedStringValues(){Id="visa",Value="VISA"},
                        new SelectedStringValues(){Id="mastercard",Value="MasterCard"},
                        new SelectedStringValues(){Id="amex",Value="American Express"},
                        new SelectedStringValues(){Id="discover",Value="Discover"},
                    };
            int i = 0;
            foreach (var item in System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames)
            {
                if (!string.IsNullOrEmpty(item))
                    ret.Months.Add(new SelectedListValues() { Id = ++i, Value = item });
            }
            for (i = 0; i < 20; i++)
            {
                ret.Years.Add(new SelectedListValues() { Id = DateTime.Now.Year + i, Value = (DateTime.Now.Year + i).ToString() });
            }
            return View("Billing", ret);
        }

        [HttpPost]
        public ActionResult Express(BillInfo ret)
        {
            ModelState.Remove("CardFName");
            ModelState.Remove("CardLName");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CVV");
            ModelState.Remove("CardType");
            ModelState.Remove("ExpMonth");
            ModelState.Remove("ExpYear");
            ModelState.Remove("SavedCard");
            if (ModelState.IsValid)
            {
                var bAddress = SaveThisAddress(ret.BillingAddress, (int)AddressTypeEnum.BillingAddress, ret.UserId);

                var sAddress = SaveThisAddress(ret.ShippingAddress, (int)AddressTypeEnum.ShippingAddress, ret.UserId);

                var order = db.Orders.Find(ret.OrderId);
                if (order != null)
                {
                    order.AddressId = sAddress.AddressId;
                    order.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                }

                var transaction = db.Transactions.FirstOrDefault(x => x.OrderId == ret.OrderId && x.AccountId == ret.UserId && x.TransactionType == "expresscheckout" && x.Status == (int)TransactionStatus.Pending);
                if (transaction == null)
                {
                    transaction = new DB.Transaction();
                    transaction.AccountId = ret.UserId;
                    transaction.OrderId = ret.OrderId;
                    transaction.TransactionType = TransactionType.ExpressCheckout.ToString().ToLower();
                    transaction.DateCreated = DateTime.UtcNow;
                }
                transaction.Status = (int)1;
                transaction.IsActive = true;
                transaction.IsDelete = false;
                transaction.DateUpdated = DateTime.UtcNow;
                if (transaction.TransactionId == 0)
                    db.Transactions.Add(transaction);
                db.SaveChanges();

                try
                {
                    if (Session["PaypalToken"] == null)
                    {
                        var txnResult = CreatePaypalPayment(order.OrderId, order.FinalAmount.Value);  // SetExpressCheckoutAPIOperation(order.FinalAmount.Value, ret.ShippingAddress, order.OrderId);
                        if (txnResult != null && !string.IsNullOrEmpty(txnResult.state) && txnResult.state == "created" && txnResult.links.Count > 0)
                        {
                            string rdr = txnResult.links.FirstOrDefault(x => x.rel.ToLower().Equals("approval_url")) != null ? txnResult.links.FirstOrDefault(x => x.rel.ToLower().Equals("approval_url")).href : "";
                            if (!string.IsNullOrEmpty(rdr))
                            {
                                transaction.PayPalId = txnResult.id;
                                transaction.DateUpdated = DateTime.UtcNow;
                                db.SaveChanges();
                                Session["PaypalToken"] = rdr;
                                return Redirect(rdr);
                            }
                            else
                                ViewBag.PageMessage = "Couldn't create payment. Please try again.";
                        }
                    }
                    else
                        return Redirect(Session["PaypalToken"].ToString());
                }
                catch (PayPal.PaymentsException ex)
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<PaypalResponse>(ex.Response);
                        foreach (var reason in resp.details)
                        {
                            ViewBag.PageMessage += reason.field + " - " + reason.issue + "\n";
                        }
                    }
                    catch
                    {
                        ViewBag.PageMessage = "Unknown error occured while parsing";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.PageMessage = ex.Message;
                }
            }
            var lastOrder = db.Orders.Find(ret.OrderId);
            if (lastOrder != null)
            {
                ret.ST = string.Format("${0:0.00}", (lastOrder.GrandTotal - (lastOrder.GrandTotal * (lastOrder.Discount.HasValue ? lastOrder.Discount.Value / 100 : 0))));
                if (lastOrder.ShippingCost > 0)
                    ret.GS = string.Format("${0:0.00}", (lastOrder.ShippingCost.HasValue ? lastOrder.ShippingCost.Value : 0));
                else
                    ret.GS = "Free";
                ret.GT = string.Format("${0:0.00}", lastOrder.FinalAmount);
                ret.QT = (lastOrder.OriginalQty ?? 0).ToString();
            }
            ret.Cards = new List<SelectedStringValues>() { 
                        new SelectedStringValues(){Id="visa",Value="VISA"},
                        new SelectedStringValues(){Id="mastercard",Value="MasterCard"},
                        new SelectedStringValues(){Id="amex",Value="American Express"},
                        new SelectedStringValues(){Id="discover",Value="Discover"},
                    };
            int i = 0;
            foreach (var item in System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames)
            {
                if (!string.IsNullOrEmpty(item))
                    ret.Months.Add(new SelectedListValues() { Id = ++i, Value = item });
            }
            for (i = 0; i < 20; i++)
            {
                ret.Years.Add(new SelectedListValues() { Id = DateTime.Now.Year + i, Value = (DateTime.Now.Year + i).ToString() });
            }
            return View("Billing", ret);
        }

        [HttpPost]
        public ActionResult SaveAddress(BillInfo ret)
        {
            ModelState.Remove("CardFName");
            ModelState.Remove("CardLName");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CVV");
            ModelState.Remove("CardType");
            ModelState.Remove("ExpMonth");
            ModelState.Remove("ExpYear");
            ModelState.Remove("SavedCard");
            if (ModelState.IsValid)
            {
                var bAddress = SaveThisAddress(ret.BillingAddress, (int)AddressTypeEnum.BillingAddress, ret.UserId);

                var sAddress = SaveThisAddress(ret.ShippingAddress, (int)AddressTypeEnum.ShippingAddress, ret.UserId);

                var order = db.Orders.Find(ret.OrderId);
                if (order != null)
                {
                    order.AddressId = sAddress.AddressId;
                    order.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                }

                if (SiteConfiguration.SubID > 0)
                    return RedirectToAction("SeeAll", "Home", new { Id = SiteConfiguration.SubID });
                else
                    return RedirectToAction("Index");
            }
            var lastOrder = db.Orders.Find(ret.OrderId);
            if (lastOrder != null)
            {
                ret.ST = string.Format("${0:0.00}", (lastOrder.GrandTotal - (lastOrder.GrandTotal * (lastOrder.Discount.HasValue ? lastOrder.Discount.Value / 100 : 0))));
                if (lastOrder.ShippingCost > 0)
                    ret.GS = string.Format("${0:0.00}", lastOrder.ShippingCost.Value);
                else
                    ret.GS = "Free";
                ret.GT = string.Format("${0:0.00}", lastOrder.FinalAmount);
                ret.QT = (lastOrder.OriginalQty ?? 0).ToString();
            }
            ret.Cards = new List<SelectedStringValues>() { 
                        new SelectedStringValues(){Id="visa",Value="VISA"},
                        new SelectedStringValues(){Id="mastercard",Value="MasterCard"},
                        new SelectedStringValues(){Id="amex",Value="American Express"},
                        new SelectedStringValues(){Id="discover",Value="Discover"},
                    };
            int i = 0;
            foreach (var item in System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames)
            {
                if (!string.IsNullOrEmpty(item))
                    ret.Months.Add(new SelectedListValues() { Id = ++i, Value = item });
            }
            for (i = 0; i < 20; i++)
            {
                ret.Years.Add(new SelectedListValues() { Id = DateTime.Now.Year + i, Value = (DateTime.Now.Year + i).ToString() });
            }
            return View("Billing", ret);
        }

        [NonAction]
        public DB.Address SaveThisAddress(BillInfoAddress address, int aType, int UserId)
        {
            var sAddress = db.Addresses.Find(address.AddressId);
            if (sAddress == null)
            {
                sAddress = new DB.Address();
                sAddress.AccountId = UserId;
                sAddress.DateCreated = DateTime.UtcNow;
                sAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
            }
            sAddress.Street = address.Address1 + (!string.IsNullOrEmpty(address.Address2) ? ", " + address.Address2 : "");
            sAddress.State = address.State;
            sAddress.City = address.City;
            sAddress.Country = address.Country;
            sAddress.Pincode = address.Zip;
            sAddress.IsActive = true;
            sAddress.IsDelete = false;
            sAddress.DateUpdated = DateTime.UtcNow;
            if (sAddress.AddressId == 0)
                db.Addresses.Add(sAddress);
            db.SaveChanges();
            return sAddress;
        }


        public ActionResult Success()
        {
            if (Session["Txn"] != null && !string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                var result = (TransactionResult)Session["Txn"];
                var lastOrder = db.Orders.Find(result.OrderId);
                if (!result.Success)
                {
                    TempData["PageMessage"] = "Please try again";
                    return RedirectToAction("Billing");
                }
                ViewBag.Message = "Transaction successful - TransactionId: " + result.TransactionId;
                if (lastOrder != null)
                {
                    ViewBag.Transaction = "1";
                    foreach (var ordSize in lastOrder.OrderSizes)
                    {
                        var clothSize = ordSize.ClothesScaleSize;
                        if (clothSize.Quantity < ordSize.Quantity)
                        {
                            bool checkpFit = clothSize.ClothesScale.FitId.HasValue;
                            bool checkpInseam = clothSize.ClothesScale.InseamId.HasValue;
                            var scaleList = db.ClothesScales.Where(x => (checkpFit ? x.FitId == clothSize.ClothesScale.FitId : true) && (checkpInseam ? x.InseamId == clothSize.ClothesScale.InseamId : true) && x.IsOpenSize == false && x.InvQty > 0 && x.ClothesId == clothSize.ClothesScale.ClothesId).ToList();
                            if (scaleList.Any())
                            {
                                int cQty = clothSize.Quantity.HasValue ? clothSize.Quantity.Value : 0;
                                int nQty = 0;
                                var dict = new Dictionary<int, int>();
                                foreach (var bScale in scaleList)
                                {
                                    var bSize = bScale.ClothesScaleSizes.FirstOrDefault(x => x.SizeId == clothSize.SizeId && (x.Quantity.HasValue ? x.Quantity.Value > 0 : false));
                                    if (bSize != null)
                                    {
                                        dict.Add(bScale.ClothesScaleId, 0);
                                        int bInv = bScale.InvQty.HasValue ? bScale.InvQty.Value : 0;
                                        int bsQty = bSize.Quantity.Value;
                                        bool flag = true;
                                        for (int i = 1; i <= bScale.InvQty.Value; ++i)
                                        {
                                            if (cQty + (bsQty * i) >= ordSize.Quantity)
                                            {
                                                dict[bScale.ClothesScaleId] = i;
                                                flag = false;
                                                break;
                                            }
                                        }
                                        if (!flag)
                                            break;
                                        dict[bScale.ClothesScaleId] = bInv;
                                        cQty += bsQty * bInv;
                                    }
                                }
                                if (dict.Keys.Any())
                                {

                                    foreach (var Key in dict.Keys)
                                    {
                                        var dPack = db.ClothesScales.Find(Key);
                                        if (dPack != null)
                                        {
                                            if (dPack.IsActive == true && dPack.IsDelete == false && dPack.IsOpenSize == false)
                                            {
                                                bool checkFit = dPack.FitId.HasValue;
                                                bool checkInseam = dPack.InseamId.HasValue;
                                                var open = db.ClothesScales.Where(x => x.ClothesId == dPack.ClothesId && (checkInseam ? x.InseamId == dPack.InseamId : true) && (checkFit ? x.FitId == dPack.FitId : true) && x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                                                if (open == null)
                                                {
                                                    open = new ClothesScale();
                                                    open.ClothesId = dPack.ClothesId;
                                                    open.FitId = dPack.FitId;
                                                    open.InseamId = dPack.InseamId;
                                                    open.InvQty = 0;
                                                    open.IsOpenSize = true;
                                                    open.Name = null;
                                                    open.IsActive = true;
                                                    open.IsDelete = false;
                                                    open.DateCreated = open.DateUpdated = DateTime.UtcNow;
                                                    db.ClothesScales.Add(open);
                                                    db.SaveChanges();
                                                }
                                                foreach (var dsize in dPack.ClothesScaleSizes)
                                                {
                                                    var openSize = db.ClothesScaleSizes.Where(x => x.ClothesScaleId == open.ClothesScaleId && x.SizeId == dsize.SizeId && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                                                    if (openSize == null)
                                                    {
                                                        openSize = new ClothesScaleSize();
                                                        openSize.ClothesScaleId = open.ClothesScaleId;
                                                        openSize.SizeId = dsize.SizeId;
                                                        openSize.Quantity = dsize.Quantity * dict[Key];
                                                        openSize.IsActive = true;
                                                        openSize.IsDelete = false;
                                                        openSize.DateCreated = openSize.DateUpdated = DateTime.UtcNow;
                                                        db.ClothesScaleSizes.Add(openSize);
                                                    }
                                                    else
                                                    {
                                                        openSize.Quantity = openSize.Quantity + (dsize.Quantity * dict[Key]);
                                                        openSize.DateUpdated = DateTime.UtcNow;
                                                    }
                                                    if (openSize.SizeId == clothSize.SizeId)
                                                        nQty += openSize.Quantity.Value;
                                                    db.SaveChanges();
                                                }
                                                dPack.InvQty -= dict[Key];
                                                dPack.DateUpdated = DateTime.UtcNow;
                                                db.SaveChanges();
                                            }
                                        }
                                    }
                                    if (nQty > 0)
                                        clothSize.Quantity = nQty - ordSize.Quantity;
                                }
                            }
                        }
                        else
                            clothSize.Quantity = clothSize.Quantity - ordSize.Quantity;
                        clothSize.DateUpdated = DateTime.UtcNow;
                    }
                    string name = "";
                    string salespersonemail = "";
                    lastOrder.SubmittedOn = DateTime.UtcNow;
                    lastOrder.StatusId = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "new").OrderStatusId;
                    lastOrder.IsSentToQuickBook = false;
                    lastOrder.OrderNumber = SiteConfiguration.OrderNumber();
                    lastOrder.GrandTotal = CalcSum(lastOrder.OrderId);
                    lastOrder.FinalAmount = lastOrder.GrandTotal - (lastOrder.GrandTotal * ((lastOrder.Discount.HasValue ? lastOrder.Discount.Value : 0) / 100));
                    lastOrder.FinalAmount += lastOrder.ShippingCost;
                    var customer = db.Accounts.Find(lastOrder.AccountId);
                    if (customer != null)
                    {
                        if (customer.Companies.Any(x => x.IsActive == true && x.IsDelete == false))
                            name = customer.Companies.FirstOrDefault(x => x.IsActive == true && x.IsDelete == false).Name;
                        else
                            name = customer.FirstName + " " + customer.LastName;
                        if (customer.CustomerSalesPersons.Any())
                        {
                            var sales = db.Accounts.Find(customer.CustomerSalesPersons.FirstOrDefault().SalesPersonId);
                            if (sales != null)
                                salespersonemail = sales.Email;
                        }
                    }
                    lastOrder.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                    EmailManager.SendOrderEmail(lastOrder.OrderId, lastOrder.OrderNumber, lastOrder.AccountId.ToString(), name, salespersonemail);
                    EmailManager.SendOrderEmailToCustomer(lastOrder.OrderId, lastOrder.OrderNumber, (customer != null ? customer.Email : ""), lastOrder.AccountId.ToString());
                    //ViewBag.PageMessage = "The Order was submitted successfully.";
                    Session.Remove("Order");
                    Session.Remove("EditingOrder");
                    Session.Remove("Txn");
                    Session["WasCleared"] = 1;
                    return View(lastOrder.OrderId);
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult SuccessPaypal(string paymentId, string token, string PayerID)
        {
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(PayerID) && !string.IsNullOrEmpty(paymentId))
            {
                try
                {
                    var order = db.Orders.Find((Guid)Session["Order"]);
                    var txnResult = ExecutePaypalPayment(paymentId, token, PayerID);
                    if (txnResult != null && !string.IsNullOrEmpty(txnResult.state) && txnResult.state == "approved")
                    {
                        Session.Remove("PaypalToken");

                        var transaction = order.Transactions.FirstOrDefault(x => x.TransactionType.ToLower() == "expresscheckout" && x.Status == (int)TransactionStatus.Pending);
                        if (transaction != null)
                        {
                            Session["Txn"] = new TransactionResult() { Success = true, TransactionId = txnResult.transactions[0].related_resources[0].sale.id, OrderId = order.OrderId };
                            transaction.TransactionNumber = txnResult.transactions[0].related_resources[0].sale.id;
                            transaction.Status = (int)TransactionStatus.Completed;
                            transaction.DateUpdated = DateTime.UtcNow;
                            db.SaveChanges();
                            return RedirectToAction("Success");
                        }

                    }

                }
                catch (PayPal.PaymentsException ex)
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<PaypalResponse>(ex.Response);
                        foreach (var reason in resp.details)
                        {
                            ViewBag.PageMessage += reason.issue + "\n";
                        }
                    }
                    catch
                    {
                        ViewBag.PageMessage = "Unknown error occured while parsing";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.PageMessage = ex.Message;
                }
            }
            else
                ViewBag.PageMessage = "Bad paypal response";
            return View("Index");
        }

        public ActionResult FailurePaypal(string tokenId)
        {
            string postData = "";
            foreach (var item in Request.QueryString.AllKeys)
            {
                postData += item + Request.QueryString[item] + "/n";
            }
            System.IO.File.AppendAllText(Server.MapPath("~/payfaillog.txt"), postData);
            return RedirectToAction("Billing");
        }

        [HttpPost]
        public ActionResult DeleteCard(int Id)
        {
            var transaction = db.Transactions.Find(Id);
            if (transaction != null)
            {
                try
                {
                    CreditCard.Delete(new PaypalOps().Api, Cryptography.Decrypt(transaction.PayPalId));
                    transaction.IsPayPalActive = false;
                    transaction.DateUpdated = DateTime.UtcNow;
                    db.SaveChanges();
                    return Json("success", JsonRequestBehavior.AllowGet);
                }
                catch { }
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Receipt(Guid Id)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                var ret = new Receipt();
                ret.OrderId = order.OrderId;
                ret.OrderNumber = order.OrderNumber;
                var prices = order.Account.CustomerItemPrices;
                bool loadRetail = true;
                var clothes = new List<Cloth>(order.OrderScales.Count + order.OrderSizes.Count);
                clothes.AddRange(order.OrderScales.Select(x => x.ClothesScale.Cloth));
                clothes.AddRange(order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                clothes = clothes.Distinct().ToList();

                foreach (var cloth in clothes)
                {
                    int Qty = order.OrderSizes.Where(x => x.ClothesId == cloth.ClothesId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                    //    + order.OrderScales.Where(x => x.ClothesId == cloth.ClothesId).Sum(x => x.Quantity * (x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0)));
                    decimal price = 0;
                    if (prices.Any(x => x.ClothesId == cloth.ClothesId && x.Price.HasValue))
                        price = prices.FirstOrDefault(x => x.ClothesId == cloth.ClothesId).Price.Value;
                    else
                        price = loadRetail ? cloth.MSRP.Value : cloth.Price.Value;
                    if (Qty > 0 && price > 0)
                    {
                        var item = new ReceiptItems();
                        item.Style = cloth.StyleNumber;
                        item.Total = "";
                        item.Qty = Qty.ToString();
                        if (prices.Any(x => x.ClothesId == cloth.ClothesId && x.Price.HasValue))
                            item.Total = string.Format("${0:0.00}", price);
                        else
                            item.Total = string.Format("${0:0.00}", price);
                        if (cloth.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                            item.Img = cloth.ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(x => x.SortOrder).FirstOrDefault().ImagePath;
                        else
                            item.Img = "NO_IMAGE.jpg";
                        ret.Items.Add(item);
                    }
                }
                if (order.Discount > 0)
                    ret.Discount = string.Format("-${0:0.00}", ((order.GrandTotal + order.ShippingCost) - order.FinalAmount));
                ret.SubTotal = string.Format("${0:0.00}", (order.GrandTotal - (order.GrandTotal * (order.Discount.HasValue ? order.Discount.Value / 100 : 0))));
                if (order.ShippingCost > 0)
                    ret.Shipping = string.Format("${0:0.00}", (order.ShippingCost.Value));
                else
                    ret.Shipping = "Free";
                ret.GrandTotal = string.Format("${0:0.00}", order.FinalAmount);

                var txn = order.Transactions.FirstOrDefault(x => x.Status == (int)TransactionStatus.Completed);
                if (txn != null)
                {
                    if (txn.TransactionType == "paypal" && !string.IsNullOrEmpty(txn.CardType))
                    {
                        if (txn.CardType.ToLower() == "visa")
                            ret.PaymentMethodImg = "visa.png";
                        else if (txn.CardType.ToLower() == "mastercard")
                            ret.PaymentMethodImg = "mc.png";
                        else if (txn.CardType.ToLower() == "amex")
                            ret.PaymentMethodImg = "ax.png";
                        else if (txn.CardType.ToLower() == "discover")
                            ret.PaymentMethodImg = "dc.png";
                    }
                    else if (txn.TransactionType == "expresscheckout")
                        ret.PaymentMethodImg = "payPal.jpg";
                }
                return PartialView(ret);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult SearchDeactivated(string searchcred, int page)
        {
            ClothList retlist = new ClothList();
            if (!string.IsNullOrEmpty(searchcred))
            {
                var cList = searchcred.Split(',').ToList();
                var list = new List<DB.Cloth>();
                list = db.Clothes.Where(i => i.IsActive == false && i.IsDelete == false).ToList();
                var testList = list.SelectMany(x => db.Categories.Where(y => y.CategoryId == x.Category.ParentId), (x, y) => new { x, sub = y.Name.ToLower(), subid = y.ParentId }).ToList().
                    SelectMany(x => db.Categories.Where(y => y.CategoryId == x.subid), (x, y) => new { cloth = x.x, x.sub, cat = y.Name.ToLower() }).ToList();
                list = testList.Where(x => (!string.IsNullOrEmpty(x.cloth.Color) ? cList.Any(y => y.Contains(x.cloth.Color.ToLower()) || x.cloth.Color.ToLower().Contains(y)) : false) ||
                        (!string.IsNullOrEmpty(x.cloth.StyleNumber) ? cList.Any(y => y.Contains(x.cloth.StyleNumber.ToLower()) || x.cloth.StyleNumber.ToLower().Contains(y)) : false)
                        || (!string.IsNullOrEmpty(x.cloth.Category.Name) ? cList.Any(y => y == x.cloth.Category.Name.ToLower()) : false)
                        || (cList.Any(y => y == x.sub)) || (cList.Any(y => y == x.cat)) ||
                        (!string.IsNullOrEmpty(x.cloth.Tags) ? x.cloth.Tags.Split(',').Distinct().SelectMany(y => cList.Where(z => !string.IsNullOrEmpty(y) && (z.Contains(y.ToLower()) || y.Contains(z.ToLower()))), (y, z) => new { y, z }).Count() > 0 : false)
                        || (!string.IsNullOrEmpty(x.cloth.ClothesDescription) ? (cList.Any(y => x.cloth.ClothesDescription.ToLower().Contains(y))) : false)
                        ).Select(x => x.cloth).ToList();

                list = list.OrderBy(x => x.Category.Name).ThenByDescending(x => x.DateChanged).ToList();
                list = list.Skip(12 * page).Take(12).ToList();

                retlist.List.InjectFrom(list);

                foreach (var item in retlist.List)
                {
                    var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                    item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
                }
            }
            return PartialView("ClothesPartialNew", retlist);
        }

        [HttpPost]
        public ActionResult SearchDeactivated(string search)
        {
            ClothList retlist = new ClothList();
            var list = db.Clothes.Where(i => i.IsActive == false && i.IsDelete == false && i.StyleNumber.Contains(search)).
                        OrderByDescending(x => x.DateChanged).ToList();

            retlist.List.InjectFrom(list);
            foreach (var item in retlist.List)
            {
                var image = list.Where(x => x.ClothesId == item.ClothesId).FirstOrDefault().ClothesImages.Where(x => x.IsActive && !x.IsDelete).OrderBy(i => i.SortOrder).FirstOrDefault();
                item.ImagePath = image != null ? image.ImagePath : "NO_IMAGE.jpg";
            }
            return PartialView("ClothesPartialNew", retlist);
        }

        [HttpPost]
        public ActionResult ClothPresent(int ClothesId, bool isPre, bool isOpen)
        {

            int UserId = 0;
            bool isCookie = string.IsNullOrEmpty(SiteIdentity.UserId);
            int StatusId = 0;
            List<string> Errors = new List<string>();
            DB.Order lastOrder = null;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            int.TryParse(SiteIdentity.UserId, out UserId);
            if (Session["Order"] == null && Session["EditingOrder"] == null && Session["WasCleared"] == null && !isCookie)
            {
                if (db.Orders.Where(x => x.AccountId == UserId && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).Count() > 0)
                {
                    lastOrder = db.Orders.Where(x => x.AccountId == UserId && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
                    if (lastOrder != null)
                        Session["Order"] = lastOrder.OrderId;
                }
            }
            else if (Session["EditingOrder"] != null)
                lastOrder = db.Orders.Find((Guid)Session["EditingOrder"]);
            else if (Session["Order"] != null)
                lastOrder = db.Orders.Find((Guid)Session["Order"]);
            else if (isCookie)
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        var cookieCart = JsonConvert.DeserializeObject<DB.Order>(ordCookie.Value);
                        lastOrder = cookieCart;
                    }
                    catch
                    {
                    }
                }
            }
            if (lastOrder != null)
            {
                var clothes = new List<int>(lastOrder.OrderScales.Count + lastOrder.OrderSizes.Count);
                if (!isCookie)
                {
                    if (isPre)
                        clothes.AddRange(lastOrder.OrderScales.Select(x => x.ClothesScale.ClothesId));
                    if (isOpen)
                        clothes.AddRange(lastOrder.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.ClothesId));
                }
                else
                {
                    var tempC = lastOrder.OrderSizes.Select(x => x.ClothesId);
                    clothes.AddRange(db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false && tempC.Contains(x.ClothesId)).ToList().Select(x => x.ClothesId));
                }
                clothes = clothes.Distinct().ToList();
                if (clothes.Contains(ClothesId))
                {
                    string name = "(no name availble)";
                    if (!isCookie)
                    {
                        var company = db.Companies.FirstOrDefault(x => x.AccountId == UserId && !string.IsNullOrEmpty(x.Name));
                        if (company != null)
                            name = company.Name;
                    }
                    return Json(name, JsonRequestBehavior.AllowGet);
                }
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        public PayPal.Api.Payment DoDirectCardPayment(string fname, string lname, string CardNumber, string CVV, string CardType, int ExpMonth, int ExpYear, Guid OrderId, BillInfoAddress bAddress, BillInfoAddress sAddress, string email, decimal amount, out CreditCard cc)
        {
            PayPal.Api.Payment result = null;
            cc = null;
            try
            {
                var paypalSetup = new PaypalOps();
                var Api = paypalSetup.Api;
                cc = paypalSetup.SetUpCard(fname, lname, CardNumber, CVV, ExpMonth, ExpYear, CardType);
                var payer = new Payer();
                payer.payment_method = "credit_card";
                if (cc != null)
                {
                    cc.billing_address = new PayPal.Api.Address();
                    cc.billing_address.city = bAddress.City;
                    cc.billing_address.line1 = bAddress.Address1;
                    if (!string.IsNullOrEmpty(bAddress.Address2))
                        cc.billing_address.line2 = bAddress.Address2;
                    cc.billing_address.state = bAddress.State;
                    cc.billing_address.postal_code = bAddress.Zip;
                    cc.billing_address.country_code = "US";

                    payer.funding_instruments = new List<FundingInstrument>() { new FundingInstrument() { credit_card = cc } };
                    payer.payer_info = new PayerInfo();
                    payer.payer_info.first_name = cc.first_name;
                    payer.payer_info.last_name = cc.last_name;
                    payer.payer_info.email = email;

                    payer.payer_info.shipping_address = new ShippingAddress();
                    payer.payer_info.shipping_address.city = sAddress.City;
                    payer.payer_info.shipping_address.line1 = sAddress.Address1;
                    if (!string.IsNullOrEmpty(sAddress.Address2))
                        payer.payer_info.shipping_address.line2 = sAddress.Address2;
                    payer.payer_info.shipping_address.state = sAddress.State;
                    payer.payer_info.shipping_address.postal_code = sAddress.Zip;
                    payer.payer_info.shipping_address.country_code = "US";

                    var txn = new PayPal.Api.Transaction();
                    txn.soft_descriptor = "Platini Items";
                    txn.description = "Platini Items";

                    txn.amount = new Amount();
                    txn.amount.currency = "USD";
                    txn.amount.total = string.Format("{0:0.00}", amount);

                    var itemList = GetTransactionItems(OrderId);
                    if (itemList != null)
                    {
                        txn.item_list = new ItemList();
                        txn.item_list.items = itemList;
                    }

                    var payment = new PayPal.Api.Payment();
                    payment.intent = "sale";
                    payment.payer = payer;
                    payment.transactions = new List<PayPal.Api.Transaction>();
                    payment.transactions.Add(txn);

                    result = payment.Create(Api);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        [NonAction]
        public PayPal.Api.Payment DoSavedCardPayment(string CardId, Guid OrderId, BillInfoAddress sAddress, string email, decimal amount)
        {
            PayPal.Api.Payment result = null;
            try
            {
                var paypalSetup = new PaypalOps();
                var Api = paypalSetup.Api;
                CreditCardToken cct = new CreditCardToken() { credit_card_id = CardId };
                var payer = new Payer();
                payer.payment_method = "credit_card";
                payer.funding_instruments = new List<FundingInstrument>() { new FundingInstrument() { credit_card_token = cct } };

                payer.payer_info = new PayerInfo();
                payer.payer_info.email = email;

                payer.payer_info.shipping_address = new ShippingAddress();
                payer.payer_info.shipping_address.city = sAddress.City;
                payer.payer_info.shipping_address.line1 = sAddress.Address1;
                if (!string.IsNullOrEmpty(sAddress.Address2))
                    payer.payer_info.shipping_address.line2 = sAddress.Address2;
                payer.payer_info.shipping_address.state = sAddress.State;
                payer.payer_info.shipping_address.postal_code = sAddress.Zip;
                payer.payer_info.shipping_address.country_code = "US";

                var txn = new PayPal.Api.Transaction();
                txn.soft_descriptor = "Platini Items";
                txn.description = "Platini Items";

                txn.amount = new Amount();
                txn.amount.currency = "USD";
                txn.amount.total = string.Format("{0:0.00}", amount);

                var itemList = GetTransactionItems(OrderId);
                if (itemList != null)
                {
                    txn.item_list = new ItemList();
                    txn.item_list.items = itemList;
                }

                var payment = new PayPal.Api.Payment();
                payment.intent = "sale";
                payment.payer = payer;
                payment.transactions = new List<PayPal.Api.Transaction>();
                payment.transactions.Add(txn);

                result = payment.Create(Api);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        [NonAction]
        public PayPal.Api.Payment CreatePaypalPayment(Guid OrderId, decimal amount)
        {
            PayPal.Api.Payment result = null;
            try
            {
                var paypalSetup = new PaypalOps();
                var Api = paypalSetup.Api;

                var payer = new Payer();
                payer.payment_method = "paypal";

                var txn = new PayPal.Api.Transaction();
                txn.soft_descriptor = "Platini Items";
                txn.description = "Platini Items";

                txn.amount = new Amount();
                txn.amount.currency = "USD";
                txn.amount.total = string.Format("{0:0.00}", amount);

                var itemList = GetTransactionItems(OrderId);
                if (itemList != null)
                {
                    txn.item_list = new ItemList();
                    txn.item_list.items = itemList;
                }

                var payment = new PayPal.Api.Payment();
                payment.intent = "sale";
                payment.payer = payer;
                payment.transactions = new List<PayPal.Api.Transaction>();
                payment.transactions.Add(txn);

                payment.redirect_urls = new RedirectUrls();
                payment.redirect_urls.cancel_url = ConfigurationManager.AppSettings["BaseUrl"] + "Home/FailurePaypal";
                payment.redirect_urls.return_url = ConfigurationManager.AppSettings["BaseUrl"] + "Home/SuccessPaypal";

                result = payment.Create(Api);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        [NonAction]
        public PayPal.Api.Payment ExecutePaypalPayment(string paymentId, string token, string PayerID)
        {
            PayPal.Api.Payment result = null;
            try
            {
                var paypalSetup = new PaypalOps();
                var payment = new PayPal.Api.Payment();
                payment.id = paymentId;
                var paymentExec = new PaymentExecution();
                paymentExec.payer_id = PayerID;
                result = payment.Execute(paypalSetup.Api, paymentExec);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        [NonAction]
        public List<PayPal.Api.Item> GetTransactionItems(Guid OrderId)
        {
            List<PayPal.Api.Item> retItems = null;
            var order = db.Orders.Find(OrderId);
            if (order != null)
            {
                retItems = new List<PayPal.Api.Item>();
                var prices = order.Account.CustomerItemPrices;
                bool loadRetail = true;
                //if(order.Account.CustomerOptionalInfoes.FirstOrDefault()!=null)
                //    loadRetail=order.Account.CustomerOptionalInfoes.FirstOrDefault().CustomerOptionalInfoId==(int)Platini.Models.CustomerType.Retail;

                var clothes = new List<Cloth>(order.OrderScales.Count + order.OrderSizes.Count);
                clothes.AddRange(order.OrderScales.Select(x => x.ClothesScale.Cloth));
                clothes.AddRange(order.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));
                clothes = clothes.Distinct().ToList();

                foreach (var cloth in clothes)
                {

                    int Qty = order.OrderSizes.Where(x => x.ClothesId == cloth.ClothesId).Sum(x => x.Quantity.HasValue ? x.Quantity.Value : 0);
                    //    + order.OrderScales.Where(x => x.ClothesId == cloth.ClothesId).Sum(x => x.Quantity * (x.ClothesScale.ClothesScaleSizes.Sum(y => y.Quantity.HasValue ? y.Quantity.Value : 0)));
                    decimal price = 0;
                    if (prices.Any(x => x.ClothesId == cloth.ClothesId && x.Price.HasValue))
                        price = prices.FirstOrDefault(x => x.ClothesId == cloth.ClothesId).Price.Value;
                    else
                        price = loadRetail ? cloth.MSRP.Value : cloth.Price.Value;
                    if (price > 0 && Qty > 0)
                    {
                        var item = new PayPal.Api.Item();
                        item.name = cloth.StyleNumber;
                        item.currency = "USD";
                        item.quantity = Qty.ToString();
                        item.price = string.Format("{0:0.00}", price);
                        //item.url = ConfigurationManager.AppSettings["BaseUrl"] + "Home/Detail/" + cloth.StyleNumber;
                        if (!string.IsNullOrEmpty(cloth.ClothesDescription))
                            item.description = cloth.ClothesDescription;
                        retItems.Add(item);
                    }
                }

                if (order.GrandTotal.Value != order.FinalAmount.Value)
                {
                    if (order.ShippingCost.HasValue && order.ShippingCost.Value > 0)
                    {
                        var item = new PayPal.Api.Item();
                        item.name = "Shipping";
                        item.currency = "USD";
                        item.price = string.Format("{0:0.00}", order.ShippingCost.Value);
                        item.quantity = "1";
                        retItems.Add(item);
                    }

                    if (order.Discount.HasValue && order.Discount.Value > 0)
                    {
                        var item = new PayPal.Api.Item();
                        item.name = "Discount";
                        item.currency = "USD";
                        item.price = string.Format("-{0:0.00}", order.GrandTotal.Value * (order.Discount.HasValue ? order.Discount.Value / 100 : 0));
                        item.quantity = "1";
                        retItems.Add(item);
                    }
                }

            }
            return retItems;
        }

        public ActionResult ImportExcel()
        {

            return View();
        }

        [HttpPost]
        public ActionResult ImportExcel(string QuickBook)
        {

            // HttpPostedFileBase upFile = Request.Files["FileUpload1"];

            //Delete Other realed Item From Elenvtial
            #region Comment
            //var account1 = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer).ToList();
            //foreach (var acc in account1)
            //{
            //    var customerinfo = db.CustomerOptionalInfoes.Where(x => x.AccountId == acc.AccountId).FirstOrDefault();

            //    if (customerinfo == null)
            //    {
            //        var company = (db.Companies.Where(x => x.AccountId == acc.AccountId).Count() > 0) ? db.Companies.FirstOrDefault(x => x.AccountId == acc.AccountId) : new Platini.DB.Company();
            //        CustomerOptionalInfo custinfo = new CustomerOptionalInfo();
            //        custinfo.AccountId = acc.AccountId;
            //        //custinfo.SecondPassword = Encoding.ASCII.GetBytes(RandomPassword.Generate(4, 6));
            //        //custinfo.BusinessReseller = "0";
            //        custinfo.DisplayName = (company != null) ? company.Name : "";
            //        custinfo.CustomerType = (int)Platini.Models.CustomerType.Wholesale;
            //        custinfo.DateCreated = DateTime.UtcNow;
            //        //custinfo.DateUpdated = DateTime.UtcNow;
            //        custinfo.Discount = 0;
            //        try
            //        {
            //            db.CustomerOptionalInfoes.Add(custinfo);
            //            db.SaveChanges();
            //        }
            //        catch { }
            //    }
            //}
            #endregion
            #region Delete Data

            //int RoleId= (int)RolesEnum.Customer;
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [OrderScale]");
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [OrderSize]");
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [OrderLog]");
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [Bag]");
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [Box]");
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [Transactions]");
            //var OrderList = db.Orders.ToList();
            //foreach (var OrList in OrderList)
            //{
            //    db.Orders.Attach(OrList);
            //    db.Orders.Remove(OrList);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}

            //var addressList = db.Addresses.ToList();
            //foreach (var add in addressList)
            //{
            //    db.Addresses.Attach(add);
            //    db.Addresses.Remove(add);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}
            //var Option = db.CustomerOptionalInfoes.ToList();
            //foreach (var com in Option)
            //{
            //    db.CustomerOptionalInfoes.Attach(com);
            //    db.CustomerOptionalInfoes.Remove(com);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [CustomerOptionalInfo]");
            //var sales = db.CustomerSalesPersons.ToList();
            //foreach (var com in sales)
            //{               
            //    db.CustomerSalesPersons.Attach(com);
            //    db.CustomerSalesPersons.Remove(com);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [CustomerSalesPerson]");
            //var custUser = db.CustomerUsers.ToList();
            //foreach (var com in custUser)
            //{              
            //    db.CustomerUsers.Attach(com);
            //    db.CustomerUsers.Remove(com);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [CustomerUser]");
            //var itemPrice = db.CustomerItemPrices.ToList();
            //foreach (var com in itemPrice)
            //{               
            //    db.CustomerItemPrices.Attach(com);
            //    db.CustomerItemPrices.Remove(com);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [CustomerItemPrice]");
            //var company = db.Companies.ToList();
            //foreach (var com in company)
            //{                
            //    db.Companies.Attach(com);
            //    db.Companies.Remove(com);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [Company]");
            //db.Database.ExecuteSqlCommand("TRUNCATE TABLE [ExtraPermission]");
            //var communication = db.Communications.ToList(); ;
            //foreach (var com in communication)
            //{
            //db.Communications.Attach(com);
            //db.Communications.Remove(com);
            //try
            //{
            //db.SaveChanges();
            //}
            //catch { }
            //}

            //var account1 = db.Accounts.Where(x => x.RoleId == (int)RolesEnum.Customer).ToList();
            //foreach (var acc in account1)
            //{
            //    db.Accounts.Attach(acc);
            //    db.Accounts.Remove(acc);
            //    try
            //    {
            //        db.SaveChanges();
            //    }
            //    catch { }
            //}
            #endregion
            //int Counter = 0;
            #region Comment
            //try
            //{

            //if (upFile.ContentLength > 0)
            //{
            //    string path = string.Format("{0}/{1}", Server.MapPath("~/Library/Uploads"), upFile.FileName);
            //    if (System.IO.File.Exists(path))
            //        System.IO.File.Delete(path);
            //    upFile.SaveAs(path);
            //    string Query = string.Empty;
            //    //Create connection string to Excel work book
            //    string excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties=Excel 12.0;Persist Security Info=False";

            //    //Create Connection to Excel work book
            //    System.Data.OleDb.OleDbConnection excelConnection = new System.Data.OleDb.OleDbConnection(excelConnectionString);
            //    excelConnection.Open();
            //    System.Data.DataTable dtExcelsheetname = new System.Data.DataTable();
            //    dtExcelsheetname = excelConnection.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            //    string[] excelSheets = new String[dtExcelsheetname.Rows.Count];
            //    int j = 0;
            //    foreach (System.Data.DataRow row in dtExcelsheetname.Rows)
            //    {
            //        excelSheets[j] = row["TABLE_NAME"].ToString();
            //        Query = string.Format("Select [Customer],[Company],[Address],[Phone],[Email],[Open Balance],[Notes] from [{0}]", excelSheets[j]);
            //        j++;
            //    }

            //    //Create OleDbCommand to fetch data from Excel
            //    System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(Query, excelConnection);

            //    //System.Data.OleDb.OleDbDataReader dReader;              
            //    System.Data.DataSet ds = new System.Data.DataSet();
            //    System.Data.OleDb.OleDbDataAdapter oda = new System.Data.OleDb.OleDbDataAdapter(Query, excelConnection);
            //    excelConnection.Close();
            //    oda.Fill(ds);
            //    System.Data.DataTable Exceldt = ds.Tables[0];

            //    if (Exceldt.Rows.Count <= 0)
            //        return View();
            //    int stepCounter = 0;
            //    foreach (System.Data.DataRow cust in Exceldt.Rows)
            //    {

            //        //stepCounter++;
            //        //if (stepCounter < 2604)
            //        //    continue;
            //        string Name = cust["Customer"].ToString();
            //        var CustomerId = db.Companies.Where(x => x.Name == Name).FirstOrDefault() != null ? db.Companies.Where(x => x.Name == Name).FirstOrDefault().AccountId : 0;
            //        QuickBookStrings.LoadQuickBookStrings(FailureFrom.Customer.ToString());
            //        string QuickCompany = string.Empty;
            //        string CompanyName = string.Empty;
            //        string Type = FailureFrom.Customer.ToString();
            //        var oauthValidator = new OAuthRequestValidator(QuickBookStrings.AccessToken, QuickBookStrings.AccessTokenSecret, QuickBookStrings.ConsumerKey, QuickBookStrings.ConsumerSecret);
            //        QuickBookFailureRecord existFailure = null;
            //        try
            //        {
            //            var context = new ServiceContext(QuickBookStrings.AppToken, QuickBookStrings.CompanyId, IntuitServicesType.QBO, oauthValidator);
            //            context.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = GetLoc(Type, CustomerId.ToString());
            //            context.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl;
            //            var service = new DataService(context);
            //            var customer = new Customer();
            //            var objCustomer = new Customer();
            //            var objCustomer1 = new Customer();
            //            var term = new Intuit.Ipp.Data.Term();
            //            QueryService<Intuit.Ipp.Data.Term> termQueryService = new QueryService<Intuit.Ipp.Data.Term>(context);
            //            var account = new QueryService<Customer>(context);
            //            var regstAccount = db.Accounts.Find(CustomerId);
            //            if (existFailure != null)
            //            {
            //                CompanyName = existFailure.FailureOriginalValue;
            //            }
            //            else if (regstAccount != null)
            //            {
            //                if (regstAccount.IsActive == true && regstAccount.IsDelete == false && regstAccount.Companies.FirstOrDefault() != null)
            //                    CompanyName = regstAccount.Companies.FirstOrDefault().Name;
            //            }
            //            else
            //                CompanyName = cust["Customer"].ToString();
            //            QuickCompany = CompanyName.Replace("'", "");
            //            QuickCompany = QuickCompany.Replace(":", " ");

            //            if (CompanyName == "Andrea's")
            //            {

            //            }

            //            //objCustomer1 = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + Convert.ToString(QuickCompany).Trim() + "'").FirstOrDefault();
            //            //if ((objCustomer1 != null))
            //            //{
            //            //    objCustomer1.ActiveSpecified = true;
            //            //    objCustomer1.Active = false;
            //            //    service.Update(objCustomer1);
            //            //}

            //            if (!string.IsNullOrEmpty(CompanyName))
            //            {
            //                //string Email1 = !string.IsNullOrEmpty(cust["Email"].ToString()) ? cust["Email"].ToString() : "";                                
            //                //DB.Account accountDB1 = null;
            //                //if (!string.IsNullOrEmpty(CompanyName))
            //                //{
            //                //    accountDB1 = db.Accounts.Where(x => x.FirstName == CompanyName).FirstOrDefault();
            //                //    if (accountDB1 != null)
            //                //        continue;
            //                //}
            //                //else
            //                //  continue;

            //                #region Address
            //                DB.Address billingAddress = new DB.Address();
            //                DB.Address shippingAddress = new DB.Address();
            //                if (regstAccount != null)
            //                    billingAddress = regstAccount.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.BillingAddress && x.IsActive == true && x.IsDelete == false);

            //                if (regstAccount == null)
            //                {
            //                    string[] address = cust["Address"].ToString().Split(',');
            //                    if (address.Count() > 0)
            //                    {
            //                        billingAddress.Street = address[0].ToString();
            //                        if (!string.IsNullOrEmpty(cust["Address"].ToString())) ;
            //                        {
            //                            if (address[1].ToString() == string.Empty)
            //                            {
            //                                billingAddress.City = address[2].ToString();
            //                                string[] pState = address[3].ToString().Split(' ');
            //                                if (pState.Count() > 0)
            //                                {
            //                                    billingAddress.State = pState[0].ToString();
            //                                    billingAddress.Pincode = pState[1].ToString();
            //                                }
            //                                if (address.Count() > 4)
            //                                    billingAddress.Country = address[4].ToString();
            //                            }
            //                            else
            //                            {
            //                                billingAddress.City = address[1].ToString();
            //                                string[] pState = address[2].ToString().Split(' ');
            //                                if (pState.Count() > 0)
            //                                {
            //                                    billingAddress.State = pState[0].ToString();
            //                                    billingAddress.Pincode = pState[1].ToString();
            //                                }
            //                                if (address.Count() > 3)
            //                                    billingAddress.Country = address[3].ToString();
            //                            }
            //                        }
            //                    }
            //                }
            //                if (regstAccount != null)
            //                    shippingAddress = regstAccount.Addresses.FirstOrDefault(x => x.AddressTypeId == (int)AddressTypeEnum.ShippingAddress && x.IsActive == true && x.IsDelete == false);
            //                if (regstAccount == null)
            //                {
            //                    string[] address = cust["Address"].ToString().Split(',');
            //                    if (address.Count() > 0)
            //                    {
            //                        shippingAddress.Street = address[0].ToString();
            //                        if (address[1].ToString() == string.Empty)
            //                        {
            //                            shippingAddress.City = address[2].ToString();
            //                            string[] pState = address[3].ToString().Split(' ');
            //                            if (pState.Count() > 0)
            //                            {
            //                                shippingAddress.State = pState[0].ToString();
            //                                shippingAddress.Pincode = pState[1].ToString();
            //                            }
            //                            if (address.Count() > 4)
            //                                shippingAddress.Country = address[4].ToString();
            //                        }
            //                        else
            //                        {
            //                            shippingAddress.City = address[1].ToString();
            //                            string[] pState = address[2].ToString().Split(' ');
            //                            if (pState.Count() > 0)
            //                            {
            //                                shippingAddress.State = pState[0].ToString();
            //                                shippingAddress.Pincode = pState[1].ToString();
            //                            }
            //                            if (address.Count() > 3)
            //                                shippingAddress.Country = address[3].ToString();
            //                        }
            //                    }
            //                }
            //                #endregion

            //                #region Elev

            //                //string Email = !string.IsNullOrEmpty(cust["Email"].ToString()) ? cust["Email"].ToString() : "";
            //                //string PhoneNumber = !string.IsNullOrEmpty(cust["Phone"].ToString()) ? cust["Phone"].ToString() : "";
            //                //DB.Account accountDB = null;
            //                //if (!string.IsNullOrEmpty(Email))
            //                //    accountDB = db.Accounts.Where(x => x.UserName == Email).FirstOrDefault();
            //                //if (accountDB != null)
            //                //{
            //                //    var dbAccount = new DB.Account();
            //                //    dbAccount.FirstName = cust["Customer"].ToString();
            //                //    dbAccount.LastName = "";
            //                //    dbAccount.UserName = dbAccount.Email = Email;
            //                //    dbAccount.IsActive = true;
            //                //    dbAccount.IsDelete = false;
            //                //    dbAccount.DateCreated = dbAccount.DateUpdated = DateTime.UtcNow;
            //                //    dbAccount.RoleId = db.Roles.Where(x => x.RoleId == (int)RolesEnum.Customer).FirstOrDefault().RoleId;
            //                //    dbAccount.Password = Encoding.ASCII.GetBytes(RandomPassword.Generate(4, 6));                                    
            //                //    db.SaveChanges();

            //                //    var dbCommunication = new Communication
            //                //    {
            //                //        Phone = PhoneNumber,
            //                //        AccountId = dbAccount.AccountId,
            //                //        IsActive = true,
            //                //        IsDelete = false,
            //                //        DateCreated = DateTime.UtcNow,
            //                //        DateUpdated = DateTime.UtcNow
            //                //    };

            //                //    db.Communications.Add(dbCommunication);
            //                //    db.SaveChanges();

            //                //    string cName = string.Empty;
            //                //    cName = SaveUniqueCompany(CompanyName, billingAddress.City, dbAccount.AccountId);
            //                //    if (billingAddress != null)
            //                //    {
            //                //        if (billingAddress.City != null)
            //                //        {
            //                //            var dbAddress = new DB.Address();
            //                //            dbAddress.InjectClass(billingAddress);
            //                //            dbAddress.AccountId = dbAccount.AccountId;
            //                //            dbAddress.IsActive = true;
            //                //            dbAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
            //                //            dbAddress.IsDelete = false;
            //                //            dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
            //                //            db.Addresses.Add(dbAddress);
            //                //            db.SaveChanges();
            //                //        }
            //                //    }

            //                //    if (shippingAddress != null)
            //                //    {
            //                //        if (shippingAddress.City != null)
            //                //        {
            //                //            var dbAddress = new DB.Address();
            //                //            dbAddress.InjectClass(shippingAddress);
            //                //            dbAddress.AccountId = dbAccount.AccountId;
            //                //            dbAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
            //                //            dbAddress.IsActive = true;
            //                //            dbAddress.IsDelete = false;
            //                //            dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
            //                //            db.Addresses.Add(dbAddress);
            //                //            db.SaveChanges();
            //                //        }
            //                //    }
            //                //}
            //                //else
            //                //{
            //                //    var dbAccount = new DB.Account();
            //                //    dbAccount.FirstName = cust["Customer"].ToString();
            //                //    dbAccount.LastName = "";
            //                //    dbAccount.UserName = dbAccount.Email = Email;
            //                //    dbAccount.IsActive = true;
            //                //    dbAccount.IsDelete = false;
            //                //    dbAccount.DateCreated = dbAccount.DateUpdated = DateTime.UtcNow;
            //                //    dbAccount.RoleId = db.Roles.Where(x => x.RoleId == (int)RolesEnum.Customer).FirstOrDefault().RoleId;
            //                //    dbAccount.Password = Encoding.ASCII.GetBytes(RandomPassword.Generate(4, 6));
            //                //    db.Accounts.Add(dbAccount);                                    
            //                //    db.SaveChanges();

            //                //    var dbCommunication = new Communication
            //                //    {
            //                //        Phone = PhoneNumber,
            //                //        AccountId = dbAccount.AccountId,
            //                //        IsActive = true,
            //                //        IsDelete = false,
            //                //        DateCreated = DateTime.UtcNow,
            //                //        DateUpdated = DateTime.UtcNow
            //                //    };

            //                //    db.Communications.Add(dbCommunication);
            //                //    db.SaveChanges();

            //                //    string cName = string.Empty;
            //                //    cName = SaveUniqueCompany(CompanyName, billingAddress.City, dbAccount.AccountId);
            //                //    if (billingAddress != null)
            //                //    {
            //                //        if (billingAddress.City != null)
            //                //        {
            //                //            var dbAddress = new DB.Address();
            //                //            dbAddress.InjectClass(billingAddress);
            //                //            dbAddress.AccountId = dbAccount.AccountId;
            //                //            dbAddress.IsActive = true;
            //                //            dbAddress.IsDelete = false;
            //                //            dbAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
            //                //            dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
            //                //            db.Addresses.Add(dbAddress);
            //                //            db.SaveChanges();
            //                //        }
            //                //    }
            //                //    if (shippingAddress != null)
            //                //    {
            //                //        if (shippingAddress.City != null)
            //                //        {
            //                //            var dbAddress = new DB.Address();
            //                //            dbAddress.InjectClass(shippingAddress);
            //                //            dbAddress.AccountId = dbAccount.AccountId;
            //                //            dbAddress.AddressTypeId = (int)AddressTypeEnum.ShippingAddress;
            //                //            dbAddress.IsActive = true;
            //                //            dbAddress.IsDelete = false;
            //                //            dbAddress.DateCreated = dbAddress.DateUpdated = DateTime.UtcNow;
            //                //            db.Addresses.Add(dbAddress);
            //                //            db.SaveChanges();
            //                //        }
            //                //    }
            //                //}

            //                #endregion

            //                #region ExcelImport Quickbook
            //                objCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + QuickCompany + "'").FirstOrDefault();
            //                var terms = new QueryService<Intuit.Ipp.Data.Term>(context);
            //                term = terms.ExecuteIdsQuery("Select * From Term Where Name='COD'").FirstOrDefault();
            //                if (objCustomer != null)
            //                {
            //                    // objCustomer.Title = "Mr.";
            //                    objCustomer.DisplayName = QuickCompany;
            //                    objCustomer.CompanyName = QuickCompany;
            //                    if (regstAccount == null)
            //                    {
            //                        objCustomer.GivenName = objCustomer.FamilyName = "";
            //                    }
            //                    else
            //                    {
            //                        objCustomer.GivenName = !string.IsNullOrEmpty(regstAccount.FirstName) ? regstAccount.FirstName : "";
            //                        objCustomer.FamilyName = !string.IsNullOrEmpty(regstAccount.LastName) ? regstAccount.LastName : "";
            //                        objCustomer.Fax = new TelephoneNumber() { AreaCode = "01", FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Fax : "" };
            //                    }
            //                    objCustomer.PrimaryPhone = new TelephoneNumber() { FreeFormNumber = !string.IsNullOrEmpty(cust["Phone"].ToString()) ? cust["Phone"].ToString() : "" };
            //                    objCustomer.PrimaryEmailAddr = new EmailAddress() { Address = cust["Email"].ToString() };

            //                    if (billingAddress != null)
            //                        objCustomer.BillAddr = new PhysicalAddress() { City = billingAddress.City, Line1 = billingAddress.Street, PostalCode = billingAddress.Pincode, CountrySubDivisionCode = billingAddress.State };
            //                    if (shippingAddress != null)
            //                        objCustomer.ShipAddr = new PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State };

            //                    objCustomer.Balance = Convert.ToDecimal(cust["Open Balance"]);
            //                    objCustomer.Notes = cust["Notes"].ToString();
            //                    if (term != null)
            //                        objCustomer.SalesTermRef = new ReferenceType() { name = term.Name, Value = term.Id };
            //                    service.Update(objCustomer);
            //                    Counter += 1;
            //                }
            //                else
            //                {
            //                    objCustomer = new Customer();
            //                    objCustomer.DisplayName = QuickCompany;
            //                    objCustomer.CompanyName = QuickCompany;
            //                    if (regstAccount == null)
            //                    {
            //                        objCustomer.GivenName = objCustomer.FamilyName = "";
            //                    }
            //                    else
            //                    {
            //                        objCustomer.GivenName = !string.IsNullOrEmpty(regstAccount.FirstName) ? regstAccount.FirstName : "";
            //                        objCustomer.FamilyName = !string.IsNullOrEmpty(regstAccount.LastName) ? regstAccount.LastName : "";
            //                        objCustomer.Fax = new TelephoneNumber() { AreaCode = "01", FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Fax : "" };
            //                    }
            //                    objCustomer.PrimaryPhone = new TelephoneNumber() { FreeFormNumber = !string.IsNullOrEmpty(cust["Phone"].ToString()) ? cust["Phone"].ToString() : "" };
            //                    objCustomer.PrimaryEmailAddr = new EmailAddress() { Address = cust["Email"].ToString() };
            //                    if (billingAddress != null)
            //                        objCustomer.BillAddr = new PhysicalAddress() { City = billingAddress.City, Line1 = billingAddress.Street, PostalCode = billingAddress.Pincode, CountrySubDivisionCode = billingAddress.State };
            //                    if (shippingAddress != null)
            //                        objCustomer.ShipAddr = new PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State };

            //                    objCustomer.Balance = Convert.ToDecimal(cust["Open Balance"]);
            //                    objCustomer.Notes = cust["Notes"].ToString();
            //                    if (term != null)
            //                        objCustomer.SalesTermRef = new ReferenceType() { name = term.Name, Value = term.Id };
            //                    service.Add(objCustomer);
            //                    Counter += 1;
            //                }
            //            #endregion
            //        }
            //    }
            //    catch (Intuit.Ipp.Exception.IdsException ex)
            //    {
            //        //return ProcessEntityValidationError(entity, ex);
            //    }
            //    catch (System.Data.Entity.Validation.DbEntityValidationException e)
            //    {
            //        foreach (var eve in e.EntityValidationErrors)
            //        {
            //            Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
            //                eve.Entry.Entity.GetType().Name, eve.Entry.State);
            //            foreach (var ve in eve.ValidationErrors)
            //            {
            //                Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
            //                    ve.PropertyName, ve.ErrorMessage);
            //            }
            //        }
            //        throw;
            //    }
            //    catch (Exception ex)
            //    {
            //        var newFailure = new QuickBookFailureRecord();
            //        newFailure.FailureFrom = FailureFrom.Customer.ToString();
            //        newFailure.FailureFromId = CustomerId;
            //        newFailure.FailureOriginalValue = CompanyName;
            //        newFailure.ErrorDetails = FailureText(ex, Type, CustomerId.ToString());
            //        newFailure.FailureOriginalValue = CompanyName;
            //        newFailure.FailureDate = DateTime.UtcNow;
            //        db.QuickBookFailureRecords.Add(newFailure);
            //        db.SaveChanges();
            //    }
            //}
            ////dReader = cmd.ExecuteReader();
            //ViewBag.SuccessMsg = "Success";
            //ViewBag.Counter = Counter;
            //        return View();
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
            #endregion

            #region Cloth
            var clothList = db.Clothes.Where(x => x.IsActive == true && x.IsDelete == false).ToList();
            foreach (var model in clothList)
            {
                if (model != null)
                {
                    QuickBookStrings.LoadQuickBookStrings(FailureFrom.Product.ToString());
                    string StyleNumber = string.Empty;
                    string Type = FailureFrom.Product.ToString();
                    var oauthValidator = new OAuthRequestValidator(QuickBookStrings.AccessToken, QuickBookStrings.AccessTokenSecret, QuickBookStrings.ConsumerKey, QuickBookStrings.ConsumerSecret);
                    QuickBookFailureRecord existFailure = null;
                    if (model.ClothesId > 0)
                        existFailure = db.QuickBookFailureRecords.FirstOrDefault(x => x.FailureFrom.ToLower() == Type.ToLower() && x.FailureFromId == model.ClothesId);
                    try
                    {
                        var context = new ServiceContext(QuickBookStrings.AppToken, QuickBookStrings.CompanyId, IntuitServicesType.QBO, oauthValidator);
                        context.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = GetLoc(Type, model.ClothesId.ToString());
                        context.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl;

                        var service = new DataService(context);
                        var productService = new QueryService<Intuit.Ipp.Data.Item>(context);
                        var newProduct = new Intuit.Ipp.Data.Item();
                        var dbCloth = db.Clothes.Find(model.ClothesId);
                        if (existFailure != null)
                            StyleNumber = existFailure.FailureOriginalValue;
                        else if (dbCloth != null)
                        {
                            if (!string.IsNullOrEmpty(dbCloth.StyleNumber) && dbCloth.IsActive == true && dbCloth.IsDelete == false)
                                StyleNumber = dbCloth.StyleNumber;
                        }
                        if (!string.IsNullOrEmpty(StyleNumber) && dbCloth != null)
                        {
                            newProduct = productService.ExecuteIdsQuery("Select * From Item where Name='" + StyleNumber + "'").FirstOrDefault();
                            if (newProduct == null)
                            {
                                newProduct = new Intuit.Ipp.Data.Item();
                                newProduct.SpecialItem = true;
                                newProduct.UnitPriceSpecified = true;
                                newProduct.Name = StyleNumber;
                                newProduct.FullyQualifiedName = GetNames(dbCloth, 1);
                                newProduct.Active = true;
                                newProduct.PurchaseDesc = GetNames(dbCloth, 2);
                                newProduct.Description = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                                newProduct.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                                newProduct.UnitPriceSpecified = true;
                                newProduct.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                                newProduct.PurchaseCostSpecified = true;
                                newProduct.ActiveSpecified = true;
                                newProduct.InvStartDate = DateTime.UtcNow;
                                newProduct.InvStartDateSpecified = true;
                                newProduct.Type = ItemTypeEnum.Inventory;
                                newProduct.QtyOnHand = (dbCloth.ClothesScales.Where(x => x.IsOpenSize == true && x.IsActive == true && x.IsDelete == false).Sum(x => x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0))))
                                + (dbCloth.ClothesScales.Where(x => x.IsOpenSize == false && x.IsActive == true && x.IsDelete == false).Sum(x => (x.InvQty.HasValue ? x.InvQty.Value : 0) * (x.ClothesScaleSizes.Sum(y => (y.Quantity.HasValue ? y.Quantity.Value : 0)))));
                                newProduct.TrackQtyOnHand = true;
                                //newProduct.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income" , Value = Convert.ToInt32(newProduct.PurchaseCost).ToString() };
                                //newProduct.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = dbCloth.MSRP.HasValue ? Convert.ToInt32(dbCloth.MSRP.Value).ToString() : "0" };
                                //newProduct.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = Convert.ToInt32(newProduct.UnitPrice).ToString() };
                                newProduct.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                                newProduct.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                                newProduct.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };
                                service.Add<Intuit.Ipp.Data.Item>(newProduct);
                            }
                            else
                            {
                                newProduct.SpecialItem = true;
                                newProduct.UnitPriceSpecified = true;
                                newProduct.Name = StyleNumber;
                                newProduct.FullyQualifiedName = GetNames(dbCloth, 1);
                                newProduct.Active = true;
                                newProduct.PurchaseDesc = GetNames(dbCloth, 2);
                                newProduct.Description = !string.IsNullOrEmpty(dbCloth.ClothesDescription) ? dbCloth.ClothesDescription : newProduct.FullyQualifiedName;
                                newProduct.UnitPrice = dbCloth.Price.HasValue ? dbCloth.Price.Value : 0.0m;
                                newProduct.UnitPriceSpecified = true;
                                newProduct.PurchaseCost = dbCloth.ProductCost.HasValue ? dbCloth.ProductCost.Value : 0.0m;
                                newProduct.PurchaseCostSpecified = true;
                                newProduct.ActiveSpecified = true;
                                newProduct.IncomeAccountRef = new ReferenceType() { name = "Sales of Product Income", Value = "125" };
                                newProduct.ExpenseAccountRef = new ReferenceType() { name = "Cost of Goods Sold", Value = "122" };
                                newProduct.AssetAccountRef = new ReferenceType() { name = "Inventory Asset", Value = "3" };
                                service.Update<Intuit.Ipp.Data.Item>(newProduct);
                            }
                        }
                    }
                    catch (Intuit.Ipp.Exception.IdsException ex)
                    {
                        //return ProcessEntityValidationError(entity, ex);
                    }
                    catch { }
                }
            }
            #endregion
            return View();
        }

        public ActionResult ClearCart(Guid Id, string Prop)
        {
            var order = db.Orders.Find(Id);
            if (order != null)
            {
                CartOwner cartOwner = new CartOwner();
                cartOwner = Data(order.Account, null);
                if (Prop == "Del")
                {
                    int StatusId = 0;
                    var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
                    if (status != null)
                        StatusId = status.OrderStatusId;
                    if (order.StatusId == StatusId)
                    {
                        if (order.OrderScales.Any())
                        {
                            var scaleList = order.OrderScales.ToList();
                            foreach (var scale in scaleList)
                            {
                                var ttd = new TableToDelete();
                                ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                ttd.TableName = "OrderScale";
                                ttd.TableKey = "OrderScaleId";
                                ttd.TableValue = scale.OrderScaleId.ToString();
                                ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                db.TableToDeletes.Add(ttd);
                                db.OrderScales.Remove(scale);
                            }
                            db.SaveChanges();

                        }
                        if (order.OrderSizes.Any())
                        {
                            var sizeList = order.OrderSizes.ToList();
                            foreach (var size in sizeList)
                            {
                                var ttd = new TableToDelete();
                                ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                ttd.TableName = "OrderSize";
                                ttd.TableKey = "OrderSizeId";
                                ttd.TableValue = size.OrderSizeId.ToString();
                                ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                db.TableToDeletes.Add(ttd);
                                db.OrderSizes.Remove(size);
                            }
                            db.SaveChanges();
                        }
                        var ttdo = new TableToDelete();
                        ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                        ttdo.TableName = "Order";
                        ttdo.TableKey = "OrderId";
                        ttdo.TableValue = order.OrderId.ToString();
                        ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                        db.TableToDeletes.Add(ttdo);
                        db.Orders.Remove(order);
                        Session.Remove("Order");
                        Session.Remove("EditingOrder");
                        Session["WasCleared"] = 1;
                        db.SaveChanges();
                        TempData["CartSuccess"] = "abc";
                        return RedirectToAction("Index", "Home", new { @area = "", OrdId = Id });
                    }
                }
                if (Prop == "Clear")
                {
                    if (string.IsNullOrEmpty(cartOwner.CompanyName))
                    {
                        int StatusId = 0;
                        var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
                        if (status != null)
                            StatusId = status.OrderStatusId;
                        if (order.StatusId == StatusId)
                        {
                            if (order.OrderScales.Any())
                            {
                                var scaleList = order.OrderScales.ToList();
                                foreach (var scale in scaleList)
                                {
                                    var ttd = new TableToDelete();
                                    ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                    ttd.TableName = "OrderScale";
                                    ttd.TableKey = "OrderScaleId";
                                    ttd.TableValue = scale.OrderScaleId.ToString();
                                    ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                    db.TableToDeletes.Add(ttd);
                                    db.OrderScales.Remove(scale);
                                }
                                db.SaveChanges();

                            }
                            if (order.OrderSizes.Any())
                            {
                                var sizeList = order.OrderSizes.ToList();
                                foreach (var size in sizeList)
                                {
                                    var ttd = new TableToDelete();
                                    ttd.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                                    ttd.TableName = "OrderSize";
                                    ttd.TableKey = "OrderSizeId";
                                    ttd.TableValue = size.OrderSizeId.ToString();
                                    ttd.DateCreated = ttd.DateUpdated = DateTime.UtcNow;
                                    db.TableToDeletes.Add(ttd);
                                    db.OrderSizes.Remove(size);
                                }
                                db.SaveChanges();
                            }
                            var ttdo = new TableToDelete();
                            ttdo.DeleteId = GuidHelper.NameGuidFromBytes(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + "-" + System.DateTime.UtcNow.Ticks));
                            ttdo.TableName = "Order";
                            ttdo.TableKey = "OrderId";
                            ttdo.TableValue = order.OrderId.ToString();
                            ttdo.DateCreated = ttdo.DateUpdated = DateTime.UtcNow;
                            db.TableToDeletes.Add(ttdo);
                            db.Orders.Remove(order);
                            Session.Remove("Order");
                            Session.Remove("EditingOrder");
                            Session["WasCleared"] = 1;
                            db.SaveChanges();
                            TempData["CartSuccess"] = "abc";
                            return RedirectToAction("Index", "Home", new { @area = "", OrdId = Id });
                        }
                    }
                    SiteConfiguration.Mode = ModeEnum.Order.ToString();
                    Session.Remove("Order");
                    Session.Remove("EditingOrder");
                    Session["WasCleared"] = 1;
                    //Session["EditingOrder"] = Id;
                    TempData["CartSuccess"] = "abc";
                    return RedirectToAction("Index", "Home", new { @area = "", OrdId = Id });
                }
            }
            else
            {
                var ordCookie = Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    //HttpCookie currentUserCookie = Request.Cookies["currentUser"];
                    HttpContext.Response.Cookies.Remove("ordCookie");
                    ordCookie.Expires = DateTime.Now.AddDays(-1);
                    ordCookie.Value = null;
                    HttpContext.Response.SetCookie(ordCookie);

                    TempData["CartSuccess"] = "abc";
                    return RedirectToAction("Index", "Home", new { @area = "", OrdId = Id });
                }

            }
            TempData["PageMessage"] = "Order not found";
            return RedirectToAction("Index");
        }

        [NonAction]
        public string GetNames(Cloth dbCloth, int Type)
        {
            string retString = string.Empty;
            Category type = null;
            Category subCat = null;
            Category cat = null;
            type = db.Categories.Find(dbCloth.CategoryId);
            if (type != null)
            {
                subCat = db.Categories.Find(type.ParentId);
                if (subCat != null)
                    cat = db.Categories.Find(subCat.ParentId);
            }
            if (Type == 1)
                retString = (cat != null ? "Category : " + cat.Name + " \n " : "") + (subCat != null ? "SubCategory : " + subCat.Name + " \n " : "") + (type != null ? "Type : " + type.Name : "") + " \n ";
            else
            {
                retString = (cat != null ? "Category : " + cat.Name + " \n " : "") + (subCat != null ? "SubCategory : " + subCat.Name + " \n " : "") + (type != null ? "Type : " + type.Name : "") + " \n ";
                retString += (!string.IsNullOrEmpty(dbCloth.Color) ? "Color : " + dbCloth.Color : "");
                //if (dbCloth.FutureDeliveryDate.HasValue ? dbCloth.FutureDeliveryDate.Value != DateTime.MinValue : false)
                //    retString += "Future Product : Yes \n\n ";
                //else
                //    retString += "Future Product : No \n\n ";
                //var fitList = db.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                //var inseamList = db.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
                //List<int?> dbFitList = dbCloth.ClothesScales.Where(x => x.FitId.HasValue && x.IsOpenSize == false).Select(y => y.FitId).Distinct().ToList();
                //List<int?> dbInseamList = dbCloth.ClothesScales.Where(x => x.InseamId.HasValue && x.IsOpenSize == false).Select(y => y.InseamId).Distinct().ToList();
                //if (dbFitList.Count == 0)
                //    dbFitList.Add(null);
                //if (dbInseamList.Count == 0)
                //    dbInseamList.Add(null);
                //foreach (var fitid in dbFitList)
                //{
                //    foreach (var inseamid in dbInseamList)
                //    {
                //        var scaleList = dbCloth.ClothesScales;
                //        var prepackList = scaleList.Where(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == false);
                //        if (prepackList.Count() > 0)
                //        {
                //            foreach (var pack in prepackList)
                //            {
                //                string SizeQuant = string.Empty;
                //                var fit = fitList.FirstOrDefault(x => x.Id == fitid);
                //                var inseam = fitList.FirstOrDefault(x => x.Id == fitid);
                //                var openSize = scaleList.FirstOrDefault(x => x.FitId == fitid && x.InseamId == inseamid && x.IsOpenSize == true);
                //                if (fit != null)
                //                    SizeQuant = "Fit: " + fit.Value + " \n ";
                //                if (!string.IsNullOrEmpty(pack.Name))
                //                    SizeQuant += pack.Name.ToUpper().Replace("SCALE", "") + " : " + (pack.InvQty.HasValue ? pack.InvQty.Value : 0) + "  \n ";
                //                if (inseam != null)
                //                    SizeQuant += "Inseam: " + inseam.Value + " ";
                //                if (openSize != null)
                //                {
                //                    List<string> sizeStrings = new List<string>();
                //                    foreach (var size in openSize.ClothesScaleSizes)
                //                    {
                //                        sizeStrings.Add(size.Size.Name + " : " + (size.Quantity.HasValue ? size.Quantity.Value : 0));
                //                    }
                //                    if (sizeStrings.Count > 0)
                //                        SizeQuant += "Size: " + string.Join(" ,", sizeStrings);
                //                    SizeQuant += " \n ";
                //                }
                //                retString += SizeQuant;
                //            }
                //        }
                //    }
                //}
            }
            if (!string.IsNullOrEmpty(retString) && retString.Length > 999)
                retString = retString.Substring(0, 996) + "...";
            return retString;
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

        public JsonResult OrderCartCount()
        {
            string Number = SiteConfiguration.OrderCount();
            return Json(Number, JsonRequestBehavior.AllowGet);
            //return Number;
        }

    }

}
