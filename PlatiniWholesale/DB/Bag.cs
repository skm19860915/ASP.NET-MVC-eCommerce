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
    
    public partial class Bag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Dimension { get; set; }
        public string Weight { get; set; }
        public Nullable<int> TotalBags { get; set; }
        public System.Guid OrderId { get; set; }
    
        public virtual Order Order { get; set; }
    }
}