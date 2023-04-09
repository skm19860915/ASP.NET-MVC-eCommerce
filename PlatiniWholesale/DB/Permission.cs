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
    
    public partial class Permission
    {
        public Permission()
        {
            this.ExtraPermissions = new HashSet<ExtraPermission>();
            this.RolesPermissions = new HashSet<RolesPermission>();
        }
    
        public int PermissionId { get; set; }
        public string PermissionPage { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> ForSuperAdmin { get; set; }
        public Nullable<bool> ForAdmin { get; set; }
        public Nullable<bool> ForWarehouse { get; set; }
        public Nullable<bool> ForSalesPerson { get; set; }
        public Nullable<bool> ForCustomer { get; set; }
        public string PermissionName { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateUpdated { get; set; }
        public Nullable<bool> IsDelete { get; set; }
    
        public virtual ICollection<ExtraPermission> ExtraPermissions { get; set; }
        public virtual ICollection<RolesPermission> RolesPermissions { get; set; }
    }
}
