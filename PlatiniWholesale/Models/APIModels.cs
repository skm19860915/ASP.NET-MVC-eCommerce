using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Platini.Models
{
    public class ActivationWrapper
    {
        public string ActivationCode { get; set; }
        public string ActivationKey { get; set; }
        public System.Guid DeviceId { get; set; }
    }

    public class ResponseWrapper
    {
        public bool Status { get; set; }
        public bool NeedToSync { get; set; }
        public dynamic Response { get; set; }
    }

    public class RequestWrapper
    {        
        public int UserId { get; set; }
        public int? Mode { get; set; }
        public int? SubMode { get; set; }
        public string DeviceInfo { get; set; }
        public DateTime? SyncDate { get; set; }
    }

    public class Sync
    {
        public ICollection<SyncCategory> Categories { get; set; }
        public ICollection<SyncAccount> Accounts { get; set; }
        public ICollection<SyncCommunication> Communications { get; set; }
        public ICollection<SyncAddress> Addresses { get; set; }
        public ICollection<SyncCompany> Companies { get; set; }
        public ICollection<SyncAddressType> AddressType { get; set; }
        public ICollection<SyncCustomerOptionalInfo> CustomerOptionalInfos { get; set; }

        public ICollection<SyncCustomerSalesPerson> CustomerSalesPersons { get; set; }
        public ICollection<SyncCustomerUser> CustomerUsers { get; set; }
        public ICollection<SyncClothes> Clothes { get; set; }
        public ICollection<SyncClothesImage> ClothesImages { get; set; }
        public ICollection<SyncClothesScale> ClothesScales { get; set; }
        public ICollection<SyncClothesScaleSize> ClothesScaleSizes { get; set; }
        public ICollection<SyncRelatedClothes> RelatedClothes { get; set; }
        public ICollection<SyncOrder> Orders { get; set; }
        public ICollection<SyncOrderScale> OrderScales { get; set; }
        public ICollection<SyncOrderSize> OrderSizes { get; set; }
        public ICollection<SyncOrderStatus> OrderStatus { get; set; }
        public ICollection<SyncCustomerItemPrice> CustomerItemPrices { get; set; }
        public ICollection<SyncFit> Fits { get; set; }
        public ICollection<SyncInseam> Inseams { get; set; }
        public ICollection<SyncSizeGroup> SizeGroups { get; set; }
        public ICollection<SyncSize> Sizes { get; set; }

        public ICollection<SyncPermission> Permissions { get; set; }
        public ICollection<SyncRole> Roles { get; set; }
        public ICollection<SyncRolePermission> RolePermissions { get; set; }
        public ICollection<SyncExtraPermission> ExtraPermissions { get; set; }
        public ICollection<SyncBackgroundPicture> BackgroundPictures { get; set; }

        public ICollection<SyncTerm> Terms { get; set; }
        public ICollection<SyncShipVia> ShipVias { get; set; }
        public ICollection<SyncOrderTag> OrderTags { get; set; }
        public ICollection<SyncTableToDelete> TablesToDelete { get; set; }

        public int UserId { get; set; }
        public DateTime ServerDate { get; set; }
        public bool Next { get; set; }
        public bool Success { get; set; }
    }

    public class FileSync
    {        
        public ICollection<SyncFilesToSync> FilesToSync { get; set; }        
        public bool Success { get; set; }
    }

    public class PrintCart
    {
        public string PdfString { get; set; }
        public bool Success { get; set; }
    }

    public class PrintWrapper
    {
        public Guid OrderId { get; set; }
        public bool HideImage { get; set; }
        public string Message { get; set; }
    }

    public class SendMail
    {
        public string Email { get; set; }
        public int AccountId {get; set;}
        public string UserName {get;set;}
        public string Password { get; set; }     
        public int Criteria { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; }       
    }

    public class SyncUserName
    {
        public string UserName { get; set; }
    }
    public class SyncUserId
    {
        public int UserId { get; set; }
    }
    public class GenericResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
    }
    public class SyncLogin
    {      
        public string UserName { get; set; }      
        public string Password { get; set; }     
        public bool RememberMe { get; set; }
    }
    public class SyncRegistration
    {
        public string CompanyName { get; set; }       
        public string UserName { get; set; }      
        public string Password { get; set; }    
        public string FirstName { get; set; }   
        public string LastName { get; set; }     
        public string PhoneNumber { get; set; }       
        public string Email { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
    }

    public class SyncAccount
    {
        public int accountId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public byte[] password { get; set; }
        public int roleId { get; set; }
        public Nullable<bool> isLocal { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> lastLoginDate { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncCategory
    {
        public int categoryId { get; set; }
        public string name { get; set; }
        public int sortOrder { get; set; }
        public int parentId { get; set; }
        public bool isActive { get; set; }
        public bool isDelete { get; set; }
        public int CategoryLevel { get; set; }        
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }       
    }

    public class SyncCommunication
    {
        public int communicationId { get; set; }
        public string phone { get; set; }
        public string fax { get; set; }
        public string mobile { get; set; }
        public Nullable<int> accountId { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncAddress
    {
        public int addressId { get; set; }
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string pincode { get; set; }
        public int addressTypeId { get; set; }
        public int accountId { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncCompany
    {
        public int companyId { get; set; }
        public string name { get; set; }
        public int accountId { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }    

    public class SyncClothes
    {
        public int clothesId { get; set; }
        public string styleNumber { get; set; }
        public string color { get; set; }
        public Nullable<decimal> price { get; set; }
        public string clothesDescription { get; set; }
        public string tags { get; set; }
        public string adminNote { get; set; }
        public int categoryId { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<int> clearance { get; set; }
        public int brandId { get; set; }
        public int sortOrder { get; set; }
        public Nullable<System.DateTime> futureDeliveryDate { get; set; }
        public int sizeGroupId { get; set; }
        public Nullable<int> isSent { get; set; }
        public Nullable<decimal> productCost { get; set; }
        public Nullable<int> originalQty { get; set; }
        public Nullable<decimal> msrp { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<int> adjustQty { get; set; }
        public Nullable<System.DateTime> dateChanged { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
        public Nullable<decimal> DiscountedPrice { get; set; }
        public Nullable<decimal> DiscountedMSRP { get; set; }
    }
    
    public class SyncClothesImage
    {
        public int clothesImageId { get; set; }
        public int clothesId { get; set; }
        public string imageName { get; set; }
        public string imagePath { get; set; }
        public bool isActive { get; set; }
        public bool isDelete { get; set; }
        public Nullable<int> sortOrder { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }        
    }

    public class SyncClothesScale
    {
        public int clothesScaleId { get; set; }
        public string name { get; set; }
        public int clothesId { get; set; }
        public Nullable<int> fitId { get; set; }
        public Nullable<int> inseamId { get; set; }
        public Nullable<bool> isOpenSize { get; set; }
        public Nullable<int> invQty { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<bool> isLocal { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncClothesScaleSize
    {
        public int clothesScaleSizeId { get; set; }
        public int clothesScaleId { get; set; }
        public int sizeId { get; set; }
        public Nullable<int> quantity { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<bool> isLocal { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncRelatedClothes
    {
        public int relatedClothesId { get; set; }       
        public int clothesId { get; set; }
        public int relClothesId { get; set; }
        public int categoryId { get; set; }      
        public bool isActive { get; set; }
        public bool isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncFit
    {
        public int fitId { get; set; }
        public string name { get; set; }
        public bool isActive { get; set; }
        public int sortOrder { get; set; }
        public bool isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncInseam
    {
        public int inseamId { get; set; }
        public string name { get; set; }
        public bool isActive { get; set; }
        public bool isDelete { get; set; }
        public int sortOrder { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncSizeGroup
    {
        public int sizeGroupId { get; set; }
        public string name { get; set; }
        public int categoryId { get; set; }
        public bool isActive { get; set; }
        public bool isDelete { get; set; }
        public int sortOrder { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }        
    }

    public class SyncSize
    {
        public int sizeId { get; set; }
        public string name { get; set; }
        public int sizeGroupId { get; set; }
        public int categoryId { get; set; }
        public bool isActive { get; set; }
        public bool isDelete { get; set; }
        public int sortOrder { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }  
    }

    public partial class SyncCustomerItemPrice
    {
        public int customerItemPriceId { get; set; }
        public int accountId { get; set; }
        public int clothesId { get; set; }
        public Nullable<decimal> price { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncCustomerSalesPerson
    {
        public int customerSalesPersonId { get; set; }
        public int accountId { get; set; }
        public int salesPersonId { get; set; }
        public Nullable<int> isSalesPersonContact { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncCustomerUser
    {
        public int customerUserId { get; set; }
        public int accountId { get; set; }
        public int customerId { get; set; }
        public Nullable<int> isCustomerContact { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncOrder
    {
        public System.Guid orderId { get; set; }
        public string orderNumber { get; set; }
        public int accountId { get; set; }
        public Nullable<System.DateTime> completedOn { get; set; }
        public System.DateTime createdOn { get; set; }
        public Nullable<System.DateTime> packedOn { get; set; }
        public Nullable<System.DateTime> shippedOn { get; set; }
        public Nullable<System.DateTime> submittedOn { get; set; }
        public Nullable<decimal> grandTotal { get; set; }
        public Nullable<decimal> discount { get; set; }
        public Nullable<decimal> finalAmount { get; set; }
        public string note { get; set; }
        public Nullable<int> employeeId { get; set; }
        public Nullable<int> addressId { get; set; }
        public Nullable<int> communicationId { get; set; }
        public int statusId { get; set; }
        public Nullable<bool> confirmStatus { get; set; }
        public Nullable<int> originalQty { get; set; }
        public Nullable<int> packedQty { get; set; }
        public Nullable<System.Guid> parentOrderId { get; set; }
        public Nullable<int> label { get; set; }
        public Nullable<int> trackId { get; set; }
        public Nullable<bool> isSentToQuickBook { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
        public Nullable<decimal> shippingCost { get; set; }
        public Nullable<int> shipViaId { get; set; }
        public Nullable<int> termId { get; set; }
        public Nullable<int> tagId { get; set; }
    }

    public class SyncOrderScale
    {
        public System.Guid orderScaleId { get; set; }
        public System.Guid orderId { get; set; }
        public int clothesScaleId { get; set; }
        public Nullable<int> quantity { get; set; }
        public Nullable<int> packedQty { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
        public Nullable<bool> isConfirmed { get; set; }
        public Nullable<int> clothesId { get; set; }
    }

    public class SyncOrderSize
    {
        public System.Guid orderSizeId { get; set; }
        public System.Guid orderId { get; set; }
        public int clothesSizeId { get; set; }
        public Nullable<int> quantity { get; set; }
        public Nullable<int> packedQty { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
        public Nullable<bool> isConfirmed { get; set; }
        public Nullable<int> clothesId { get; set; }
    }

    public class SyncOrderStatus
    {
        public int orderStatusId { get; set; }
        public string status { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncPermission
    {
        public int permissionId { get; set; }
        public string permissionPage { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> forSuperAdmin { get; set; }
        public Nullable<bool> forAdmin { get; set; }
        public Nullable<bool> forWarehouse { get; set; }
        public Nullable<bool> forSalesPerson { get; set; }
        public Nullable<bool> forCustomer { get; set; }
        public string permissionName { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
        public Nullable<bool> isDelete { get; set; }
    }

    public class SyncRole
    {
        public int roleId { get; set; }
        public string roleName { get; set; }
        public string alias { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncRolePermission
    {
        public int rolePermissionId { get; set; }
        public int roleId { get; set; }
        public int permissionId { get; set; }
        public Nullable<bool> isPermission { get; set; }
        public Nullable<bool> canView { get; set; }
        public Nullable<bool> canEdit { get; set; }
        public Nullable<bool> canOrder { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncExtraPermission
    {
        public int extraPermissionId { get; set; }
        public int accountId { get; set; }
        public int permissionId { get; set; }
        public Nullable<bool> isPermission { get; set; }
        public Nullable<bool> canView { get; set; }
        public Nullable<bool> canEdit { get; set; }
        public Nullable<bool> canOrder { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncBackgroundPicture
    {
        public int pictureId { get; set; }
        public string picture { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
        public Nullable<int> sortOrder { get; set; }
        public string picture_iPhone6 { get; set; }
        public string picture_iPhone6s { get; set; }
        public string picture_iPadL { get; set; }
        public string picture_iPadP { get; set; }
        public string picture_iPadRL { get; set; }
        public string picture_iPadRP { get; set; }
    }

    public class SyncFilesToSync
    {
        public System.Guid fileId { get; set; }
        public string fileName { get; set; }        
        public string filePath { get; set; }
        public byte[] data { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<bool> syncChanged { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }    

    public class SyncAddressType
    {
        public int addressTypeId { get; set; }
        public string type { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncCustomerOptionalInfo
    {
        public int customerOptionalInfoId { get; set; }
        public string secondEmailId { get; set; }
        public byte[] secondPassword { get; set; }
        public string businessReseller { get; set; }
        public int customerType { get; set; }
        public Nullable<int> termId { get; set; }
        public int accountId { get; set; }
        public Nullable<int> shipViaId { get; set; }
        public Nullable<decimal> discount { get; set; }       
        public string displayName { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }

    }

    public class SyncTerm
    {
        public int termId { get; set; }
        public string name { get; set; }
        public bool isActive { get; set; }
        public int sortOrder { get; set; }
        public bool isDelete { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }        
    }

    public class SyncShipVia
    {        
        public int shipViaId { get; set; }
        public string name { get; set; }
        public bool isActive { get; set; }
        public int sortOrder { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
        public bool isDelete { get; set; }        
    }

    public class SyncOrderTag
    {
        public int orderTagId { get; set; }
        public string name { get; set; }
        public Nullable<bool> isDefault { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public Nullable<int> sortOrder { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }

    public class SyncTableToDelete
    {
        public System.Guid deleteId { get; set; }
        public string tableName { get; set; }
        public string tableKey { get; set; }
        public string tableValue { get; set; }
        public Nullable<bool> justOverride { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<System.DateTime> dateUpdated { get; set; }
    }    
}