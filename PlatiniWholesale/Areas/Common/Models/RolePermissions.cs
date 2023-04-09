using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Platini.DB;

namespace Platini.Areas.Common.Models
{
    public class RolePermissions
    {
        public int RoleId { get; set; }

        public List<Permission> PermissionsList { get; set; }

        public List<RolesPermission> RolesPermisionList { get; set; }
    }
}