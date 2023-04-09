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
    
    public partial class ExtraPermission
    {
        public int ExtraPermissionId { get; set; }
        public int AccountId { get; set; }
        public int PermissionId { get; set; }
        public Nullable<bool> IsPermission { get; set; }
        public Nullable<bool> CanView { get; set; }
        public Nullable<bool> CanEdit { get; set; }
        public Nullable<bool> CanOrder { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsDelete { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
    
        public virtual Account Account { get; set; }
        public virtual Permission Permission { get; set; }
    }
}
