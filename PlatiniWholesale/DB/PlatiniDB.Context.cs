﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Platini.DB
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Entities : DbContext
    {
        public Entities()
            : base("name=Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<AddressType> AddressTypes { get; set; }
        public DbSet<BackgroundPicture> BackgroundPictures { get; set; }
        public DbSet<Bag> Bags { get; set; }
        public DbSet<Box> Boxes { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cloth> Clothes { get; set; }
        public DbSet<ClothesImage> ClothesImages { get; set; }
        public DbSet<ClothesScale> ClothesScales { get; set; }
        public DbSet<ClothesScaleSize> ClothesScaleSizes { get; set; }
        public DbSet<Communication> Communications { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CreditCardDetail> CreditCardDetails { get; set; }
        public DbSet<CustomerItemPrice> CustomerItemPrices { get; set; }
        public DbSet<CustomerOptionalInfo> CustomerOptionalInfoes { get; set; }
        public DbSet<CustomerSalesPerson> CustomerSalesPersons { get; set; }
        public DbSet<CustomerUser> CustomerUsers { get; set; }
        public DbSet<ExtraPermission> ExtraPermissions { get; set; }
        public DbSet<Fit> Fits { get; set; }
        public DbSet<Inseam> Inseams { get; set; }
        public DbSet<LoginLog> LoginLogs { get; set; }
        public DbSet<NewsLetterSignUp> NewsLetterSignUps { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLog> OrderLogs { get; set; }
        public DbSet<OrderScale> OrderScales { get; set; }
        public DbSet<OrderSize> OrderSizes { get; set; }
        public DbSet<OrderStatu> OrderStatus { get; set; }
        public DbSet<OrderTag> OrderTags { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<QuickBookFailureRecord> QuickBookFailureRecords { get; set; }
        public DbSet<RelatedClothe> RelatedClothes { get; set; }
        public DbSet<RelatedColor> RelatedColors { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolesPermission> RolesPermissions { get; set; }
        public DbSet<ScaleSizeQuantity> ScaleSizeQuantities { get; set; }
        public DbSet<ShipVia> ShipVias { get; set; }
        public DbSet<ShowRoomImage> ShowRoomImages { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<SizeGroup> SizeGroups { get; set; }
        public DbSet<TableToDelete> TableToDeletes { get; set; }
        public DbSet<TagMapping> TagMappings { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<VisitorLog> VisitorLogs { get; set; }
        public DbSet<WebsiteSetting> WebsiteSettings { get; set; }
        public DbSet<CustomerOptionalInfo1> CustomerOptionalInfo1 { get; set; }
    }
}
