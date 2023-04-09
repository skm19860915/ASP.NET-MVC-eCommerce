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
    
    public partial class Track
    {
        public Track()
        {
            this.Orders = new HashSet<Order>();
        }
    
        public int Id { get; set; }
        public string TrackingNumber { get; set; }
        public Nullable<decimal> ShippingCost { get; set; }
        public int ShipViaId { get; set; }
    
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ShipVia ShipVia { get; set; }
    }
}
