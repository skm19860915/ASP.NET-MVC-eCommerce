using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Platini.Areas.Common.Models
{
    public class RolesContainerClass
    {
        public int RoleId { get; set; }

        public IEnumerable<Platini.DB.Role> Roles { get; set; }

        public List<PermissionModel> rolesPermission { get; set; }
    }
}