using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Platini.DB;
using Newtonsoft.Json;

namespace Platini.Models
{
    public class SiteConfiguration
    {
        public static Entities db = new Entities();
        public static int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);
        public static List<CategoryClass> Categories()
        {
            db = new Entities();
            var categories = db.Categories.Where(i => i.IsActive == true && i.IsDelete == false).OrderBy(x => x.ParentId).ThenBy(x => x.SortOrder).ThenBy(x => x.CategoryId).ToList();
            List<CategoryClass> retList = new List<CategoryClass>().InjectFrom(categories);
            return retList;
        }

        public static List<CategoryClass> Types(int subcategoryid)
        {
            if (subcategoryid == 0) return new List<CategoryClass>();

            var types = db.Categories.Where(i => i.IsActive == true && i.IsDelete == false && i.ParentId == subcategoryid).OrderBy(i => i.SortOrder).ToList();
            var retList = types.Select(x => new CategoryClass() { CategoryId = x.CategoryId, CategoryLevel = x.CategoryLevel, Name = x.Name }).ToList();
            return retList;
        }

        public static List<PermissionClass> AllRolePermissions(int RoleId)
        {
            var perresult = db.RolesPermissions.Where(x => x.RoleId == RoleId).FirstOrDefault();
            List<PermissionClass> perList = new List<PermissionClass>();
            var query = db.Permissions.ToList();
            var result = db.RolesPermissions.Where(x => x.RoleId == RoleId).ToList();
            foreach (var item in result)
            {
                PermissionClass per = new PermissionClass();
                per.RoleId = item.RoleId;
                per.PermissionName = query.Where(x => x.PermissionId == item.PermissionId).SingleOrDefault().PermissionPage;
                per.CanView = item.CanView;
                per.CanEdit = item.CanEdit;
                per.CanOrder = item.CanOrder;
                perList.Add(per);
            }

            return perList;
        }

        public static Address Address(string userName, int roleId)
        {
            int accountId = db.Accounts.FirstOrDefault(x => x.UserName == userName.Trim() && x.RoleId == roleId).AccountId;
            var address = db.Addresses.Where(x => x.AccountId == accountId && x.AddressTypeId == (int)AddressTypeEnum.BillingAddress).FirstOrDefault();
            return address;           
        }

        public static int GetRoleId(string role)
        {
            int roleId = 0;
            if (role == RolesEnum.Admin.ToString())
            {
                roleId = (int)RolesEnum.Admin;
            }
            if (role == RolesEnum.Customer.ToString())
            {
                roleId = (int)RolesEnum.Customer;
            }
            if (role == RolesEnum.SalesPerson.ToString())
            {
                roleId = (int)RolesEnum.SalesPerson;
            }
            if (role == RolesEnum.SuperAdmin.ToString())
            {
                roleId = (int)RolesEnum.SuperAdmin;
            }
            if (role == RolesEnum.User.ToString())
            {
                roleId = (int)RolesEnum.User;
            }
            if (role == RolesEnum.Warehouse.ToString())
            {
                roleId = (int)RolesEnum.Warehouse;
            }

            return roleId;
        }

        public static string OrderNumber()
        {
            int OrderNumberStart = 100;
            int OrderNumberLength = 6;
            int OrderNumberIncrement = 1;
            var OrderNumberStartSetting = db.WebsiteSettings.Where(x => x.SettingKey == "OrderNumberStart").FirstOrDefault();
            if (OrderNumberStartSetting != null)
            {
                OrderNumberStart = Convert.ToInt32(OrderNumberStartSetting.SettingValue);
            }
            else
            {
                OrderNumberStartSetting = new WebsiteSetting();
                OrderNumberStartSetting.SettingKey = "OrderNumberStart";
                OrderNumberStartSetting.SettingValue = OrderNumberStart.ToString();
                db.WebsiteSettings.Add(OrderNumberStartSetting);
                db.SaveChanges();
            }
            var OrderNumberLengthSetting = db.WebsiteSettings.Where(x => x.SettingKey == "OrderNumberLength").FirstOrDefault();
            if (OrderNumberLengthSetting != null)
            {
                OrderNumberLength = Convert.ToInt32(OrderNumberLengthSetting.SettingValue);
            }
            else
            {
                OrderNumberLengthSetting = new WebsiteSetting();
                OrderNumberLengthSetting.SettingKey = "OrderNumberLength";
                OrderNumberLengthSetting.SettingValue = OrderNumberLength.ToString();
                db.WebsiteSettings.Add(OrderNumberLengthSetting);
                db.SaveChanges();
            }
            var OrderNumberIncrementSetting = db.WebsiteSettings.Where(x => x.SettingKey == "OrderNumberIncrement").FirstOrDefault();
            if (OrderNumberIncrementSetting != null)
            {
                OrderNumberIncrement = Convert.ToInt32(OrderNumberIncrementSetting.SettingValue);
            }
            else
            {
                OrderNumberIncrementSetting = new WebsiteSetting();
                OrderNumberIncrementSetting.SettingKey = "OrderNumberIncrement";
                OrderNumberIncrementSetting.SettingValue = OrderNumberIncrement.ToString();
                db.WebsiteSettings.Add(OrderNumberIncrementSetting);
                db.SaveChanges();
            }

            int OrderNumber = OrderNumberStart + OrderNumberIncrement;
            if (OrderNumberStartSetting != null)
            {
                OrderNumberStartSetting.SettingValue = OrderNumber.ToString();
                db.SaveChanges();
            }
            string changeNumber = OrderNumber.ToString().PadLeft(OrderNumberLength, '0');
            return changeNumber;
        }


