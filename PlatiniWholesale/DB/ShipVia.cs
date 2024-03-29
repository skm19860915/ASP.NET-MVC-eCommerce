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
    
    public partial class ShipVia
    {
        public ShipVia()
        {
            this.CustomerOptionalInfoes = new HashSet<CustomerOptionalInfo>();
            this.Orders = new HashSet<Order>();
            this.Tracks = new HashSet<Track>();
        }
    
        public int ShipViaId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
        public bool IsDelete { get; set; }
    
        public virtual ICollection<CustomerOptionalInfo> CustomerOptionalInfoes { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Track> Tracks { get; set; }
    }
}
