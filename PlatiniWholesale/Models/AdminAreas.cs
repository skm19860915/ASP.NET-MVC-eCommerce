using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Platini.DB;
using System.ComponentModel;

namespace Platini.Models
{
    public class CommonClass
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int SortOrder { get; set; }
        [Required]
        public bool IsActive { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }

    public class TagClass
    {
        public int OrderTagId { get; set; }
        [Required]
        public string Name { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDefault { get; set; }
    }

    public class FitClass
    {
        public int FitId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int SortOrder { get; set; }
        [Required]
        public bool IsActive { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }

    public class InseamClass
    {
        public int InseamId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int SortOrder { get; set; }
        [Required]
        public bool IsActive { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }

    public class CategoryClass
    {
        public int CategoryId { get; set; }
        [Display(Name = "CategoryClass_Name", ResourceType = typeof(Resources.Resources))]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }
        [Required]
        public int SortOrder { get; set; }
        [Required]
        public bool IsActive { get; set; }
        [Required]
        public int ParentId { get; set; }
        public Nullable<int> CategoryLevel { get; set; }
        public IEnumerable<ParentCategory> parentCategory { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }

    public class ParentCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public int ParentId { get; set; }
    }

    public class SizeEditClass
    {
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Category> SubCategories { get; set; }
        public IEnumerable<Category> Types { get; set; }
        public IList<SizeGroup> SizeGroups { get; set; }

        [Required]
        public int categoryId { get; set; }
        [Required]
        public int subCategoryId { get; set; }
        [Required]
        public int subCategoryTypeId { get; set; }
        [Required]
        public int sizeGroupId { get; set; }

        public SizeEditClass()
        {
            Categories = new List<Category>();
            SubCategories = new List<Category>();
            Types = new List<Category>();
            SizeGroups = new List<SizeGroup>();
        }
    }

    public class SizeClass
    {
        public int SizeId { get; set; }
        [Required]

        public string Name { get; set; }
        [Required]