        public static void Clear()
        {
            HttpContext.Current.Session["SiteMapping"] = null;
            HttpContext.Current.Session.Abandon();
            HttpContext.Current.Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", ""));
        }

        public static void AddToApp(HttpApplicationState appState, SelectedListValues newCustomer)
        {
            var List = GetApp(appState);

            // to create new list if object is null
            if (newCustomer == null)
            {
                newCustomer = new SelectedListValues() { Id = 0, Value = null };
            }
            
            if (List.FirstOrDefault(x => x.Value == newCustomer.Value) == null)
                List.Add(newCustomer);
            else
                List.FirstOrDefault(x => x.Value == newCustomer.Value).Id = newCustomer.Id;
            appState.Lock();
            appState["OnlineNow"] = List;
            appState.UnLock();
        }

        public static List<SelectedListValues> GetApp(HttpApplicationState appState)
        {

            if (appState["OnlineNow"] != null)
                return (List<SelectedListValues>)appState["OnlineNow"];
            else
                return new List<SelectedListValues>();
        }

        public static void RemoveFromApp(HttpApplicationState appState, SelectedListValues removeCustomer)
        {
            var List = GetApp(appState);
            if (List.FirstOrDefault(x => x.Value == removeCustomer.Value) != null)
            {
                var customer = List.FirstOrDefault(x => x.Value == removeCustomer.Value);
                List.Remove(customer);
                appState.Lock();
                appState["OnlineNow"] = List;
                appState.UnLock();
            }
        }

        public static bool LoadCookieCart()
        {
            bool ret = false;
            if (string.IsNullOrEmpty(SiteIdentity.UserId))
            {
                var ordCookie = HttpContext.Current.Request.Cookies["ordCookie"];
                if (ordCookie != null)
                {
                    try
                    {
                        var lastOrder = JsonConvert.DeserializeObject<Order>(ordCookie.Value);
                        ret = lastOrder.OrderId != Guid.Empty;
                    }
                    catch
                    {
                        ret = false;
                    }
                }
            }
            return ret;
        }

        public static string OrderCount()
        {
            Order lastOrder = null;
            int UserId = 0;
            int StatusId = 0;
            var status = db.OrderStatus.FirstOrDefault(x => x.Status.ToLower() == "pending");
            if (status != null)
                StatusId = status.OrderStatusId;
            int.TryParse(SiteIdentity.UserId, out UserId);
            bool isCookie = LoadCookieCart();
            if (isCookie)
                lastOrder = JsonConvert.DeserializeObject<Order>(HttpContext.Current.Request.Cookies["ordCookie"].Value);
            else
            {
                if (HttpContext.Current.Session["Order"] == null && HttpContext.Current.Session["EditingOrder"] == null && HttpContext.Current.Session["WasCleared"] == null)
                    lastOrder = db.Orders.Where(x => x.AccountId == UserId && x.StatusId == StatusId && x.IsDelete == false).ToList().OrderByDescending(x => x.DateCreated).FirstOrDefault();
                else if (HttpContext.Current.Session["Order"] != null)
                    lastOrder = db.Orders.Find((Guid)HttpContext.Current.Session["Order"]);
                else if (HttpContext.Current.Session["EditingOrder"] != null)
                    lastOrder = db.Orders.Find((Guid)HttpContext.Current.Session["EditingOrder"]);
            }
            if (lastOrder != null)
            {
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
                if (clothes.Count > 0)
                    return "(" + clothes.Count + ")";
            }
            return string.Empty;
        }

