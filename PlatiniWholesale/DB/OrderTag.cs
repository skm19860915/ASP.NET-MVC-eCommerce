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
    
    public partial class OrderTag
    {
        public OrderTag()
        {
            this.Orders = new HashSet<Order>();
        }
    
        public int OrderTagId { get; set; }
        public string Name { get; set; }
        public Nullable<bool> IsDefault { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsDelete { get; set; }
        public Nullable<int> SortOrder { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
    
        public virtual ICollection<Order> Orders { get; set; }
    }
}