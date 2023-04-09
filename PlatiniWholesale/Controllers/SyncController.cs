using Platini.DB;
using Platini.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Hosting;
using System.Web.Http;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace Platini.Controllers
{
    public class SyncController : ApiController
    {
        Entities dbcontext = new Entities();
        public DateTime InitialDate = new DateTime(2015, 6, 10, 0, 0, 0);

        [HttpPost]
        public GenericResponse Verify([FromBody]SyncLogin ret)
        {
            GenericResponse Response;
            if (!string.IsNullOrEmpty(ret.UserName) && !string.IsNullOrEmpty(ret.Password))
            {
                var verifyList = dbcontext.Accounts.Where(a => a.UserName == ret.UserName).ToList();
                if (verifyList != null)
                {
                    byte[] bytePassword = Encoding.ASCII.GetBytes(ret.Password);
                    var verifyUser = verifyList.Where(x => x.Password.SequenceEqual(bytePassword)).FirstOrDefault();
                    if (verifyUser != null)
                    {
                        if (verifyUser.IsActive.Value && !verifyUser.IsDelete.Value)
                            Response = new GenericResponse() { Status = true, UserId = verifyUser.AccountId, Message = "User verified" };
                        else if (verifyUser.IsDelete.Value)
                            Response = new GenericResponse() { Status = false, UserId = 0, Message = "Your account is deleted !!" };
                        else
                            Response = new GenericResponse() { Status = false, UserId = 0, Message = "Your account is currently deactivated !!" };
                    }
                    else
                        Response = new GenericResponse() { Status = false, UserId = 0, Message = "Please enter a valid user name and password." };
                }
                else
                    Response = new GenericResponse() { Status = false, UserId = 0, Message = "Please enter a valid user name and password." };
            }
            else
                Response = new GenericResponse() { Status = false, UserId = 0, Message = "Username & Password field should not be empty" };
            return Response;
        }

        [HttpPost]
        public GenericResponse Register([FromBody]SyncRegistration ret)
        {
            GenericResponse Response;
            if (!string.IsNullOrEmpty(ret.UserName) && !string.IsNullOrEmpty(ret.Password))
            {
                var verifyUser = dbcontext.Accounts.Where(a => (a.UserName == ret.UserName) && a.IsDelete == false).FirstOrDefault();
                if (verifyUser == null)
                {
                    byte[] password = Encoding.ASCII.GetBytes(ret.Password);
                    Account account = new Account();
                    account.UserName = ret.UserName;
                    account.Password = password;
                    account.FirstName = ret.FirstName;
                    account.LastName = ret.LastName;
                    account.Email = ret.Email;
                    account.RoleId = (int)RolesEnum.Customer;
                    account.IsActive = true;
                    account.IsDelete = false;
                    account.DateCreated = DateTime.UtcNow;
                    account.DateUpdated = DateTime.UtcNow;
                    dbcontext.Accounts.Add(account);
                    try
                    {
                        dbcontext.SaveChanges();

                        if (ret.CompanyName != null)
                        {
                            SaveUniqueCompany(ret.CompanyName, ret.City, account.AccountId);
                        }
                        else
                        {
                            SaveUniqueCompany(ret.FirstName + " " + ret.LastName, ret.City, account.AccountId);
                        }

                        if (!string.IsNullOrEmpty(ret.City))
                        {
                            Address dbAddress = new Address();
                            dbAddress.AccountId = account.AccountId;
                            dbAddress.City = ret.City;
                            dbAddress.Street = !string.IsNullOrEmpty(ret.Address) ? ret.Address : string.Empty;
                            dbAddress.AddressTypeId = (int)AddressTypeEnum.BillingAddress;
                            dbAddress.IsActive = true;
                            dbAddress.IsDelete = false;
                            dbAddress.DateCreated = DateTime.UtcNow;
                            dbAddress.DateUpdated = DateTime.UtcNow;
                            dbcontext.Addresses.Add(dbAddress);
                            dbcontext.SaveChanges();
                        }

                        if (!string.IsNullOrEmpty(ret.PhoneNumber))
                        {
                            Communication communication = new Communication();
                            communication.AccountId = account.AccountId;
                            communication.Phone = ret.PhoneNumber;
                            communication.IsActive = true;
                            communication.IsDelete = false;
                            communication.DateCreated = DateTime.UtcNow;
                            communication.DateUpdated = DateTime.UtcNow;
                            dbcontext.Communications.Add(communication);
                            dbcontext.SaveChanges();
                        }

                        //bool check = EmailManager.SendWelcomeEmail(ret.Email, !string.IsNullOrEmpty(ret.CompanyName) ? ret.CompanyName : ret.UserName);
                        //if (check)
                        //    Response = new GenericResponse() { Status = true, UserId = account.AccountId, Message = "Your account is now successfully registered.  Please check your email for confirmation and the next steps." };
                        //else
                        //    Response = new GenericResponse() { Status = true, UserId = account.AccountId, Message = "Your account is now successfully registered. But Error while sending email. So wait for Admin Approval" };

                        if (account.AccountId > 0)
                        {
                            Response = new GenericResponse() { Status = true, UserId = account.AccountId, Message = " Thank you for registering.  You can now login to the app." };
                        }
                        else
                        {
                            Response = new GenericResponse() { Status = false, UserId = 0, Message = "Error! while registering customer" };
                        }
                    }
                    catch (Exception ex)
                    {
                        Response = new GenericResponse() { Status = false, UserId = 0, Message = "Error!" + ex.Message };
                    }
                }
                else
                    Response = new GenericResponse() { Status = false, UserId = 0, Message = "Username in use already" };
            }
            else
                Response = new GenericResponse() { Status = false, UserId = 0, Message = "Data Invalid Or incomplete" };
            return Response;
        }

        [HttpPost]
        public GenericResponse ForgotPassword([FromBody]SyncUserName ret)
        {
            GenericResponse Response;
            var verifyUser = dbcontext.Accounts.Where(a => (a.UserName == ret.UserName) && a.IsDelete == false && a.IsActive == true).FirstOrDefault();
            if (verifyUser != null)
            {
                string email = verifyUser.Email;
                string pass = Encoding.ASCII.GetString(verifyUser.Password);
                bool check = EmailManager.SendForgotPasswordEmail(email, ret.UserName, pass);
                if (check)
                    Response = new GenericResponse() { Status = true, UserId = verifyUser.AccountId, Message = "Your password has been emailed to the address on file." };
                else
                    Response = new GenericResponse() { Status = false, UserId = verifyUser.AccountId, Message = "There is some problem, Please try later!" };
            }
            else
                Response = new GenericResponse() { Status = false, UserId = 0, Message = "We could not find the username in our system." };
            return Response;
        }

        [HttpPost]
        public List<SyncCategory> SyncCategories([FromBody]SyncUserId ret)
        {
            var searchList = dbcontext.Categories.Where(i => i.IsActive == true && i.IsDelete == false).OrderBy(i => i.ParentId).ThenBy(i => i.SortOrder).ToList();
            List<SyncCategory> categoryList = new List<SyncCategory>().InjectFrom(searchList);
            return categoryList;
        }

        [HttpPost]
        public List<SyncClothes> SyncClothes([FromBody]SyncUserId ret)
        {
            var searchList = dbcontext.Clothes.Where(i => i.IsActive == true).OrderBy(i => i.CategoryId).ThenBy(i => i.SortOrder).ToList();
            List<SyncClothes> clothesList = new List<SyncClothes>().InjectFrom(searchList);
            return clothesList;
        }

        public void SaveUniqueCompany(string companyName, string city, int accountId)
        {
            string cName = companyName;
            bool check = dbcontext.Companies.Where(x => x.Name == cName && x.AccountId != accountId).Any();
            if (check)
            {
                cName = companyName + "-" + city;
                check = dbcontext.Companies.Where(x => x.Name == cName && x.AccountId != accountId).Any();
                if (check)
                    cName = companyName + "-" + city + "-" + accountId;
            }
            var dbCompany = dbcontext.Companies.Where(x => x.AccountId == accountId).FirstOrDefault();
            if (dbCompany != null)
            {
                dbCompany.Name = cName;
                dbCompany.AccountId = accountId;
            }
            else
            {
                dbCompany = new Company();
                dbCompany.Name = cName;
                dbCompany.AccountId = accountId;
                dbCompany.IsActive = true;
                dbCompany.IsDelete = false;
                dbCompany.DateCreated = dbCompany.DateUpdated = DateTime.UtcNow;
                dbcontext.Companies.Add(dbCompany);
            }
            dbcontext.SaveChanges();
        }

        [HttpPost]
        public string InitialSync()
        {
            bool dates = false;
            var Wrapper = new Sync();
            Wrapper.ServerDate = InitialDate;
            try
            {
                Wrapper.Categories = new List<SyncCategory>().InjectFrom(dbcontext.Categories.Where(e => (dates ? e.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Clothes = new List<SyncClothes>().InjectFrom(dbcontext.Clothes.Where(c => c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                var tempClothes = Wrapper.Clothes.Where(r => r.isActive == true).Select(r => r.clothesId).ToArray();
                //Wrapper.ClothesImages = new List<SyncClothesImage>().InjectFrom(dbcontext.ClothesImages.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                //Wrapper.ClothesScales = new List<SyncClothesScale>().InjectFrom(dbcontext.ClothesScales.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                //Wrapper.ClothesScaleSizes = new List<SyncClothesScaleSize>().InjectFrom(dbcontext.ClothesScaleSizes.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.ClothesImages = new List<SyncClothesImage>().InjectFrom(dbcontext.ClothesImages.Where(c => tempClothes.Contains(c.ClothesId)).Select(s => s));
                Wrapper.ClothesScales = new List<SyncClothesScale>().InjectFrom(dbcontext.ClothesScales.Where(c => tempClothes.Contains(c.ClothesId)).Select(s => s));
                var tempClothesScales = Wrapper.ClothesScales.Select(r => r.clothesScaleId).ToArray();
                Wrapper.ClothesScaleSizes = new List<SyncClothesScaleSize>().InjectFrom(dbcontext.ClothesScaleSizes.Where(c => tempClothesScales.Contains(c.ClothesScaleId)).Select(s => s)); 

                Wrapper.RelatedClothes = new List<SyncRelatedClothes>().InjectFrom(dbcontext.RelatedClothes.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Fits = new List<SyncFit>().InjectFrom(dbcontext.Fits.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Inseams = new List<SyncInseam>().InjectFrom(dbcontext.Inseams.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.SizeGroups = new List<SyncSizeGroup>().InjectFrom(dbcontext.SizeGroups.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Sizes = new List<SyncSize>().InjectFrom(dbcontext.Sizes.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.OrderStatus = new List<SyncOrderStatus>().InjectFrom(dbcontext.OrderStatus.Where(c => (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Terms = new List<SyncTerm>().InjectFrom(dbcontext.Terms.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.ShipVias = new List<SyncShipVia>().InjectFrom(dbcontext.ShipVias.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.OrderTags = new List<SyncOrderTag>().InjectFrom(dbcontext.OrderTags.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.BackgroundPictures = new List<SyncBackgroundPicture>().InjectFrom(dbcontext.BackgroundPictures.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));

                var tempAccounts = dbcontext.Accounts.Select(r => r.AccountId).ToArray();
                Wrapper.Accounts = new List<SyncAccount>().InjectFrom(dbcontext.Accounts.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Companies = new List<SyncCompany>().InjectFrom(dbcontext.Companies.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Communications = new List<SyncCommunication>().InjectFrom(dbcontext.Communications.Where(c => tempAccounts.Contains(c.AccountId.Value) && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.AddressType = new List<SyncAddressType>().InjectFrom(dbcontext.AddressTypes.Where(c => (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Addresses = new List<SyncAddress>().InjectFrom(dbcontext.Addresses.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.CustomerOptionalInfos = new List<SyncCustomerOptionalInfo>().InjectFrom(dbcontext.CustomerOptionalInfoes.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));

                Wrapper.Permissions = new List<SyncPermission>().InjectFrom(dbcontext.Permissions.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.ExtraPermissions = new List<SyncExtraPermission>().InjectFrom(dbcontext.ExtraPermissions.Where(c => c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.Roles = new List<SyncRole>().InjectFrom(dbcontext.Roles.Where(c => c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                var tempRoles = Wrapper.Roles.Select(r => r.roleId).ToArray();
                Wrapper.RolePermissions = new List<SyncRolePermission>().InjectFrom(dbcontext.RolesPermissions.Where(s => tempRoles.Contains(s.RoleId)).Select(s => s));

                Wrapper.CustomerSalesPersons = new List<SyncCustomerSalesPerson>().InjectFrom(dbcontext.CustomerSalesPersons.Where(c => (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.CustomerUsers = new List<SyncCustomerUser>().InjectFrom(dbcontext.CustomerUsers.Where(c => (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.CustomerItemPrices = new List<SyncCustomerItemPrice>().InjectFrom(dbcontext.CustomerItemPrices.Where(c => (dates ? c.DateUpdated < Wrapper.ServerDate : true)));

                Wrapper.Orders = new List<SyncOrder>().InjectFrom(dbcontext.Orders.Where(c => c.IsDelete == false && (dates ? c.DateUpdated < Wrapper.ServerDate : true)));
                var tempOrders = Wrapper.Orders.Select(r => r.orderId).ToArray();
                Wrapper.OrderScales = new List<SyncOrderScale>().InjectFrom(dbcontext.OrderScales.Where(s => tempOrders.Contains(s.OrderId)).Select(s => s));
                Wrapper.OrderSizes = new List<SyncOrderSize>().InjectFrom(dbcontext.OrderSizes.Where(s => tempOrders.Contains(s.OrderId)).Select(s => s));
                Wrapper.OrderTags = new List<SyncOrderTag>().InjectFrom(dbcontext.OrderTags.Where(e => (dates ? e.DateUpdated < Wrapper.ServerDate : true)));
                Wrapper.UserId = 0;
                Wrapper.Next = false;
                Wrapper.Success = true;
                JsonSerializerSettings jSS = new JsonSerializerSettings();
                jSS.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff", Culture = CultureInfo.InvariantCulture, DateTimeStyles = DateTimeStyles.None });
                string Response = JsonConvert.SerializeObject(Wrapper, jSS);
                //string Response = JsonConvert.SerializeObject(Wrapper);
                System.IO.File.WriteAllText(HostingEnvironment.MapPath("~/Template/InitialSync.txt"), Response);
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                var fullErrorMessage = string.Join("; ", errorMessages);
                string response = Environment.NewLine + "InitialSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage) + Environment.NewLine + ex.StackTrace;
                System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), response);
            }
            catch (Exception ex)
            {
                Wrapper.Success = false;
                string Response = Environment.NewLine + ex.Message;
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                Response += Environment.NewLine + "InitialSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + ex.Message + Environment.NewLine + ex.StackTrace;
                System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), Response);
            }
            return Wrapper.Success.ToString();
        }

        [HttpPost]
        public Sync FullSync([FromBody]RequestWrapper wrapper)
        {
            bool dates = false;
            var Wrapper = new Sync();
            Wrapper.ServerDate = DateTime.UtcNow;
            if (wrapper != null && wrapper.UserId > 0)
            {
                try
                {
                    dates = wrapper.SyncDate != null && wrapper.SyncDate != DateTime.MinValue;
                    var account = dbcontext.Accounts.Where(x => x.AccountId == wrapper.UserId).FirstOrDefault();
                    if (account != null)
                    {
                        if (!dates)
                        {
                            wrapper.SyncDate = InitialDate;
                            dates = true;
                        }
                        var tempList = new List<int>();
                        if (account.RoleId == (int)RolesEnum.SuperAdmin || account.RoleId == (int)RolesEnum.Admin || account.RoleId == (int)RolesEnum.Warehouse)
                        {
                            tempList = dbcontext.Accounts.Select(r => r.AccountId).ToList();
                        }
                        else if (account.RoleId == (int)RolesEnum.SalesPerson)
                        {
                            tempList = dbcontext.CustomerSalesPersons.Where(c => c.SalesPersonId == wrapper.UserId).Select(r => r.AccountId).ToList();
                            tempList.Add(wrapper.UserId);
                        }
                        else if (account.RoleId == (int)RolesEnum.Customer)
                        {
                            tempList = dbcontext.CustomerUsers.Where(c => c.CustomerId == wrapper.UserId).Select(r => r.AccountId).ToList();
                            tempList.Add(wrapper.UserId);
                        }
                        else
                        {
                            tempList.Add(wrapper.UserId);
                        }
                        var tempAccounts = tempList.ToArray();
                        wrapper.Mode = (wrapper.Mode == null) ? 0 : wrapper.Mode;
                        wrapper.SubMode = (wrapper.SubMode == null) ? 0 : wrapper.SubMode;
                        if (wrapper.Mode == 0 || wrapper.Mode == 1)
                        {
                            if (wrapper.SubMode == 0 || wrapper.SubMode == 1)
                            {
                                Wrapper.Accounts = new List<SyncAccount>().InjectFrom(dbcontext.Accounts.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Companies = new List<SyncCompany>().InjectFrom(dbcontext.Companies.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Communications = new List<SyncCommunication>().InjectFrom(dbcontext.Communications.Where(c => tempAccounts.Contains(c.AccountId.Value) && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.AddressType = new List<SyncAddressType>().InjectFrom(dbcontext.AddressTypes.Where(c => (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Addresses = new List<SyncAddress>().InjectFrom(dbcontext.Addresses.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.CustomerOptionalInfos = new List<SyncCustomerOptionalInfo>().InjectFrom(dbcontext.CustomerOptionalInfoes.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.CustomerSalesPersons = new List<SyncCustomerSalesPerson>().InjectFrom(dbcontext.CustomerSalesPersons.Where(c => tempAccounts.Contains(c.AccountId) && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.BackgroundPictures = new List<SyncBackgroundPicture>().InjectFrom(dbcontext.BackgroundPictures.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                //if (wrapper.Mode == 0)
                                //    System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), Environment.NewLine + "FullSyncStart - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss tt") + " " + wrapper.DeviceInfo);
                            }
                            if (wrapper.SubMode == 0 || wrapper.SubMode == 2)
                            {
                                Wrapper.Permissions = new List<SyncPermission>().InjectFrom(dbcontext.Permissions.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.ExtraPermissions = new List<SyncExtraPermission>().InjectFrom(dbcontext.ExtraPermissions.Where(c => c.AccountId == wrapper.UserId && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Roles = new List<SyncRole>().InjectFrom(dbcontext.Roles.Where(c => c.RoleId == account.RoleId && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                var tempRoles = Wrapper.Roles.Select(r => r.roleId).ToArray();
                                Wrapper.RolePermissions = new List<SyncRolePermission>().InjectFrom(dbcontext.RolesPermissions.Where(s => tempRoles.Contains(s.RoleId)).Select(s => s));
                            }
                            if (wrapper.SubMode == 0 || wrapper.SubMode == 3)
                            {
                                Wrapper.CustomerSalesPersons = new List<SyncCustomerSalesPerson>().InjectFrom(dbcontext.CustomerSalesPersons.Where(c => c.SalesPersonId == wrapper.UserId && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.CustomerUsers = new List<SyncCustomerUser>().InjectFrom(dbcontext.CustomerUsers.Where(c => c.CustomerId == wrapper.UserId && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.CustomerItemPrices = new List<SyncCustomerItemPrice>().InjectFrom(dbcontext.CustomerItemPrices.Where(c => c.AccountId == wrapper.UserId && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                            }
                        }
                        if (wrapper.Mode == 0 || wrapper.Mode == 3)
                        {
                            if (wrapper.SubMode == 0 || wrapper.SubMode == 4)
                            {
                                Wrapper.Orders = new List<SyncOrder>().InjectFrom(dbcontext.Orders.Where(c => tempAccounts.Contains(c.AccountId) && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                var tempOrders = Wrapper.Orders.Select(r => r.orderId).ToArray();
                                Wrapper.OrderScales = new List<SyncOrderScale>().InjectFrom(dbcontext.OrderScales.Where(s => tempOrders.Contains(s.OrderId)).Select(s => s));
                                Wrapper.OrderSizes = new List<SyncOrderSize>().InjectFrom(dbcontext.OrderSizes.Where(s => tempOrders.Contains(s.OrderId)).Select(s => s));
                            }
                        }
                        if (wrapper.Mode == 0 || wrapper.Mode == 2)
                        {
                            if (wrapper.SubMode == 0 || wrapper.SubMode == 5)
                            {
                                Wrapper.Categories = new List<SyncCategory>().InjectFrom(dbcontext.Categories.Where(e => (dates ? e.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Clothes = new List<SyncClothes>().InjectFrom(dbcontext.Clothes.Where(c => c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                var tempClothes = Wrapper.Clothes.Where(r => r.isActive == true).Select(r => r.clothesId).ToArray();
                                //Wrapper.ClothesImages = new List<SyncClothesImage>().InjectFrom(dbcontext.ClothesImages.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                //Wrapper.ClothesScales = new List<SyncClothesScale>().InjectFrom(dbcontext.ClothesScales.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                //Wrapper.ClothesScaleSizes = new List<SyncClothesScaleSize>().InjectFrom(dbcontext.ClothesScaleSizes.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.ClothesImages = new List<SyncClothesImage>().InjectFrom(dbcontext.ClothesImages.Where(c => tempClothes.Contains(c.ClothesId)).Select(s => s));
                                Wrapper.ClothesScales = new List<SyncClothesScale>().InjectFrom(dbcontext.ClothesScales.Where(c => tempClothes.Contains(c.ClothesId)).Select(s => s));
                                var tempClothesScales = Wrapper.ClothesScales.Select(r => r.clothesScaleId).ToArray();
                                Wrapper.ClothesScaleSizes = new List<SyncClothesScaleSize>().InjectFrom(dbcontext.ClothesScaleSizes.Where(c => tempClothesScales.Contains(c.ClothesScaleId)).Select(s => s));
                            }
                            if (wrapper.SubMode == 0 || wrapper.SubMode == 6)
                            {
                                Wrapper.RelatedClothes = new List<SyncRelatedClothes>().InjectFrom(dbcontext.RelatedClothes.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Fits = new List<SyncFit>().InjectFrom(dbcontext.Fits.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Inseams = new List<SyncInseam>().InjectFrom(dbcontext.Inseams.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.SizeGroups = new List<SyncSizeGroup>().InjectFrom(dbcontext.SizeGroups.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.Sizes = new List<SyncSize>().InjectFrom(dbcontext.Sizes.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                                Wrapper.OrderStatus = new List<SyncOrderStatus>().InjectFrom(dbcontext.OrderStatus.Where(c => (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                            }
                        }
                        if (wrapper.SubMode == 0 || wrapper.SubMode == 7)
                        {
                            Wrapper.Terms = new List<SyncTerm>().InjectFrom(dbcontext.Terms.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                            Wrapper.ShipVias = new List<SyncShipVia>().InjectFrom(dbcontext.ShipVias.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                            //Wrapper.BackgroundPictures = new List<SyncBackgroundPicture>().InjectFrom(dbcontext.BackgroundPictures.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                            Wrapper.OrderTags = new List<SyncOrderTag>().InjectFrom(dbcontext.OrderTags.Where(c => c.IsActive == true && c.IsDelete == false && (dates ? c.DateUpdated >= wrapper.SyncDate : true)));
                            Wrapper.TablesToDelete = new List<SyncTableToDelete>().InjectFrom(dbcontext.TableToDeletes.Where(r => (dates ? r.DateUpdated >= wrapper.SyncDate : true)));
                            Wrapper.Next = false;
                            //if (wrapper.Mode == 0)
                            //     System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), Environment.NewLine + "FullSyncEnd - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss tt") + " " + wrapper.DeviceInfo);
                        }
                        else
                        {
                            Wrapper.Next = true;
                        }
                        Wrapper.Success = true;
                        Wrapper.UserId = wrapper.UserId;
                        string Response = JsonConvert.SerializeObject(Wrapper);
                        System.IO.File.WriteAllText(HostingEnvironment.MapPath("~/Template/FullSync.txt"), Response);
                    }
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                    var fullErrorMessage = string.Join("; ", errorMessages);
                    string response = Environment.NewLine + "FullSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage) + Environment.NewLine + ex.StackTrace;
                    System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), response);
                }
                catch (Exception ex)
                {
                    Wrapper.Success = false;
                    string Response = Environment.NewLine + ex.Message;
                    while (ex.InnerException != null)
                        ex = ex.InnerException;
                    Response += Environment.NewLine + "FullSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + ex.Message + Environment.NewLine + ex.StackTrace;
                    System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), Response);
                }
            }            
            return Wrapper;
        }

        [HttpPost]
        public ResponseWrapper iPadSync([FromBody] Sync Wrapper)
        {
            var response = new ResponseWrapper();
            if (Wrapper != null)
            {
                if (Wrapper.UserId > 0)
                {
                    var temp = dbcontext.Accounts.Where(x => x.AccountId == Wrapper.UserId).FirstOrDefault();
                    if (temp != null)
                    {
                        DateTime DateCreated;
                        int UniqueId;
                        Guid UniqueGuid;
                        try
                        {
                            #region AccountSync
                            if (Wrapper.Accounts != null)
                            {
                                foreach (var account in Wrapper.Accounts)
                                {
                                    if (account.isLocal.HasValue && account.isLocal == true)
                                    {
                                        int OldAccountId = account.accountId;
                                        var newAccount = new Account();
                                        account.accountId = 0;
                                        newAccount.InjectClass(account);
                                        newAccount.IsLocal = false;
                                        newAccount.DateUpdated = DateTime.UtcNow;
                                        dbcontext.Accounts.Add(newAccount);
                                        dbcontext.SaveChanges();
                                        int newAccountId = newAccount.AccountId;
                                        account.accountId = newAccountId;
                                        var CompanyDB = dbcontext.Companies.Where(x => x.AccountId == newAccount.AccountId).FirstOrDefault();
                                        string CompanyName = CompanyDB != null ? CompanyDB.Name : string.Empty;
                                        if (Wrapper.Companies != null)
                                        {
                                            var Companies = Wrapper.Companies.ToList();
                                            Companies.ForEach(x => { if (x.accountId == OldAccountId) { x.accountId = newAccountId; x.companyId = 0; } });
                                            Wrapper.Companies = new List<SyncCompany>().InjectFrom(Companies);
                                        }
                                        if (Wrapper.Addresses != null)
                                        {
                                            var Addresses = Wrapper.Addresses.ToList();
                                            Addresses.ForEach(x => { if (x.accountId == OldAccountId) { x.accountId = newAccountId; x.addressId = 0; } });
                                            Wrapper.Addresses = new List<SyncAddress>().InjectFrom(Addresses);
                                        }
                                        if (Wrapper.Communications != null)
                                        {
                                            var Communications = Wrapper.Communications.ToList();
                                            Communications.ForEach(x => { if (x.accountId == OldAccountId) { x.accountId = newAccountId; x.communicationId = 0; } });
                                            Wrapper.Communications = new List<SyncCommunication>().InjectFrom(Communications);
                                        }
                                        if (Wrapper.CustomerOptionalInfos != null)
                                        {
                                            var CustomerOptionalInfos = Wrapper.CustomerOptionalInfos.ToList();
                                            CustomerOptionalInfos.ForEach(x => { if (x.accountId == OldAccountId) { x.accountId = newAccountId; x.customerOptionalInfoId = 0; } });
                                            Wrapper.CustomerOptionalInfos = new List<SyncCustomerOptionalInfo>().InjectFrom(CustomerOptionalInfos);
                                        }
                                        if (Wrapper.CustomerUsers != null)
                                        {
                                            var CustomerUsers = Wrapper.CustomerUsers.ToList();
                                            CustomerUsers.ForEach(x => { if (x.accountId == OldAccountId) { x.accountId = newAccountId; x.customerUserId = 0; } });
                                            Wrapper.CustomerUsers = new List<SyncCustomerUser>().InjectFrom(CustomerUsers);
                                        }
                                        if (Wrapper.CustomerSalesPersons != null)
                                        {
                                            var CustomerSalesPersons = Wrapper.CustomerSalesPersons.ToList();
                                            CustomerSalesPersons.ForEach(x => { if (x.accountId == OldAccountId) { x.accountId = newAccountId; x.customerSalesPersonId = 0; } });
                                            Wrapper.CustomerSalesPersons = new List<SyncCustomerSalesPerson>().InjectFrom(CustomerSalesPersons);
                                        }
                                        if (Wrapper.Orders != null)
                                        {
                                            var Orders = Wrapper.Orders.ToList();
                                            Orders.ForEach(x => { if (x.accountId == OldAccountId) { x.accountId = newAccountId; } });
                                            Wrapper.Orders = new List<SyncOrder>().InjectFrom(Orders);
                                        }
                                        if (newAccount.IsActive == true && QuickBookStrings.UseQuickBook())
                                            AddCustomerToQuickBook(newAccount.AccountId, CompanyName);
                                    }
                                    else
                                    {
                                        UniqueId = 0;
                                        DateCreated = DateTime.MinValue;
                                        if (dbcontext.Accounts.Any(r => r.AccountId == account.accountId))
                                        {
                                            var existAccount = dbcontext.Accounts.Where(r => r.AccountId == account.accountId).FirstOrDefault();
                                            if (existAccount.DateUpdated <= account.dateUpdated)
                                            {
                                                UniqueId = existAccount.AccountId;
                                                DateCreated = existAccount.DateCreated.Value;
                                                existAccount.InjectClass(account);
                                                existAccount.AccountId = UniqueId;
                                                existAccount.DateCreated = DateCreated;
                                                existAccount.DateUpdated = DateTime.UtcNow;
                                            }
                                        }
                                        else
                                        {
                                            var newAccount = new Account();
                                            newAccount.InjectClass(account);
                                            newAccount.DateUpdated = DateTime.UtcNow;
                                            dbcontext.Accounts.Add(newAccount);
                                        }
                                    }
                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region CompanySync
                            if (Wrapper.Companies != null)
                            {
                                foreach (var company in Wrapper.Companies)
                                {
                                    UniqueId = 0;
                                    DateCreated = DateTime.MinValue;
                                    if (dbcontext.Companies.Any(r => r.CompanyId == company.companyId))
                                    {
                                        var existCompany = dbcontext.Companies.Where(r => r.CompanyId == company.companyId).FirstOrDefault();
                                        if (existCompany.DateUpdated <= company.dateUpdated)
                                        {
                                            UniqueId = existCompany.CompanyId;
                                            DateCreated = existCompany.DateCreated.Value;
                                            existCompany.InjectClass(company);
                                            existCompany.CompanyId = UniqueId;
                                            existCompany.DateCreated = DateCreated;
                                            existCompany.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newCompany = new Company();
                                        newCompany.InjectClass(company);
                                        newCompany.DateUpdated = DateTime.UtcNow;
                                        dbcontext.Companies.Add(newCompany);
                                    }

                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region AddressSync
                            if (Wrapper.Addresses != null)
                            {
                                foreach (var address in Wrapper.Addresses)
                                {
                                    UniqueId = 0;
                                    DateCreated = DateTime.MinValue;
                                    if (dbcontext.Addresses.Any(r => r.AddressId == address.addressId))
                                    {
                                        var existAddress = dbcontext.Addresses.Where(r => r.AddressId == address.addressId).FirstOrDefault();
                                        if (existAddress.DateUpdated <= address.dateUpdated)
                                        {
                                            UniqueId = existAddress.AddressId;
                                            DateCreated = existAddress.DateCreated.Value;
                                            existAddress.InjectClass(address);
                                            existAddress.AddressId = UniqueId;
                                            existAddress.DateCreated = DateCreated;
                                            existAddress.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newAddress = new Address();
                                        newAddress.InjectClass(address);
                                        newAddress.DateUpdated = DateTime.UtcNow;
                                        dbcontext.Addresses.Add(newAddress);
                                    }

                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region CommunicationSync
                            if (Wrapper.Communications != null)
                            {
                                foreach (var communication in Wrapper.Communications)
                                {
                                    UniqueId = 0;
                                    DateCreated = DateTime.MinValue;
                                    if (dbcontext.Communications.Any(r => r.CommunicationId == communication.communicationId))
                                    {
                                        var existCommunication = dbcontext.Communications.Where(r => r.CommunicationId == communication.communicationId).FirstOrDefault();
                                        if (existCommunication.DateUpdated <= communication.dateUpdated)
                                        {
                                            UniqueId = existCommunication.CommunicationId;
                                            DateCreated = existCommunication.DateCreated.Value;
                                            existCommunication.InjectClass(communication);
                                            existCommunication.CommunicationId = UniqueId;
                                            existCommunication.DateCreated = DateCreated;
                                            existCommunication.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newCommunication = new Communication();
                                        newCommunication.InjectClass(communication);
                                        newCommunication.DateUpdated = DateTime.UtcNow;
                                        dbcontext.Communications.Add(newCommunication);
                                    }

                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region CustomerOptionalInfoSync
                            if (Wrapper.CustomerOptionalInfos != null)
                            {
                                foreach (var customeroptioninfo in Wrapper.CustomerOptionalInfos)
                                {
                                    UniqueId = 0;
                                    DateCreated = DateTime.MinValue;
                                    if (customeroptioninfo.shipViaId == 0)
                                        customeroptioninfo.shipViaId = null;
                                    if (customeroptioninfo.termId == 0)
                                        customeroptioninfo.termId = null;
                                    if (dbcontext.CustomerOptionalInfoes.Any(r => r.CustomerOptionalInfoId == customeroptioninfo.customerOptionalInfoId))
                                    {
                                        var existCustomerOptionalInfo = dbcontext.CustomerOptionalInfoes.Where(r => r.CustomerOptionalInfoId == customeroptioninfo.customerOptionalInfoId).FirstOrDefault();
                                        if (existCustomerOptionalInfo.DateUpdated <= customeroptioninfo.dateUpdated)
                                        {
                                            UniqueId = existCustomerOptionalInfo.CustomerOptionalInfoId;
                                            DateCreated = existCustomerOptionalInfo.DateCreated.Value;
                                            existCustomerOptionalInfo.InjectClass(customeroptioninfo);
                                            existCustomerOptionalInfo.CustomerOptionalInfoId = UniqueId;
                                            existCustomerOptionalInfo.DateCreated = DateCreated;
                                            existCustomerOptionalInfo.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newCustomerOptionalInfo = new CustomerOptionalInfo();
                                        newCustomerOptionalInfo.InjectClass(customeroptioninfo);
                                        newCustomerOptionalInfo.DateUpdated = DateTime.UtcNow;
                                        dbcontext.CustomerOptionalInfoes.Add(newCustomerOptionalInfo);
                                    }

                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region QuickBookSync
                            if (Wrapper.Accounts != null)
                            {
                                foreach (var account in Wrapper.Accounts)
                                {
                                    if (account.isLocal.HasValue && account.isLocal == true)
                                    { }
                                    else
                                    {
                                        var CompanyDB = dbcontext.Companies.Where(x => x.AccountId == account.accountId).FirstOrDefault();
                                        string CompanyName = CompanyDB != null ? CompanyDB.Name : string.Empty;
                                        var existAccount = dbcontext.Accounts.Where(r => r.AccountId == account.accountId).FirstOrDefault();
                                        if (existAccount.IsActive == true && QuickBookStrings.UseQuickBook())
                                            AddCustomerToQuickBook(existAccount.AccountId, CompanyName);
                                    }
                                }
                            }
                            #endregion

                            #region CustomerUserSync
                            if (Wrapper.CustomerUsers != null)
                            {
                                foreach (var customeruser in Wrapper.CustomerUsers)
                                {
                                    UniqueId = 0;
                                    DateCreated = DateTime.MinValue;
                                    if (dbcontext.CustomerUsers.Any(r => r.CustomerUserId == customeruser.customerUserId))
                                    {
                                        var existCustomerUser = dbcontext.CustomerUsers.Where(r => r.CustomerUserId == customeruser.customerUserId).FirstOrDefault();
                                        if (existCustomerUser.DateUpdated <= customeruser.dateUpdated)
                                        {
                                            UniqueId = existCustomerUser.CustomerUserId;
                                            DateCreated = existCustomerUser.DateCreated.Value;
                                            existCustomerUser.InjectClass(customeruser);
                                            existCustomerUser.CustomerUserId = UniqueId;
                                            existCustomerUser.DateCreated = DateCreated;
                                            existCustomerUser.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newCustomerUser = new CustomerUser();
                                        newCustomerUser.InjectClass(customeruser);
                                        newCustomerUser.DateUpdated = DateTime.UtcNow;
                                        dbcontext.CustomerUsers.Add(newCustomerUser);
                                    }

                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region CustomerSalesPersonSync
                            if (Wrapper.CustomerSalesPersons != null)
                            {
                                foreach (var customersalesperson in Wrapper.CustomerSalesPersons)
                                {
                                    UniqueId = 0;
                                    DateCreated = DateTime.MinValue;
                                    if (dbcontext.CustomerSalesPersons.Any(r => r.CustomerSalesPersonId == customersalesperson.customerSalesPersonId))
                                    {
                                        var existCustomerSalesPerson = dbcontext.CustomerSalesPersons.Where(r => r.CustomerSalesPersonId == customersalesperson.customerSalesPersonId).FirstOrDefault();
                                        if (existCustomerSalesPerson.DateUpdated <= customersalesperson.dateUpdated)
                                        {
                                            UniqueId = existCustomerSalesPerson.CustomerSalesPersonId;
                                            DateCreated = existCustomerSalesPerson.DateCreated.Value;
                                            existCustomerSalesPerson.InjectClass(customersalesperson);
                                            existCustomerSalesPerson.CustomerSalesPersonId = UniqueId;
                                            existCustomerSalesPerson.DateCreated = DateCreated;
                                            existCustomerSalesPerson.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newCustomerSalesPerson = new CustomerSalesPerson();
                                        newCustomerSalesPerson.InjectClass(customersalesperson);
                                        newCustomerSalesPerson.DateUpdated = DateTime.UtcNow;
                                        dbcontext.CustomerSalesPersons.Add(newCustomerSalesPerson);
                                    }
                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region ClothesScaleSync
                            if (Wrapper.ClothesScales != null)
                            {
                                foreach (var clothesScale in Wrapper.ClothesScales)
                                {
                                    if (clothesScale.fitId == 0)
                                        clothesScale.fitId = null;
                                    if (clothesScale.inseamId == 0)
                                        clothesScale.inseamId = null;
                                    if (clothesScale.isLocal.HasValue && clothesScale.isLocal == true)
                                    {
                                        int clothesId = clothesScale.clothesId;
                                        var existclothes = dbcontext.Clothes.Where(r => r.ClothesId == clothesId).FirstOrDefault();
                                        if (existclothes != null && existclothes.DateUpdated <= clothesScale.dateUpdated)
                                        {
                                            existclothes.DateUpdated = DateTime.UtcNow;
                                        }
                                        int OldClothesScaleId = clothesScale.clothesScaleId;
                                        var newClothesScale = new ClothesScale();
                                        clothesScale.clothesScaleId = 0;
                                        newClothesScale.InjectClass(clothesScale);
                                        newClothesScale.IsLocal = false;
                                        newClothesScale.DateUpdated = DateTime.UtcNow;
                                        dbcontext.ClothesScales.Add(newClothesScale);
                                        dbcontext.SaveChanges();
                                        int newClothesScaleId = newClothesScale.ClothesScaleId;
                                        clothesScale.clothesScaleId = newClothesScaleId;
                                        if (Wrapper.ClothesScaleSizes != null)
                                        {
                                            var ClothesScaleSizes = Wrapper.ClothesScaleSizes.ToList();
                                            ClothesScaleSizes.ForEach(x => { if (x.clothesScaleId == OldClothesScaleId) { x.clothesScaleId = newClothesScaleId; x.clothesScaleSizeId = 0; } });
                                            Wrapper.ClothesScaleSizes = new List<SyncClothesScaleSize>().InjectFrom(ClothesScaleSizes);
                                        }
                                    }
                                    else
                                    {
                                        UniqueId = 0;
                                        DateCreated = DateTime.MinValue;
                                        if (dbcontext.ClothesScales.Any(r => r.ClothesScaleId == clothesScale.clothesScaleId))
                                        {
                                            var existclothesScale = dbcontext.ClothesScales.Where(r => r.ClothesScaleId == clothesScale.clothesScaleId).FirstOrDefault();
                                            if (existclothesScale.DateUpdated <= clothesScale.dateUpdated)
                                            {
                                                UniqueId = existclothesScale.ClothesScaleId;
                                                DateCreated = existclothesScale.DateCreated.Value;
                                                existclothesScale.InjectClass(clothesScale);
                                                existclothesScale.ClothesScaleId = UniqueId;
                                                existclothesScale.DateCreated = DateCreated;
                                                existclothesScale.DateUpdated = DateTime.UtcNow;
                                            }
                                        }
                                        else
                                        {
                                            var newClothesScale = new ClothesScale();
                                            newClothesScale.InjectClass(clothesScale);
                                            newClothesScale.DateUpdated = DateTime.UtcNow;
                                            dbcontext.ClothesScales.Add(newClothesScale);
                                        }
                                    }
                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region ClothesScaleSizeSync
                            if (Wrapper.ClothesScaleSizes != null)
                            {
                                foreach (var clothesScaleSize in Wrapper.ClothesScaleSizes)
                                {                                    
                                    if (clothesScaleSize.isLocal.HasValue && clothesScaleSize.isLocal == true)
                                    {
                                        int clothesScaleId = clothesScaleSize.clothesScaleId;
                                        var existclothesScale = dbcontext.ClothesScales.Where(r => r.ClothesScaleId == clothesScaleId).FirstOrDefault();
                                        if (existclothesScale != null)
                                        {
                                            var existclothes = dbcontext.Clothes.Where(r => r.ClothesId == existclothesScale.ClothesId).FirstOrDefault();
                                            if (existclothes != null && existclothes.DateUpdated <= clothesScaleSize.dateUpdated)
                                            {
                                                existclothes.DateUpdated = DateTime.UtcNow;
                                            }
                                        }
                                        var newClothesScaleSize = new ClothesScaleSize();
                                        newClothesScaleSize.ClothesScaleSizeId = 0;
                                        newClothesScaleSize.InjectClass(clothesScaleSize);
                                        newClothesScaleSize.IsLocal = false;
                                        newClothesScaleSize.DateUpdated = DateTime.UtcNow;
                                        dbcontext.ClothesScaleSizes.Add(newClothesScaleSize);
                                    }
                                    else
                                    {
                                        UniqueId = 0;
                                        DateCreated = DateTime.MinValue;
                                        if (dbcontext.ClothesScaleSizes.Any(r => r.ClothesScaleSizeId == clothesScaleSize.clothesScaleSizeId))
                                        {
                                            var existclothesScaleSize = dbcontext.ClothesScaleSizes.Where(r => r.ClothesScaleSizeId == clothesScaleSize.clothesScaleSizeId).FirstOrDefault();
                                            if (existclothesScaleSize.DateUpdated <= clothesScaleSize.dateUpdated)
                                            {
                                                UniqueId = existclothesScaleSize.ClothesScaleSizeId;
                                                DateCreated = existclothesScaleSize.DateCreated.Value;
                                                existclothesScaleSize.InjectClass(clothesScaleSize);
                                                existclothesScaleSize.ClothesScaleSizeId = UniqueId;
                                                existclothesScaleSize.DateCreated = DateCreated;
                                                existclothesScaleSize.DateUpdated = DateTime.UtcNow;
                                            }
                                        }
                                        else
                                        {
                                            var newClothesScaleSize = new ClothesScaleSize();
                                            newClothesScaleSize.InjectClass(clothesScaleSize);
                                            newClothesScaleSize.DateUpdated = DateTime.UtcNow;
                                            dbcontext.ClothesScaleSizes.Add(newClothesScaleSize);
                                        }
                                    }
                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region OrderSync
                            if (Wrapper.Orders != null)
                            {
                                foreach (var order in Wrapper.Orders)
                                {
                                    UniqueGuid = Guid.Empty;
                                    DateCreated = DateTime.MinValue;
                                    if (order.employeeId == 0)
                                        order.employeeId = null;
                                    if (order.addressId == 0)
                                        order.addressId = null;
                                    if (order.communicationId == 0)
                                        order.communicationId = null;
                                    if (order.shipViaId == 0)
                                        order.shipViaId = null;
                                    if (order.termId == 0)
                                        order.termId = null;
                                    if (order.trackId == 0)
                                        order.trackId = null;
                                    if (order.tagId == 0)
                                        order.tagId = null;
                                    if (order.parentOrderId == Guid.Empty)
                                        order.parentOrderId = null;
                                    if (dbcontext.Orders.Any(r => r.OrderId == order.orderId))
                                    {
                                        var existOrder = dbcontext.Orders.Where(r => r.OrderId == order.orderId).FirstOrDefault();
                                        if (existOrder.DateUpdated <= order.dateUpdated)
                                        {
                                            UniqueGuid = existOrder.OrderId;
                                            DateCreated = existOrder.DateCreated.Value;
                                            existOrder.InjectClass(order);
                                            existOrder.OrderId = UniqueGuid;
                                            if (string.IsNullOrEmpty(existOrder.OrderNumber))
                                                existOrder.OrderNumber = SiteConfiguration.OrderNumber();
                                            existOrder.DateCreated = DateCreated;
                                            existOrder.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newOrder = new Order();
                                        newOrder.InjectClass(order);
                                        newOrder.OrderNumber = SiteConfiguration.OrderNumber();
                                        newOrder.DateUpdated = DateTime.UtcNow;
                                        dbcontext.Orders.Add(newOrder);
                                    }

                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region OrderScaleSync
                            if (Wrapper.OrderScales != null)
                            {
                                foreach (var orderScale in Wrapper.OrderScales)
                                {
                                    UniqueGuid = Guid.Empty;
                                    DateCreated = DateTime.MinValue;
                                    if (dbcontext.OrderScales.Any(r => r.OrderScaleId == orderScale.orderScaleId))
                                    {
                                        var existOrderScale = dbcontext.OrderScales.Where(r => r.OrderScaleId == orderScale.orderScaleId).FirstOrDefault();
                                        if (existOrderScale.DateUpdated <= orderScale.dateUpdated)
                                        {
                                            UniqueGuid = existOrderScale.OrderScaleId;
                                            DateCreated = existOrderScale.DateCreated.Value;
                                            existOrderScale.InjectClass(orderScale);
                                            existOrderScale.OrderScaleId = UniqueGuid;
                                            existOrderScale.DateCreated = DateCreated;
                                            existOrderScale.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newOrderScale = new OrderScale();
                                        newOrderScale.InjectClass(orderScale);
                                        newOrderScale.DateUpdated = DateTime.UtcNow;
                                        dbcontext.OrderScales.Add(newOrderScale);
                                    }

                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region OrderSizeSync
                            if (Wrapper.OrderSizes != null)
                            {
                                foreach (var orderSize in Wrapper.OrderSizes)
                                {
                                    UniqueGuid = Guid.Empty;
                                    DateCreated = DateTime.MinValue;
                                    if (dbcontext.OrderSizes.Any(r => r.OrderSizeId == orderSize.orderSizeId))
                                    {
                                        var existOrderSize = dbcontext.OrderSizes.Where(r => r.OrderSizeId == orderSize.orderSizeId).FirstOrDefault();
                                        if (existOrderSize.DateUpdated <= orderSize.dateUpdated)
                                        {
                                            UniqueGuid = existOrderSize.OrderSizeId;
                                            DateCreated = existOrderSize.DateCreated.Value;
                                            existOrderSize.InjectClass(orderSize);
                                            existOrderSize.OrderSizeId = UniqueGuid;
                                            existOrderSize.DateCreated = DateCreated;
                                            existOrderSize.DateUpdated = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        var newOrderSize = new OrderSize();
                                        newOrderSize.InjectClass(orderSize);
                                        newOrderSize.DateUpdated = DateTime.UtcNow;
                                        dbcontext.OrderSizes.Add(newOrderSize);
                                    }
                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            #region TablesToDelete
                            if (Wrapper.TablesToDelete != null)
                            {
                                foreach (var tabletodelete in Wrapper.TablesToDelete.OrderBy(i => i.dateUpdated.Value.Ticks))
                                {
                                    if (!string.IsNullOrEmpty(tabletodelete.tableName) && !string.IsNullOrEmpty(tabletodelete.tableKey))
                                    {
                                        //tabletodelete.tableValue = tabletodelete.tableValue.ToUpper(); 
                                        string strQuery = string.Format("Select DateUpdated from [{0}] where {1}='{2}'", tabletodelete.tableName, tabletodelete.tableKey, tabletodelete.tableValue);
                                        DateTime DateServer = dbcontext.Database.SqlQuery<DateTime>(strQuery).FirstOrDefault();
                                        if (DateServer != null && DateServer > DateTime.MinValue && DateServer <= tabletodelete.dateUpdated.Value)
                                        {
                                            if (tabletodelete.justOverride.HasValue && tabletodelete.justOverride.Value == true)
                                            {
                                                string sqlCommand = string.Format("Update [{0}] set DateUpdated=UTC_TIMESTAMP() where {1}='{2}'", tabletodelete.tableName, tabletodelete.tableKey, tabletodelete.tableValue);
                                                int checkSql = dbcontext.Database.ExecuteSqlCommand(sqlCommand);
                                            }
                                            else
                                            {
                                                string sqlCommand = string.Format("Delete from [{0}] where {1}='{2}'", tabletodelete.tableName, tabletodelete.tableKey, tabletodelete.tableValue);
                                                int checkSql = dbcontext.Database.ExecuteSqlCommand(sqlCommand);
                                                if (checkSql > 0)
                                                {
                                                    if (dbcontext.TableToDeletes.Any(r => r.TableName == tabletodelete.tableName && r.TableKey == tabletodelete.tableKey && r.TableValue == tabletodelete.tableValue))
                                                    {
                                                        var existTableToDelete = dbcontext.TableToDeletes.Where(r => r.TableName == tabletodelete.tableName && r.TableKey == tabletodelete.tableKey && r.TableValue == tabletodelete.tableValue).FirstOrDefault();
                                                        if (existTableToDelete.DateUpdated <= tabletodelete.dateUpdated)
                                                        {
                                                            UniqueGuid = existTableToDelete.DeleteId;
                                                            DateCreated = tabletodelete.dateCreated.Value;
                                                            existTableToDelete.InjectClass(tabletodelete);
                                                            existTableToDelete.DeleteId = UniqueGuid;
                                                            existTableToDelete.DateCreated = DateCreated;
                                                            existTableToDelete.DateUpdated = DateTime.UtcNow;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var newTableToDelete = new TableToDelete();
                                                        newTableToDelete.InjectClass(tabletodelete);
                                                        newTableToDelete.DateUpdated = DateTime.UtcNow;
                                                        dbcontext.TableToDeletes.Add(newTableToDelete);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                dbcontext.SaveChanges();
                            }
                            #endregion

                            response.Status = true;
                            response.NeedToSync = true;
                            response.Response = "Success";
                        }
                        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                        {
                            var errorMessages = ex.EntityValidationErrors
                                    .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.ErrorMessage);

                            var fullErrorMessage = string.Join("; ", errorMessages);
                            response.Response = Environment.NewLine + "iPadSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage) + Environment.NewLine + ex.StackTrace;
                            System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), response.Response);
                        }
                        catch (Exception ex)
                        {
                            response.Status = false;
                            response.Response = Environment.NewLine + ex.Message;
                            while (ex.InnerException != null)
                                ex = ex.InnerException;
                            response.Response += Environment.NewLine + "iPadSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + ex.Message + Environment.NewLine + ex.StackTrace;
                            System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), response.Response);
                        }
                    }
                    else
                    {
                        response.Status = false;
                        response.NeedToSync = false;
                        response.Response = "Bad Data Received";
                    }
                }
                else
                {
                    response.Status = false;
                    response.NeedToSync = false;
                    response.Response = "User Data Missing";
                }
            }
            else
            {
                response.Status = false;
                response.NeedToSync = false;
                response.Response = "No Data Received";
            }
            return response;

        }

        [HttpPost]
        public FileSync FileDataSync(string FileName, string FilePath)
        {
            var Wrapper = new FileSync();
            try
            {
                var syncfilestosync = new SyncFilesToSync();
                syncfilestosync.fileId = Guid.NewGuid();
                syncfilestosync.fileName = FileName;
                syncfilestosync.filePath = FilePath;
                string strDirectory = HostingEnvironment.MapPath("~/Library/Uploads/" + FilePath);
                if (Directory.Exists(strDirectory))
                {
                    string FullPath = strDirectory + "/" + FileName;
                    if (File.Exists(FullPath))
                    {
                        syncfilestosync.data = System.IO.File.ReadAllBytes(FullPath);
                        syncfilestosync.isDelete = false;
                    }
                    else
                    {
                        syncfilestosync.isDelete = true;
                    }
                    syncfilestosync.isActive = true;
                    Wrapper.Success = true;
                }
                else
                {
                    syncfilestosync.isActive = false;
                    syncfilestosync.isDelete = true;
                    Wrapper.Success = false;
                }
                syncfilestosync.syncChanged = false;
                syncfilestosync.dateCreated = syncfilestosync.dateUpdated = DateTime.UtcNow;
                Wrapper.FilesToSync = new List<SyncFilesToSync>() { syncfilestosync };
            }
            catch (Exception ex)
            {
                Wrapper.Success = false;
                string Response = Environment.NewLine + ex.Message;
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                Response += Environment.NewLine + "FileDataSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + ex.Message + Environment.NewLine + ex.StackTrace;
                System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), Response);
            }
            return Wrapper;
        }
        
        [HttpPost]
        public PrintCart PrintCartSync([FromBody]PrintWrapper wrapper)
        {
            var Wrapper = new PrintCart();
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            try
            {
                byte[] toPdf = null;
                var list = CartData(wrapper.OrderId, true, wrapper.HideImage);
                var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A3, 0, 0, 20, 20);
                using (var output = new MemoryStream())
                using (var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, output))
                {
                    writer.CloseStream = false;
                    document.Open();
                    document = PlatiniWebService.CartPdfCreaterWithoutImage(document, writer, list, !string.IsNullOrEmpty(string.Empty), wrapper.Message, wrapper.HideImage);
                    document.Close();
                    toPdf = new byte[output.Position];
                    output.Position = 0;
                    output.Read(toPdf, 0, toPdf.Length);
                    document.Dispose();
                }
                if (toPdf != null)
                {                    
                    Wrapper.PdfString = Convert.ToBase64String(toPdf);
                }
                Wrapper.Success = true;
            }
            catch (Exception ex)
            {
                Wrapper.Success = false;
                string ResponseCatch = Environment.NewLine + ex.Message;
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                ResponseCatch += Environment.NewLine + "FilesDataSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + ex.Message + Environment.NewLine + ex.StackTrace;
                System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), ResponseCatch);
            }
            return Wrapper;
        }

        [HttpPost]
        public GenericResponse SendMail([FromBody]SendMail Wrapper)
        {
            GenericResponse Response;
            switch(Wrapper.Criteria)
            {
                case 1:                                       
                    EmailManager.SendWelcomeEmailCustomer(Wrapper.Email, Wrapper.AccountId.ToString(), Wrapper.UserName, Wrapper.Password);
                    Response = new GenericResponse() { Status = true, UserId = Wrapper.AccountId, Message = "Thank you for registering.  You can now login to the app." };
                    return Response;
                case 2:
                    var customer = dbcontext.Accounts.Find(Wrapper.AccountId);
                    EmailManager.SendOrderEmailToCustomer(Wrapper.OrderId, Wrapper.OrderNumber, (customer != null ? customer.Email : ""), Wrapper.AccountId.ToString());
                    Response = new GenericResponse() { Status = false, UserId = 0, Message = "Data Invalid Or incomplete" };
                    return Response;
                default:
                    Response = new GenericResponse() { Status = false, UserId = 0, Message = "Data Invalid Or incomplete" };
                    return Response;
            }            
        }

        [HttpPost]
        public FileSync FilesDataSync(string FileName)
        {
            var Wrapper = new FileSync();
            try
            {
                string FullPath = HostingEnvironment.MapPath("~/Library/Backgrounds/" + FileName);
                if (File.Exists(FullPath))
                {
                    var fileList = new List<SyncFilesToSync>();
                    var syncfilestosync = new SyncFilesToSync();
                    syncfilestosync.fileId = Guid.NewGuid();
                    syncfilestosync.fileName = FileName;
                    syncfilestosync.filePath = "Backgrounds";
                    syncfilestosync.data = System.IO.File.ReadAllBytes(FullPath);
                    syncfilestosync.isActive = true;
                    syncfilestosync.isDelete = false;
                    syncfilestosync.syncChanged = false;
                    syncfilestosync.dateCreated = syncfilestosync.dateUpdated = DateTime.UtcNow;
                    fileList.Add(syncfilestosync);
                    Wrapper.FilesToSync = fileList;
                }
                Wrapper.Success = true;
            }
            catch (Exception ex)
            {
                Wrapper.Success = false;
                string Response = Environment.NewLine + ex.Message;
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                Response += Environment.NewLine + "FilesDataSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + ex.Message + Environment.NewLine + ex.StackTrace;
                System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), Response);
            }
            return Wrapper;
        }

        [HttpPost]
        public FileSync OldFileDataSync(int DeviceType, string FileName)
        {
            var Wrapper = new FileSync();
            try
            {

                string FullPath = HostingEnvironment.MapPath("~/Library/Backgrounds/" + FileName);
                if (File.Exists(FullPath))
                {
                    var fileList = new List<SyncFilesToSync>();
                    for (int i = 0; i < 2; i++)
                    {
                        int width = 0;
                        int height = 0;
                        if (DeviceType == 1 && i == 0)
                        {
                            width = 768;
                            height = 1024;
                        }
                        else if (DeviceType == 1 && i == 1)
                        {
                            width = 1024;
                            height = 768;
                        }
                        else if (DeviceType == 2 && i == 0)
                        {
                            width = 1536;
                            height = 2048;
                        }
                        else if (DeviceType == 2 && i == 1)
                        {
                            width = 2048;
                            height = 1536;
                        }
                        else if (i == 0)
                        {
                            width = 750;
                            height = 1334;
                        }
                        else if (i == 1)
                        {
                            width = 1242;
                            height = 2208;
                        }

                        var syncfilestosync = new SyncFilesToSync();
                        syncfilestosync.fileId = Guid.NewGuid();
                        syncfilestosync.fileName = width + "_" + height + "_" + FileName;
                        syncfilestosync.filePath = "Backgrounds";
                        syncfilestosync.data = ImgHelper.ResizeImage(FullPath, width, height, "white");
                        syncfilestosync.isActive = true;
                        syncfilestosync.isDelete = false;
                        syncfilestosync.syncChanged = false;
                        syncfilestosync.dateCreated = syncfilestosync.dateUpdated = DateTime.UtcNow;
                        fileList.Add(syncfilestosync);
                    }
                    Wrapper.FilesToSync = fileList;
                }
                Wrapper.Success = true;
            }
            catch (Exception ex)
            {
                Wrapper.Success = false;
                string Response = Environment.NewLine + ex.Message;
                while (ex.InnerException != null)
                    ex = ex.InnerException;
                Response += Environment.NewLine + "OldDataSync - " + System.DateTime.UtcNow.ToString("MM/dd/yyyy") + " " + ex.Message + Environment.NewLine + ex.StackTrace;
                System.IO.File.AppendAllText(HostingEnvironment.MapPath("~/Template/SyncCheck.txt"), Response);
            }
            return Wrapper;
        }

        [NonAction]
        public Cart CartData(Guid Id, bool isPrint, bool HideImage)
        {
            decimal Price = 0.0m;
            DB.Order lastOrder = null;

            lastOrder = dbcontext.Orders.Find(Id);

            if (lastOrder != null)
            {
                var account = dbcontext.Accounts.Find(lastOrder.AccountId);
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
                retModel.UserId = lastOrder.AccountId;
                retModel.OrderId = lastOrder.OrderId;
                retModel.TagId = lastOrder.TagId.HasValue ? lastOrder.TagId.Value : dbcontext.OrderTags.FirstOrDefault(x => x.IsDefault.HasValue ? x.IsDefault.Value : false).OrderTagId;
                retModel.Tags = dbcontext.OrderTags.Where(x => x.IsActive == true && x.IsDelete == false).ToList().Select(x => new SelectedListValues() { Id = x.OrderTagId, Value = x.Name, IsSelected = false });
                retModel.Shipping = dbcontext.ShipVias.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.ShipViaId, Value = x.Name, IsSelected = false });
                retModel.Terms = dbcontext.Terms.Where(x => x.IsActive && !x.IsDelete).ToList().Select(x => new SelectedListValues() { Id = x.TermId, Value = x.Name, IsSelected = false });
                retModel.TotalQty = lastOrder.OriginalQty.HasValue ? lastOrder.OriginalQty.Value : 0;
                var clothes = new List<Cloth>(lastOrder.OrderScales.Count + lastOrder.OrderSizes.Count);

                clothes.AddRange(lastOrder.OrderScales.Select(x => x.ClothesScale.Cloth));
                clothes.AddRange(lastOrder.OrderSizes.Select(x => x.ClothesScaleSize.ClothesScale.Cloth));


                clothes = clothes.Distinct().ToList();
                var list = clothes.GroupBy(x => x.CategoryId);
                var mFitList = dbcontext.Fits.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.FitId, Value = x.Name, IsSelected = false }).ToList();
                var mInseamList = dbcontext.Inseams.Where(x => x.IsActive == true).Select(x => new SelectedListValues { Id = x.InseamId, Value = x.Name, IsSelected = false }).ToList();
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

                        if (HideImage)
                        { contents.Image = ""; }
                        else
                        {
                            if (cloth.ClothesImages.Any(x => x.IsActive && !x.IsDelete))
                                contents.Image = cloth.ClothesImages.OrderBy(x => x.SortOrder).FirstOrDefault(x => x.IsActive && !x.IsDelete).ImagePath;
                            else
                                contents.Image = "";
                        }
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
                                    var sSQPrePacks = dbcontext.ClothesScaleSizes.Where(x => x.ClothesScaleId == scale.ClothesScaleId).ToList();
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
                                    var sSQOpenSize = dbcontext.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
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
                                                if (dbcontext.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                    scaleRow.RtlAvlbl = false;
                                                else if (dbcontext.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                    SelectMany(x => dbcontext.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
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
                                var sSQOpenSize = dbcontext.ClothesScaleSizes.Where(x => x.ClothesScaleId == size.Key.ClothesScaleId && x.IsActive == true && x.IsDelete == false).ToList();
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
                                            if (dbcontext.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                scaleRow.RtlAvlbl = false;
                                            else if (dbcontext.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == FitId && (x.InseamId.HasValue ? x.InseamId.Value : 0) == InseamId && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                SelectMany(x => dbcontext.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
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
                                    var oScale = dbcontext.ClothesScales.FirstOrDefault(x => x.ClothesId == contents.ClothesId && (x.Fit != null ? x.FitId == sFit : true) && (x.InseamId != null ? x.InseamId == sInseam : true) && x.IsOpenSize == true);
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
                                                    if (dbcontext.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == sFit && (x.InseamId.HasValue ? x.InseamId.Value : 0) == sInseam && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).Count() <= 0)
                                                        scaleRow.RtlAvlbl = false;
                                                    else if (dbcontext.ClothesScales.Where(x => (x.FitId.HasValue ? x.FitId.Value : 0) == sFit && (x.InseamId.HasValue ? x.InseamId.Value : 0) == sInseam && x.InvQty > 0 && x.IsOpenSize == false && x.ClothesId == contents.ClothesId).ToList().
                                                        SelectMany(x => dbcontext.ClothesScaleSizes.Where(y => y.SizeId == os.SizeId && y.ClothesScaleId == x.ClothesScaleId), (x, y) => (y.Quantity.HasValue ? y.Quantity.Value : 0)).Sum() <= 0)
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
                if (!isPrint)
                    dbcontext.SaveChanges();
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
                if (printOrder.HasValue)
                {
                    Guid lastOrder = Guid.Empty;
                    if (printOrder.HasValue)
                        lastOrder = printOrder.Value;
                    var order = dbcontext.Orders.FirstOrDefault(x => x.OrderId == lastOrder);
                    if (order != null)
                    {
                        if (order.AddressId.HasValue)
                            Address = dbcontext.Addresses.FirstOrDefault(x => x.AddressId == order.AddressId.Value);
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
            var salesPerson = dbcontext.CustomerSalesPersons.FirstOrDefault(x => x.AccountId == model.UserId);
            model.SalesPerson = "Default";
            if (salesPerson != null)
            {
                model.SalesPersonId = salesPerson.SalesPersonId;
                model.SalesPerson = dbcontext.Accounts.FirstOrDefault(x => x.AccountId == model.SalesPersonId).Email;
            }
            return model;
        }

        [NonAction]
        public string GroupName(int TypeId)
        {
            string GroupName = string.Empty;
            var cType = dbcontext.Categories.Find(TypeId);
            if (cType != null)
            {
                var subCat = dbcontext.Categories.Find(cType.ParentId);
                if (subCat != null)
                {
                    var cat = dbcontext.Categories.Find(subCat.ParentId);
                    if (cat != null)
                        if (cat.ParentId == 0)
                            GroupName = cat.Name + " ";
                }
                GroupName += cType.Name;
            }
            return GroupName;
        }
        [NonAction]
        public bool AddCustomerToQuickBook(int CustomerId, string CompName)
        {
            QuickBookStrings.LoadQuickBookStrings(FailureFrom.Customer.ToString());
            string CompanyName = string.Empty;
            string Type = FailureFrom.Customer.ToString();
            var oauthValidator = new Intuit.Ipp.Security.OAuthRequestValidator(QuickBookStrings.AccessToken, QuickBookStrings.AccessTokenSecret, QuickBookStrings.ConsumerKey, QuickBookStrings.ConsumerSecret);
            QuickBookFailureRecord existFailure = null;
            if (CustomerId > 0)
                existFailure = dbcontext.QuickBookFailureRecords.FirstOrDefault(x => x.FailureFrom.ToLower() == Type.ToLower() && x.FailureFromId == CustomerId);
            try
            {
                var context = new Intuit.Ipp.Core.ServiceContext(QuickBookStrings.AppToken, QuickBookStrings.CompanyId, Intuit.Ipp.Core.IntuitServicesType.QBO, oauthValidator);
                if (QuickBookStrings.IsSandBox())
                    context.IppConfiguration.BaseUrl.Qbo = QuickBookStrings.SandBoxUrl;
                context.IppConfiguration.Logger.RequestLog.EnableRequestResponseLogging = true;
                context.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = QuickBookStrings.GetLoc(Type, CustomerId.ToString());
                var service = new Intuit.Ipp.DataService.DataService(context);
                var customer = new Intuit.Ipp.Data.Customer();
                var objCustomer = new Intuit.Ipp.Data.Customer();
                var objterm = new Intuit.Ipp.Data.Term();
                var account = new Intuit.Ipp.QueryFilter.QueryService<Intuit.Ipp.Data.Customer>(context);
                var regstAccount = dbcontext.Accounts.Find(CustomerId);
                if (existFailure != null)
                {
                    CompanyName = existFailure.FailureOriginalValue;
                }
                else if (regstAccount != null)
                {
                    if (regstAccount.IsActive == true && regstAccount.IsDelete == false && regstAccount.Companies.FirstOrDefault() != null)
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
                    //objCustomer = account.ExecuteIdsQuery("Select * From Customer where DisplayName='" + CompanyName.Trim() + "'").FirstOrDefault();
                    var dbcustomerOptionalInfo = dbcontext.CustomerOptionalInfoes.FirstOrDefault(x => x.AccountId == regstAccount.AccountId);
                    var dbterms = dbcontext.Terms.Where(x => x.TermId == dbcustomerOptionalInfo.TermId && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
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
                    var terms = new Intuit.Ipp.QueryFilter.QueryService<Intuit.Ipp.Data.Term>(context);
                    string termName = dbterms != null ? (dbterms.Name != null ? dbterms.Name : string.Empty) : "COD";
                    objterm = terms.ExecuteIdsQuery("Select * From Term Where Name='" + termName + "'").FirstOrDefault();

                    if (objCustomer != null)
                    {
                        objCustomer.DisplayName = CompanyName.Trim();
                        objCustomer.CompanyName = CompanyName.Trim();
                        objCustomer.GivenName = regstAccount.FirstName;
                        objCustomer.FamilyName = regstAccount.LastName;
                        objCustomer.PrimaryPhone = new Intuit.Ipp.Data.TelephoneNumber() { FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Phone : "" };
                        objCustomer.PrimaryEmailAddr = new Intuit.Ipp.Data.EmailAddress() { Address = regstAccount.Email };
                        if (billingAddress != null)
                            objCustomer.BillAddr = new Intuit.Ipp.Data.PhysicalAddress() { City = billingAddress.City, Line1 = billingAddress.Street, PostalCode = billingAddress.Pincode, CountrySubDivisionCode = billingAddress.State, Country = billingAddress.Country };
                        if (shippingAddress != null)
                            objCustomer.ShipAddr = new Intuit.Ipp.Data.PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State, Country = shippingAddress.Country };
                        objCustomer.Fax = new Intuit.Ipp.Data.TelephoneNumber() { AreaCode = "01", FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Fax : "" };
                        if (objterm != null)
                            objCustomer.SalesTermRef = new Intuit.Ipp.Data.ReferenceType() { name = objterm.Name, Value = objterm.Id };
                        objCustomer.ResaleNum = dbcustomerOptionalInfo.BusinessReseller;
                        service.Update(objCustomer);
                    }
                    else
                    {
                        objCustomer = new Intuit.Ipp.Data.Customer();
                        objCustomer.DisplayName = CompanyName.Trim();
                        objCustomer.CompanyName = CompanyName.Trim();
                        objCustomer.GivenName = regstAccount.FirstName;
                        objCustomer.FamilyName = regstAccount.LastName;
                        objCustomer.PrimaryPhone = new Intuit.Ipp.Data.TelephoneNumber() { FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Phone : "" };
                        objCustomer.PrimaryEmailAddr = new Intuit.Ipp.Data.EmailAddress() { Address = regstAccount.Email };
                        if (billingAddress != null)
                            objCustomer.BillAddr = new Intuit.Ipp.Data.PhysicalAddress() { City = billingAddress.City, Line1 = billingAddress.Street, PostalCode = billingAddress.Pincode, CountrySubDivisionCode = billingAddress.State, Country = billingAddress.Country };
                        if (shippingAddress != null)
                            objCustomer.ShipAddr = new Intuit.Ipp.Data.PhysicalAddress() { City = shippingAddress.City, Line1 = shippingAddress.Street, PostalCode = shippingAddress.Pincode, CountrySubDivisionCode = shippingAddress.State, Country = shippingAddress.Country };
                        objCustomer.Fax = new Intuit.Ipp.Data.TelephoneNumber() { AreaCode = "01", FreeFormNumber = regstAccount.Communications.FirstOrDefault() != null ? regstAccount.Communications.FirstOrDefault().Fax : "" };
                        if (objterm != null)
                            objCustomer.SalesTermRef = new Intuit.Ipp.Data.ReferenceType() { name = objterm.Name, Value = objterm.Id };
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
                dbcontext.QuickBookFailureRecords.Add(newFailure);
                dbcontext.SaveChanges();
            }
            return false;
        }

    }
}