        public static string ModTest(string ccNum)
        {
            string retCCNum = string.Empty;
            if (ccNum.Length > 13)
            {
                int checkDigit = 0;
                int.TryParse(ccNum.Last().ToString(), out checkDigit);
                string num = ccNum;
                var baseArr = num.Substring(0, ccNum.Length - 1).Reverse().Select(x => int.Parse(x.ToString()));
                var oddArr = baseArr.Where((item, index) => index % 2 != 0);
                var evenArr = baseArr.Where((item, index) => index % 2 == 0).Select(x => x * 2).Select(x => x > 9 ? x - 9 : x);
                int Sum = oddArr.Sum() + evenArr.Sum();
                if ((checkDigit + Sum) % 10 == 0)
                    retCCNum = ccNum.Substring(ccNum.Length - 4).PadLeft(16, 'X');
            }
            return retCCNum;
        }

        public static string CompanyName(string userName, int roleId)
        {
            int accountId = db.Accounts.FirstOrDefault(x => x.UserName == userName.Trim() && x.RoleId == roleId).AccountId;
            var retCompany = db.Companies.FirstOrDefault(i => i.AccountId == accountId);

            if (retCompany != null && !string.IsNullOrEmpty(retCompany.Name))
                return retCompany.Name;
            else
                return string.Empty;
        }

        public static bool CanEdit
        {
            get
            {
                Fill();
                return _CanEdit;
            }
            set
            {
                Fill();
                _CanEdit = value;
                Bind();
            }
        }

        public static bool CanView
        {
            get
            {
                Fill();
                return _CanView;
            }
            set
            {
                Fill();
                _CanView = value;
                Bind();
            }
        }

        public static bool CanOrder
        {
            get
            {
                Fill();
                return _CanOrder;
            }
            set
            {
                Fill();
                _CanOrder = value;
                Bind();
            }
        }

        public static int MainID
        {
            get
            {
                Fill();
                return _MainID;
            }
            set
            {
                Fill();
                _MainID = value;
                Bind();
            }
        }

        public static int SubID
        {
            get
            {
                Fill();
                return _SubID;
            }
            set
            {
                Fill();
                _SubID = value;
                Bind();
            }
        }

        public static int CatID
        {
            get
            {
                Fill();
                return _CatID;
            }
            set
            {
                Fill();
                _CatID = value;
                Bind();
            }
        }

        public static string Mode
        {
            get
            {
                Fill();
                return _Mode;
            }
            set
            {
                Fill();
                _Mode = value;
                Bind();
            }
        }


        private static void Fill()
        {
            _CanView = true; _CanEdit = false; _CanOrder = false; _MainID = 0; _SubID = 0; _CatID = 0; _Mode = ModeEnum.Order.ToString();
            if (HttpContext.Current.Session["SiteMapping"] != null && (SiteMapping)HttpContext.Current.Session["SiteMapping"] != null)
            {
                SiteMapping siteMapping = (SiteMapping)HttpContext.Current.Session["SiteMapping"];
                _CanEdit = siteMapping.CanEdit;
                _CanView = siteMapping.CanView;
                _CanOrder = siteMapping.CanOrder;
                _MainID = siteMapping.MainID;
                _SubID = siteMapping.SubID;
                _CatID = siteMapping.CatID;
                _Mode = siteMapping.Mode;
            }
            else
            {
                Bind();
            }
        }

        private static void Bind()
        {
            SiteMapping siteMapping = null;
            if (HttpContext.Current.Session["SiteMapping"] != null && (SiteMapping)HttpContext.Current.Session["SiteMapping"] != null)
                siteMapping = (SiteMapping)HttpContext.Current.Session["SiteMapping"];
            else
                siteMapping = new SiteMapping();
            siteMapping.CanEdit = _CanEdit;
            siteMapping.CanView = _CanView;
            siteMapping.CanOrder = _CanOrder;
            siteMapping.MainID = _MainID;
            siteMapping.SubID = _SubID;
            siteMapping.CatID = _CatID;
            siteMapping.Mode = _Mode;
            HttpContext.Current.Session["SiteMapping"] = siteMapping;
        }

        private static bool _CanView = true;
        private static bool _CanOrder = false;
        private static bool _CanEdit = false;
        private static int _MainID = 0;
        private static int _SubID = 0;
        private static int _CatID = 0;
        private static string _Mode = ModeEnum.Order.ToString();

        private class SiteMapping
        {
            public bool CanView = true;
            public bool CanOrder = false;
            public bool CanEdit = false;
            public int MainID = 0;
            public int SubID = 0;
            public int CatID = 0;
            public string Mode = ModeEnum.Order.ToString();
        }
    }
}