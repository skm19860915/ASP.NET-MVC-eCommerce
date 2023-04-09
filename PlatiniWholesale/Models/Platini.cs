using Platini.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Platini.Models
{
    public class CommonModel
    {
        public LoginModel loginModel { get; set; }
        public RegistrationModel registrationModel { get; set; }
    }

    public class LoginModel
    {
        [Display(Name = "UserName", ResourceType = typeof(Resources.Resources))]
        [Required(ErrorMessage = "User name is required.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
        public bool isPartial { get; set; }
    }

    public class RegistrationModel
    {
        [StringLength(41, ErrorMessage = "The Company Name cannot exceed 41 characters. ")]
        public string CompanyName { get; set; }
        [Required(ErrorMessage = "User Name is required.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [System.Web.Mvc.CompareAttribute("Password", ErrorMessage = "Passwords do not match.")]
        public string cPassword { get; set; }
        [StringLength(25, ErrorMessage = "The First Name cannot exceed 25 characters. ")]
        [Required(ErrorMessage = "First Name is required.")]
        public string FirstName { get; set; }
        [StringLength(25, ErrorMessage = "The Last Name cannot exceed 25 characters. ")]
        [Required(ErrorMessage = "Last Name is required.")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Phone Number is required.")]
        public string PhoneNumber { get; set; }
        public string CountryCode { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            ErrorMessage = "The email address entered is not in a valid format")]
        public string Email { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string ResellerNumber { get; set; }
        public bool forceWholesale { get; set; }
    }

    public enum RolesEnum
    {
        [Description("Super Admin")]
        SuperAdmin = 1,
        [Description("Admin")]
        Admin = 2,
        [Description("Warehouse")]
        Warehouse = 3,
        [Description("Sales Person")]
        SalesPerson = 4,
        [Description("Customer")]
        Customer = 5,
        [Description("User")]
        User = 6,
    }

    public enum AddressTypeEnum
    {
        [Description("Billing Address")]
        BillingAddress = 1,
        [Description("Shipping Address")]
        ShippingAddress = 2
    }

    public enum ModeEnum
    {
        Edit = 1,
        View,
        Order
    }

    public enum CustomerType
    {
        [Description("Wholesale")]
        Wholesale = 1,
        [Description("Retail")]
        Retail = 2
    }

    public enum TransactionType
    {
        Paypal = 1,
        ExpressCheckout
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Completed,
        Declined
    }

    public class Menu
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class ClothListItem
    {
        public int ClothesId { get; set; }
        public string StyleNumber { get; set; }
        public string ImagePath { get; set; }
        public bool? IsActive { get; set; }
        public Nullable<decimal> Price { get; set; }
        public Nullable<decimal> DiscountedPrice { get; set; }
        public Nullable<int> Clearance { get; set; }
        public Nullable<DateTime> FutureDeliveryDate { get; set; }
    }

    public class ClothList
    {
        public IList<ClothListItem> List { get; set; }
        public ClothList()
        {
            List = new List<ClothListItem>();
        }
    }

    public class DetailViewClass
    {
        public int ClothesId { get; set; }
        public int CategoryId { get; set; }
        public string StyleNumber { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public decimal MSRP { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
        public int PrevStyle { get; set; }
        public int NextStyle { get; set; }
        public bool isFuture { get; set; }
        public DateTime fDate { get; set; }
        public bool isSelected { get; set; }
        public Nullable<decimal> DiscountedPrice { get; set; }
        public Nullable<decimal> DiscountedMSRP { get; set; }
        public List<Platini.DB.ClothesImage> Images { get; set; }
        public List<ClothesScaleClass> AvailableOpenSizes { get; set; }
        public List<ClothesScaleClass> AvailablePrePacks { get; set; }
        public List<SelectedListValues> FitList { get; set; }
        public List<SelectedListValues> InseamList { get; set; }
        public DetailViewClass()
        {
            AvailableOpenSizes = new List<ClothesScaleClass>();
            AvailablePrePacks = new List<ClothesScaleClass>();
            RelatedProducts = new List<RelatedProductsItem>();
            MoreProducts = new List<RelatedProductsItem>();
            RelatedColors = new List<RelatedProductsItem>();
        }
        public int Clearance { get; set; }
        public string Note { get; set; }
        public List<RelatedProductsItem> RelatedProducts { get; set; }
        public List<RelatedProductsItem> MoreProducts { get; set; }
        public List<RelatedProductsItem> RelatedColors { get; set; }
        public string PageType { get; set; }
    }

    public class LineSheetViewClass
    {
        public int ClothesId { get; set; }
        public int CategoryId { get; set; }
        public string StyleNumber { get; set; }
        public string Type { get; set; }
        public string Sub { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public decimal MSRP { get; set; }
        public string Color { get; set; }
        public int PrevStyle { get; set; }
        public int NextStyle { get; set; }
        public bool isFuture { get; set; }
        public bool isActive { get; set; }
        public string fDate { get; set; }
        public bool isSelected { get; set; }
        public int? fitID { get; set; }
        public int Clearance { get; set; }
        public Nullable<decimal> DiscountedPrice { get; set; }
        public Nullable<decimal> DiscountedMSRP { get; set; }
        public int TotalQty { get; set; }
        public List<Platini.DB.ClothesImage> Images { get; set; }
        public List<ClothesScaleClass> ClothesScale { get; set; }
        public List<SelectedListValues> FitList { get; set; }
        public List<SelectedListValues> InseamList { get; set; }
        public LineSheetViewClass()
        {
            ClothesScale = new List<ClothesScaleClass>();
        }
    }

    public class SortOrders
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "so")]
        public int so { get; set; }
    }

    public class LinesheetSelectModel
    {
        public List<SelectedListValues> Categories { get; set; }
        public List<SelectedListValues> SubCategories { get; set; }
        public List<SelectedListValues> Types { get; set; }

        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int SubCategoryTypeId { get; set; }
    }

    public class ContentDetails
    {
        public List<ClothesScaleSizeClass> OpenSizes { get; set; }
        public ClothesScaleClass Pack { get; set; }
        public string Fit { get; set; }
        public string Inseam { get; set; }
        public int FitId { get; set; }
        public int InseamId { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public bool isConfirmed { get; set; }
        public bool ShowPrepack { get; set; }
        public ContentDetails()
        {
            OpenSizes = new List<ClothesScaleSizeClass>();
            Pack = new ClothesScaleClass();
        }

    }
    public class CartContents
    {
        public int ClothesId { get; set; }
        public string Image { get; set; }
        public string StyleNumber { get; set; }
        public string Delivery { get; set; }
        public decimal Price { get; set; }

        public List<ContentDetails> SPs { get; set; }
        public CartContents()
        {
            SPs = new List<ContentDetails>();
        }
    }

    public class CartCloth
    {
        public string GroupName { get; set; }
        public List<CartContents> Contents { get; set; }
        public CartCloth()
        {
            Contents = new List<CartContents>();
        }
    }

    public class Cart
    {

        public Guid OrderId { get; set; }
        public int UserId { get; set; }
        public CartOwner CartOwner { get; set; }
        public List<CartCloth> Clothes { get; set; }
        public bool isSubmit { get; set; }
        public int TotalQty { get; set; }
        public string OrdNum { get; set; }
        public string Note { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal ShippingAmount { get; set; }
        public int TagId { get; set; }
        public IEnumerable<SelectedListValues> Terms { get; set; }
        public IEnumerable<SelectedListValues> Shipping { get; set; }
        public IEnumerable<SelectedListValues> Tags { get; set; }
        public string UserName { get; set; }
        public string PrintMe { get; set; }
        public bool isConfirmed { get; set; }
        public Cart()
        {
            Clothes = new List<CartCloth>();
            Terms = new List<SelectedListValues>();
            Shipping = new List<SelectedListValues>();
            Tags = new List<SelectedListValues>();
        }
    }

    public class CartOwner
    {
        public int UserId { get; set; }
        public string Buyer { get; set; }
        public string CompanyName { get; set; }
        public AddressText BillingAddress { get; set; }
        public AddressText ShippingAddress { get; set; }
        public int TermId { get; set; }
        public int ShipVia { get; set; }
        public decimal Discount { get; set; }
        public int TagId { get; set; }
        public int CommunicationId { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public bool isRetail { get; set; }
        public string SalesPerson { get; set; }
        public int SalesPersonId { get; set; }
    }

    public class AddressText
    {
        public int AddressId { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string To { get; set; }
        public string ZipCode { get; set; }
    }

    public class WebsiteSettingModel
    {
        public int WebsiteSettingId { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public bool CheckKey { get; set; }
    }
    public enum FailureFrom
    {
        Invoice = 1,
        Customer = 2,
        Product = 3
    }

    public class QuickBookStrings
    {
        public static string AccessToken = string.Empty;
        public static string AccessTokenSecret = string.Empty;
        public static string ConsumerKey = string.Empty;
        public static string ConsumerSecret = string.Empty;
        public static string AppToken = string.Empty;
        public static string CompanyId = string.Empty;
        public static string ProductCategory = string.Empty;
        public static string SandBoxUrl = string.Empty;
        public static void LoadQuickBookStrings(string Type)
        {
            AccessToken = ConfigurationManager.AppSettings["QuickBookAccessToken"];
            AccessTokenSecret = ConfigurationManager.AppSettings["QuickBookAccessTokenSecret"];
            ConsumerKey = ConfigurationManager.AppSettings["QuickBookAppConsumerKey"];
            ConsumerSecret = ConfigurationManager.AppSettings["QuickBookAppConsumerSecret"];
            AppToken = ConfigurationManager.AppSettings["QuickBookAppToken"];
            CompanyId = ConfigurationManager.AppSettings["QuickBookCompanyId"];
            if (Type.ToLower() == FailureFrom.Product.ToString().ToLower())
                ProductCategory = ConfigurationManager.AppSettings["QuickBookProductCategory"];
            SandBoxUrl = ConfigurationManager.AppSettings["QuickBookSandboxUrl"];
        }
        public static bool UseQuickBook()
        {
            int proceedQuickBook;
            return (int.TryParse(ConfigurationManager.AppSettings["QuickBookActive"], out proceedQuickBook)) && proceedQuickBook > 0;
        }
        public static bool IsSandBox()
        {
            int proceedQuickBook;
            return (int.TryParse(ConfigurationManager.AppSettings["QuickBookSandbox"], out proceedQuickBook)) && proceedQuickBook > 0;
        }
        public static string GetLoc(string type, string Id)
        {
            var di = new System.IO.DirectoryInfo(HttpContext.Current.Server.MapPath("~/Library/Uploads/" + type + "/" + Id));
            di.Create();
            return di.FullName;
        }
        public static string FailureText(string type, string Id)
        {
            var di = new System.IO.DirectoryInfo(GetLoc(type, Id));
            string retSting = string.Empty;
            var file = di.GetFiles("*", System.IO.SearchOption.AllDirectories).OrderByDescending(x => x.LastWriteTime).FirstOrDefault();
            if (file != null)
            {
                var reader = new System.IO.StreamReader(file.FullName);
                retSting = reader.ReadToEnd();
                reader.Dispose();
            }
            return retSting;
        }
        public static void DeleteFiles(string type, string Id)
        {
            var di = new System.IO.DirectoryInfo(GetLoc(type, Id));
            di.Delete(true);
        }
    }

    public class imgList
    {
        public Guid Id { get; set; }
        public int aId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int SO { get; set; }
        public imgList()
        {
            Id = Guid.NewGuid();
        }
    }

    public class ClothImgUpload
    {
        public int ClothesId { get; set; }
        public string StyleNumber { get; set; }
        public List<imgList> Images { get; set; }
        public bool isLast { get; set; }
        public ClothImgUpload()
        {
            Images = new List<imgList>();
        }
    }

    public class IPData
    {
        public string statusCode { get; set; }
        public string statusMessage { get; set; }
        public string ipAddress { get; set; }
        public string countryCode { get; set; }
        public string countryName { get; set; }
        public string regionName { get; set; }
        public string cityName { get; set; }
        public string zipCode { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string timeZone { get; set; }
    }

    public class BillInfoAddress
    {

        public int AddressType { get; set; }
        public int AddressId { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string City { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string State { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string Zip { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string Country { get; set; }
    }

    public class BillInfo
    {
        public BillInfoAddress BillingAddress { get; set; }
        public BillInfoAddress ShippingAddress { get; set; }
        public int UserId { get; set; }
        public Guid OrderId { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string CardFName { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string CardLName { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        [StringLength(16)]
        public string CardNumber { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        [StringLength(4)]
        public string CVV { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public string CardType { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public int ExpMonth { get; set; }
        [Required(ErrorMessage = "This field is mandatory")]
        public int ExpYear { get; set; }

        public int SavedCard { get; set; }
        public string ST { get; set; }
        public string GS { get; set; }
        public string GT { get; set; }
        public string QT { get; set; }

        public List<SelectedListValues> SavedCards { get; set; }
        public List<SelectedStringValues> Cards { get; set; }
        public List<SelectedListValues> Years { get; set; }
        public List<SelectedListValues> Months { get; set; }
        public BillInfo()
        {
            BillingAddress = new BillInfoAddress();
            ShippingAddress = new BillInfoAddress();
            Cards = new List<SelectedStringValues>();
            Years = new List<SelectedListValues>();
            Months = new List<SelectedListValues>();
            SavedCards = new List<SelectedListValues>();
        }
    }

    public class TransactionResult
    {
        public bool Success { get; set; }
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; }
    }

    public class ReceiptItems
    {
        public string Style { get; set; }
        public string Img { get; set; }
        public string Qty { get; set; }
        public string Total { get; set; }
    }

    public class Receipt
    {
        public string SubTotal { get; set; }
        public string Shipping { get; set; }
        public string GrandTotal { get; set; }
        public string Discount { get; set; }
        public string OrderNumber { get; set; }
        public Guid OrderId { get; set; }
        public string PaymentMethodImg { get; set; }
        public List<ReceiptItems> Items { get; set; }
        public Receipt()
        {
            Items = new List<ReceiptItems>();
        }
    }

    public enum LineSheetSort
    {
        DateChanged = 1,
        FutureDelivery = 2,
        Category = 3
    }

    public class ClearanceModel
    {
        public int Id { get; set; }
        public int Value { get; set; }
    }
}