        public int SortOrder { get; set; }
        [Required]
        public bool IsActive { get; set; }
        [Required(ErrorMessage = "Category is required.")]
        public int CId { get; set; }
        [Required(ErrorMessage = "SubCategory is required.")]
        public int SId { get; set; }
        [Required(ErrorMessage = "Category Type is required.")]
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Size Group is required.")]
        public int SizeGroupId { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Category> SubCategories { get; set; }
        public IEnumerable<Category> CategoryTypes { get; set; }
        public IEnumerable<SizeGroup> SizeGroups { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }


    public class SizeGroupClass
    {
        public int SizeGroupId { get; set; }
        [Required]

        public string Name { get; set; }
        [Required]

        public int SortOrder { get; set; }
        [Required]

        public bool IsActive { get; set; }
        [Required(ErrorMessage = "Category is required.")]
        public int CId { get; set; }
        [Required(ErrorMessage = "SubCategory is required.")]
        public int SId { get; set; }
        [Required(ErrorMessage = "Category Type is required.")]
        public int CategoryId { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Category> SubCategories { get; set; }
        public IEnumerable<Category> CategoryTypes { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }

    public class ClothesClass
    {
        public int ClothesId { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        public string StyleNumber { get; set; }
        [Required]
        public int SortOrder { get; set; }
        [Required]
        public Nullable<bool> IsActive { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public IEnumerable<ParentCategory> parentCategory { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
    }

    public class StepOneClass
    {
        public int ClothesId { get; set; }
        [Required]
        public string StyleNumber { get; set; }
        public string ClothesDescription { get; set; }
        public string Color { get; set; }
        //public int IsSent { get; set; }
        //public int OriginalQty { get; set; }
        //public int SortOrder { get; set; }
        //public int BrandId { get; set; }
        public int SortOrder { get; set; }
        public Nullable<bool> IsActive { get; set; }
        //public Nullable<bool> Clearance { get; set; }  

        public Nullable<System.DateTime> FutureDeliveryDate { get; set; }

        public Nullable<decimal> ProductCost { get; set; }
        [Required]
        public Nullable<decimal> MSRP { get; set; }
        [Required]
        public Nullable<decimal> Price { get; set; }
        public Nullable<decimal> DiscountedPrice { get; set; }
        public Nullable<decimal> DiscountedMSRP { get; set; }
        [Required]
        public int Qty { get; set; }

        public List<int> Quantites { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Category> SubCategories { get; set; }
        public IEnumerable<Category> CategoryTypes { get; set; }
        public IEnumerable<Brand> Brands { get; set; }
        public IList<SizeGroup> SizeGroups { get; set; }
        public ClothAttribute ClothAttribute { get; set; }

        [Required]
        public int CId { get; set; }
        [Required]
        public int SId { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int SizeGroupId { get; set; }
        [Required]
        public int BrandId { get; set; }
        public string Image1 { get; set; }
        public string Image2 { get; set; }
        public string Image3 { get; set; }
        public string Image4 { get; set; }
        public string Image5 { get; set; }
        public string Ids { get; set; }
        public string Tags { get; set; }

        public StepOneClass()
        {
            Categories = new List<Category>();
            SubCategories = new List<Category>();
            CategoryTypes = new List<Category>();
            SizeGroups = new List<SizeGroup>();
            Brands = new List<Brand>();
            ClothAttribute = new ClothAttribute();
        }

        public bool Clearance { get; set; }
    }


    public class ClothClass
    {
        public int ClothesId { get; set; }
        [Required]
        public int CId { get; set; }
        [Required]
        public int SId { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int SizeGroupId { get; set; }

        [Required]
        public string StyleNumber { get; set; }
        public string Color { get; set; }
        public Nullable<System.DateTime> FutureDeliveryDate { get; set; }

        [Required]
        public Nullable<decimal> ProductCost { get; set; }
        [Required]
        public Nullable<decimal> MSRP { get; set; }
        [Required]
        public Nullable<decimal> Price { get; set; }
    }

    public class SelectedListValues
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ClothAttribute
    {
        public List<SelectedListValues> FitList { get; set; }
        public List<SelectedListValues> InseamList { get; set; }

        public ClothAttribute()
        {
            FitList = new List<SelectedListValues>();
            InseamList = new List<SelectedListValues>();
        }
    }

    public class RelatedProductListItem
    {
        public int ClothesId { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string SubCategoryTypeName { get; set; }
        public string StyleNumber { get; set; }
    }

    public class RelatedProductsItem
    {
        public int ClothesId { get; set; }
        public string SubCategoryTypeName { get; set; }
        public string StyleNumber { get; set; }
        public string ImagePath { get; set; }
        public decimal? Price { get; set; }
    }

    public class AllProductListItem
    {
        public int ClothesId { get; set; }
        public string StyleNumber { get; set; }
        public bool Checked { get; set; }
        //public string ImageLink
    }

    public class AllProductListClass
    {
        public List<AllProductListItem> AllProductList { get; set; }
        public AllProductListClass()
        {
            AllProductList = new List<AllProductListItem>();
        }
    }

    public class RelatedProductClass
    {
        public int ClothesId { get; set; }
        public string StyleNumber { get; set; }

        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Category> SubCategories { get; set; }
        public IEnumerable<Category> Types { get; set; }
        public IList<RelatedProductListItem> RelatedProducts { get; set; }

        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int SubCategoryTypeId { get; set; }

        public RelatedProductClass()
        {
            Categories = new List<Category>();
            SubCategories = new List<Category>();
            Types = new List<Category>();
            RelatedProducts = new List<RelatedProductListItem>();
        }
    }

    public class StepTwoClass
    {
        public int ClothesId { get; set; }
        public string StyleNumber { get; set; }
        public string Ids { get; set; }
        public string isFuture { get; set; }
        public List<ClothesImage> Images { get; set; }
        public List<ClothesScaleClass> AvailableOpenSizes { get; set; }
        public List<ClothesScaleClass> AvailablePrePacks { get; set; }
        public List<SelectedListValues> FitList { get; set; }
        public List<SelectedListValues> InseamList { get; set; }
        public StepTwoClass()
        {
            AvailableOpenSizes = new List<ClothesScaleClass>();
            AvailablePrePacks = new List<ClothesScaleClass>();
        }
    }

    //public class AvailableOpenSizeClass
    //{
    //    public string Name { get; set; }
    //    public string FitName { get; set; }
    //    public string InseamName { get; set; }
    //    public List<ClothSizeClass> ClothSizeClass { get; set; }

    //    public AvailableOpenSizeClass()
    //    {
    //        ClothSizeClass = new List<ClothSizeClass>();
    //    }
    //}

    //public class AvailablePrePackSizeClass
    //{
    //    public string Name { get; set; }
    //    public int selectedFitId { get; set; }
    //    public int selectedInseamId { get; set; }
    //    public List<SelectedListValues> FitList { get; set; }
    //    public List<SelectedListValues> InseamList { get; set; }
    //    public int InventoryQuantity { get; set; }
    //    public List<ClothSizeClass> ClothSizeClass { get; set; }

    //    public AvailablePrePackSizeClass()
    //    {
    //        ClothSizeClass = new List<ClothSizeClass>();
    //    }
    //}

    //public class ClothSizeClass
    //{
    //    public int ClothesScaleId { get; set; }
    //    [Required]
    //    public int ClothesId { get; set; }
    //    [Required]
    //    public int SizeId { get; set; }
    //    public string SizeName { get; set; }
    //    public Nullable<int> Quantity { get; set; }
    //    public Nullable<int> InvQty { get; set; }
    //    public string ScaleName { get; set; }
    //    public Nullable<bool> IsOpenSize { get; set; }
    //    public Nullable<int> FitId { get; set; }
    //    public Nullable<int> InseamId { get; set; }
    //}

    public class ClothesScaleClass
    {
        [Required]
        public int ClothesId { get; set; }
        public int ClothesScaleId { get; set; }
        public string Name { get; set; }
        public string FitName { get; set; }
        public string InseamName { get; set; }
        public int selectedFitId { get; set; }
        public int selectedInseamId { get; set; }
        public List<SelectedListValues> FitList { get; set; }
        public List<SelectedListValues> InseamList { get; set; }
        public Nullable<int> InvQty { get; set; }
        public Nullable<int> PurchasedQty { get; set; }
        public Nullable<int> QuantSum { get; set; }
        public Guid OrderSSId { get; set; }
        public Nullable<int> FitId { get; set; }
        public Nullable<int> InseamId { get; set; }
        public Nullable<bool> IsOpenSize { get; set; }
        public bool isConfirm { get; set; }
        public List<ClothesScaleSizeClass> ClothesScaleSizeClass { get; set; }

        public ClothesScaleClass()
        {
            ClothesScaleSizeClass = new List<ClothesScaleSizeClass>();
        }

    }


    public class ClothesScaleSizeClass
    {
        public int ClothesScaleSizeId { get; set; }
        [Required]
        public int ClothesScaleId { get; set; }
        [Required]
        public int SizeId { get; set; }
        public Guid OrderSSId { get; set; }
        public string SizeName { get; set; }
        public Nullable<int> Quantity { get; set; }
        public Nullable<int> PurchasedQuantity { get; set; }
        public Nullable<int> TotalInventory { get; set; }
        public bool isConfirm { get; set; }
        public bool RtlAvlbl { get; set; }
    }


    public class EmployeeClass
    {
        public int EmployeeId { get; set; }
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(128, ErrorMessage = "The First Name cannot exceed 128 characters. ")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(128, ErrorMessage = "The Last Name cannot exceed 128 characters. ")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(64, ErrorMessage = "The Username cannot exceed 64 characters. ")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            ErrorMessage = "The email address entered is not in a valid format")]
        public string Email { get; set; }
        public Nullable<bool> IsActive { get; set; }
        [Required]
        public int RoleId { get; set; }
        public string Role { get; set; }
        [Required(ErrorMessage = "Phone Number is required.")]
        public string PhoneNo { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }

    public class CustomerClass
    {
        public int AccountId { get; set; }
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(25, ErrorMessage = "The First Name cannot exceed 25 characters. ")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(25, ErrorMessage = "The Last Name cannot exceed 25 characters. ")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(64, ErrorMessage = "The Username cannot exceed 64 characters. ")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            ErrorMessage = "The email address entered is not in a valid format")]
        public string Email { get; set; }
        public Nullable<bool> IsActive { get; set; }
        [StringLength(41, ErrorMessage = "The Business Name cannot exceed 41 characters. ")]
        public string BusinessName { get; set; }
        public string DisplayName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        [Required(ErrorMessage = "Phone Number is required.")]
        public string PhoneNo { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
        public string CountryCode { get; set; }

        public List<SelectedListValues> SalesPersonList { get; set; }
        public List<SelectedListValues> TermList { get; set; }
        public int SelectedSalesPerson { get; set; }
        public int SelectedTerm { get; set; }
        public DateTime? LastLoginDate { get; set; }

        public string BusinessReseller { get; set; }
        public string SecondEmail { get; set; }
        public string SecondPassword { get; set; }
        public bool Wholesale { get; set; }
        public string Note { get; set; }

        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }

        public bool SameAsBillingCheckBox { get; set; }
        public string FromInActiveCustomerPage { get; set; }

        public int UserCount { get; set; }
        public string BusinessLicense { get; set; }
        public decimal Discount { get; set; }
    }

    public class AddressModel
    {
        public int AddressId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Pincode { get; set; }
        public int AddressTypeId { get; set; }
        public int AccountId { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsDelete { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
        public string ShipTo { get; set; }
    }
    public class UserClass
    {
        public int AccountId { get; set; }
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(128, ErrorMessage = "The First Name cannot exceed 128 characters. ")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(128, ErrorMessage = "The Last Name cannot exceed 128 characters. ")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(64, ErrorMessage = "The Username cannot exceed 64 characters. ")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$",
                            ErrorMessage = "The email address entered is not in a valid format")]
        public string Email { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public string BusinessName { get; set; }
        [Required(ErrorMessage = "Phone Number is required.")]
        public string PhoneNo { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
        public int CustomerId { get; set; }
        public string SalesPerson { get; set; }
        public string BusinessLicense { get; set; }
    }

    public class BackgroundClass
    {
        public int Id { get; set; }
        [Required]
        public int SortOrder { get; set; }
        [Required]
        public bool IsActive { get; set; }
        public string MainPic { get; set; }
        public List<SelectedStringValues> MobPics { get; set; }
        public BackgroundClass()
        {
            MainPic = string.Empty;
            MobPics = new List<SelectedStringValues>();
        }
    }

    public class SelectedStringValues
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class OrderHeader
    {
        public List<SelectedListValues> Statuses { get; set; }
        public decimal GrandTotal { get; set; }
        public List<SelectedListValues> SalesPersons { get; set; }
        public List<SelectedListValues> Tags { get; set; }
        public int? Tag { get; set; }
        public int? SalesMan { get; set; }
        public string Search { get; set; }
    }

    public class BagsBoxes
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public Guid OrderId { get; set; }
        public string Shipping { get; set; }
        public int ShipId { get; set; }
        public int Status { get; set; }
        public IEnumerable<SelectedListValues> ShipVias { get; set; }
        public int BagCount { get; set; }
        public int BoxCount { get; set; }
        public List<string> StyleNumberList { get; set; }
    }

    public class BBDetails
    {
        public List<Bag> Bags { get; set; }
        public List<Box> Boxes { get; set; }
        public BBDetails()
        {
            Bags = new List<Bag>();
            Boxes = new List<Box>();
        }
    }

    public class PermissionClass
    {
        public int RoleId { get; set; }

        public string PermissionName { get; set; }

        public bool? CanView { get; set; }

        public bool? CanEdit { get; set; }

        public bool? CanOrder { get; set; }
    }

    public class OrderTotals
    {
        public Guid OrderId { get; set; }
        public int UserId { get; set; }
        public decimal GT { get; set; }
        public decimal SC { get; set; }
        public decimal DT { get; set; }
        public decimal FA { get; set; }
    }

    public class UpsValidate
    {
        public AddressText ShipToAddress { get; set; }
        public string ShipFrom { get; set; }
        public Guid OrderId { get; set; }
        public int BoxCount { get; set; }
        public bool COD { get; set; }
        public bool satD { get; set; }
        public string Verified { get; set; }
        public UpsValidate()
        {
        }
    }

    public class UpsReview
    {
        public string lNo { get; set; }
        public string UN { get; set; }
        public string PW { get; set; }
        public string ShipAddress { get; set; }
        public string ShipFrom { get; set; }
        public string sId { get; set; }
        public string ServiceName { get; set; }
        public string Price { get; set; }
        public List<Box> Boxes { get; set; }
        public Guid Id { get; set; }
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public bool isCod { get; set; }
        public bool isSatD { get; set; }
        public UpsReview()
        {
            Boxes = new List<Box>();
        }
    }

    public class UpsFinal
    {
        public string Message { get; set; }
        public string Price { get; set; }
        public string sNo { get; set; }
        public string file { get; set; }
        public bool isSuccess { get; set; }
    }

    public class ShowRoomImages
    {
        public List<DB.ShowRoomImage> Images { get; set; }
        public int AccountId { get; set; }
    }

    public class InvRept
    {
        public int Id { get; set; }
        public string Pic { get; set; }
        public string Style { get; set; }
        public DateTime iDate { get; set; }
        public bool isFuture { get; set; }
        public int oQty { get; set; }
        public int aQty { get; set; }
        public int cQty { get; set; }
        public decimal sQty { get; set; }
        public decimal pQty { get; set; }
        public decimal aCost { get; set; }
        public decimal Cost { get; set; }
        public decimal Total { get; set; }
        public decimal Amount { get; set; }
        public decimal Profit { get; set; }
        public int Days { get; set; }
    }

    public class SBCRept
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SalesPerson { get; set; }
        public DateTime iDate { get; set; }
        public decimal Cost { get; set; }
        public decimal Amount { get; set; }
        public decimal Profit { get; set; }
    }

    public class DashBoard
    {
        public decimal TotAmt { get; set; }
        public int uCount { get; set; }
        public int iCount { get; set; }
        public int vCount { get; set; }
        public int OrdCount { get; set; }
        public bool showDollar { get; set; }
        public bool showDashboard { get; set; }
    }

    public class OnlineCustomers
    {
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string SessionId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    public class SiteVisitors
    {
        public string Name { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Date { get; set; }
    }

    public enum OrderMode
    {
        [Description("New")]
        New = 1,
        [Description("In Warehouse")]
        InProcess,
        [Description("Shipped")]
        Shipped,
        [Description("Back Order")]
        BackOrder,
        [Description("Completed")]
        Completed,
        [Description("Packed")]
        Packed,
        [Description("Pending")]
        Pending
    }

    public class SBTModel
    {
        public List<SelectedListValues> SalesPersons { get; set; }
        public List<SelectedListValues> OrderStatuses { get; set; }
        public List<SelectedListValues> OrderTags { get; set; }
    }
}