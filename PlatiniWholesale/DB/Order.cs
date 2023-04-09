//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class Order
    {
        public Order()
        {
            this.Bags = new HashSet<Bag>();
            this.Boxes = new HashSet<Box>();
            this.Order1 = new HashSet<Order>();
            this.OrderScales = new HashSet<OrderScale>();
            this.OrderSizes = new HashSet<OrderSize>();
            this.Transactions = new HashSet<Transaction>();
        }
    
        public System.Guid OrderId { get; set; }
        public string OrderNumber { get; set; }
        public int AccountId { get; set; }
        public Nullable<System.DateTime> CompletedOn { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public Nullable<System.DateTime> PackedOn { get; set; }
        public Nullable<System.DateTime> ShippedOn { get; set; }
        public Nullable<System.DateTime> SubmittedOn { get; set; }
        public Nullable<decimal> GrandTotal { get; set; }
        public Nullable<decimal> Discount { get; set; }
        public Nullable<decimal> FinalAmount { get; set; }
        public string Note { get; set; }
        public Nullable<int> EmployeeId { get; set; }
        public Nullable<int> AddressId { get; set; }
        public Nullable<int> CommunicationId { get; set; }
        public int StatusId { get; set; }
        public Nullable<bool> ConfirmStatus { get; set; }
        public Nullable<int> OriginalQty { get; set; }
        public Nullable<int> PackedQty { get; set; }
        public Nullable<System.Guid> ParentOrderId { get; set; }
        public Nullable<int> Label { get; set; }
        public Nullable<int> TrackId { get; set; }
        public Nullable<bool> IsSentToQuickBook { get; set; }
        public Nullable<bool> IsDelete { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
        public Nullable<decimal> ShippingCost { get; set; }
        public Nullable<int> ShipViaId { get; set; }
        public Nullable<int> TermId { get; set; }
        public Nullable<int> TagId { get; set; }
    
        public virtual Account Account { get; set; }
        public virtual Account Account1 { get; set; }
        public virtual Address Address { get; set; }
        public virtual ICollection<Bag> Bags { get; set; }
        public virtual ICollection<Box> Boxes { get; set; }
        public virtual Communication Communication { get; set; }
        public virtual Term Term { get; set; }
        public virtual ICollection<Order> Order1 { get; set; }
        public virtual Order Order2 { get; set; }
        public virtual OrderTag OrderTag { get; set; }
        public virtual ICollection<OrderScale> OrderScales { get; set; }
        public virtual ShipVia ShipVia { get; set; }
        public virtual Track Track { get; set; }
        public virtual ICollection<OrderSize> OrderSizes { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}