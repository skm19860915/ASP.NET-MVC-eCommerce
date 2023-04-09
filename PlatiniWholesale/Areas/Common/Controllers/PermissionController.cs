using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Platini.DB;
using MvcPaging;
using Platini.Areas.Common.Models;
using System.Text.RegularExpressions;

namespace Platini.Areas.Common.Controllers
{
    public class PermissionController : Controller
    {
        private Entities db = new Entities();
        private int defaultpageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPagesize"]);
        //
        // GET: /Admin/Permissions/

        public ActionResult Index(int? page)
        {
            //var permissions = db.Permissions.ToList();

            var roles = db.Roles.ToList();
            var allPermissionsId = db.RolesPermissions.ToList();
            var allPermissions = db.Permissions.ToList();

            List<Platini.Areas.Common.Models.Roles> rolesCollection = new List<Platini.Areas.Common.Models.Roles>();
            List<Platini.Areas.Common.Models.RolePermissions> rolePermissionsCollection = new List<Platini.Areas.Common.Models.RolePermissions>();

            foreach (var item in roles)
            {
                var permissionsId = allPermissionsId.Where(x => x.RoleId == item.RoleId).ToList();

                Platini.Areas.Common.Models.Roles role = new Platini.Areas.Common.Models.Roles();
                //Platini.Areas.Admin.Models.RolePermissions rp = new Platini.Areas.Admin.Models.RolePermissions();
                List<RolesPermission> permissionsIdByRole = new List<RolesPermission>();
                List<Permission> permissionsByRole = new List<Permission>();

                foreach (var p in permissionsId)
                {
                    //var permission = db.Permissions.Where(x => x.Id == p.PermissionId).SingleOrDefault();
                    permissionsIdByRole.Add(allPermissionsId.Where(x => x.PermissionId == p.PermissionId && x.RoleId == item.RoleId).SingleOrDefault());
                    permissionsByRole.Add(allPermissions.Where(x => x.PermissionId == p.PermissionId).SingleOrDefault());
                }

                role.RoleId = item.RoleId;
                role.RoleName = item.RoleName;
                role.RolePermissions = new Platini.Areas.Common.Models.RolePermissions();
                role.RolePermissions.RoleId = item.RoleId;
                role.RolePermissions.PermissionsList = permissionsByRole;
                role.RolePermissions.RolesPermisionList = permissionsIdByRole;

                rolesCollection.Add(role);
            }

            if (page == null)
                page = 1;
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            Platini.Models.SiteConfiguration.Mode = Platini.Models.ModeEnum.Edit.ToString();
            return View(rolesCollection.ToPagedList(currentPageIndex, defaultpageSize));
        }

        
        //GET
        public ActionResult Edit(int? Id)
        {
            RolesContainerClass rolesContainerClass = new RolesContainerClass();
            if (Id.HasValue)
            {
                var roles = db.Roles.ToList();
                rolesContainerClass.Roles = roles;
                rolesContainerClass.RoleId = Id.Value;
                List<PermissionModel> permissionList = new List<PermissionModel>();
                var allPermissions = db.Permissions.ToList();
                foreach (var item in allPermissions)
                {
                    PermissionModel per = new PermissionModel();
                    per.RoleId = Id.Value;
                    per.PermissionId = item.PermissionId;
                    per.PermissionPage = item.PermissionPage;
                    foreach (var rp in item.RolesPermissions)
                    {
                        if (rp.RoleId == Id.Value)
                        {
                            per.RolePermission = rp;
                            break;
                        }
                    }
                    permissionList.Add(per);
                }
                rolesContainerClass.rolesPermission = permissionList;

            }
            else
            {
                var roles = db.Roles.ToList();
                rolesContainerClass.Roles = roles;
                rolesContainerClass.rolesPermission = new List<PermissionModel>();//db.RolesPermissions.Where(x => x.RoleId.Equals(1)).ToList();
            }
            return View(rolesContainerClass);
        }

        //For Partial View - Grid View of Permissions for Roles
        public PartialViewResult FilterRolePermissions(int? Id)
        {
            var allPermissionsId = db.RolesPermissions.ToList();

            if (Id.HasValue)
            {
                allPermissionsId = allPermissionsId.Where(x => x.RoleId.Equals(Id)).ToList();
            }

            return PartialView("_PermissionsTable", allPermissionsId);
        }

        [HttpPost]
        public ActionResult Studentl(string[] Check, string[] Uncheck)
        {
            char[] b = {};
            if (Check != null && Uncheck != null)
            {
                foreach (string str in Check)
                {
                    //string[] s = Regex.Split(str, @"[a-z]");
                    string[] s = str.Split('-');
                    int num1, num2, num3;
                    int.TryParse(s[0], out num1);
                    int.TryParse(s[1], out num2);
                    int.TryParse(s[2], out num3);

                    RolesPermission rp = db.RolesPermissions.Where(x => x.RoleId == num1 && x.PermissionId == num3).SingleOrDefault();

                    if (rp != null)
                    {
                        if (num2 == 1)
                        {
                            rp.CanView = true;
                            db.SaveChanges();
                        }
                        else if (num2 == 2)
                        {
                            rp.CanEdit = true;
                            db.SaveChanges();
                        }
                        else if (num2 == 3)
                        {
                            rp.CanOrder = true;
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        RolesPermission newrp = new RolesPermission();
                        newrp.RoleId = num1;
                        newrp.PermissionId = num3;
                        if (num2 == 1)
                        {
                            newrp.CanView = true;
                            newrp.CanEdit = false;
                            newrp.CanOrder = false;
                        }
                        else if (num2 == 2)
                        {
                            newrp.CanEdit = true;
                            newrp.CanView = false;
                            newrp.CanOrder = false;
                        }
                        else if (num2 == 3)
                        {
                            newrp.CanOrder = true;
                            newrp.CanView = false;
                            newrp.CanEdit = false;
                        }
                        db.RolesPermissions.Add(newrp);
                        db.SaveChanges();
                    }
                }

                foreach (string str in Uncheck)
                {
                    //string[] s = Regex.Split(str, @"[a-z]");
                    string[] s = str.Split('-');
                    int num1, num2, num3;
                    int.TryParse(s[0], out num1);
                    int.TryParse(s[1], out num2);
                    int.TryParse(s[2], out num3);

                    RolesPermission rp = db.RolesPermissions.Where(x => x.RoleId == num1 && x.PermissionId == num3).SingleOrDefault();

                    if (rp != null)
                    {
                        if (num2 == 1)
                        {
                            rp.CanView = false;
                            db.SaveChanges();
                        }
                        else if (num2 == 2)
                        {
                            rp.CanEdit = false;
                            db.SaveChanges();
                        }
                        else if (num2 == 3)
                        {
                            rp.CanOrder = false;
                            db.SaveChanges();
                        }
                    }
                }
            }
            else
            {
                if (Check == null)
                {
                    foreach (string str in Uncheck)
                    {
                        //string[] s = Regex.Split(str, @"[a-z]");
                        string[] s = str.Split('-');
                        int num1, num2, num3;
                        int.TryParse(s[0], out num1);
                        int.TryParse(s[1], out num2);
                        int.TryParse(s[2], out num3);

                        RolesPermission rp = db.RolesPermissions.Where(x => x.RoleId == num1 && x.PermissionId == num3).SingleOrDefault();

                        if (rp != null)
                        {
                            if (num2 == 1)
                            {
                                rp.CanView = false;
                                db.SaveChanges();
                            }
                            else if (num2 == 2)
                            {
                                rp.CanEdit = false;
                                db.SaveChanges();
                            }
                            else if (num2 == 3)
                            {
                                rp.CanOrder = false;
                                db.SaveChanges();
                            }
                        }
                    }
                }
                else
                {
                    foreach (string str in Check)
                    {
                        //string[] s = Regex.Split(str, @"[a-z]");
                        string[] s = str.Split('-');
                        int num1, num2, num3;
                        int.TryParse(s[0], out num1);
                        int.TryParse(s[1], out num2);
                        int.TryParse(s[2], out num3);

                        RolesPermission rp = db.RolesPermissions.Where(x => x.RoleId == num1 && x.PermissionId == num3).SingleOrDefault();

                        if (rp != null)
                        {
                            if (num2 == 1)
                            {
                                rp.CanView = true;
                                db.SaveChanges();
                            }
                            else if (num2 == 2)
                            {
                                rp.CanEdit = true;
                                db.SaveChanges();
                            }
                            else if (num2 == 3)
                            {
                                rp.CanOrder = true;
                                db.SaveChanges();
                            }
                        }
                        else
                        {
                            RolesPermission newrp = new RolesPermission();
                            newrp.RoleId = num1;
                            newrp.PermissionId = num3;
                            if (num2 == 1)
                            {
                                newrp.CanView = true;
                                newrp.CanEdit = false;
                                newrp.CanOrder = false;
                            }
                            else if (num2 == 2)
                            {
                                newrp.CanEdit = true;
                                newrp.CanView = false;
                                newrp.CanOrder = false;
                            }
                            else if (num2 == 3)
                            {
                                newrp.CanOrder = true;
                                newrp.CanView = false;
                                newrp.CanEdit = false;
                            }
                            db.RolesPermissions.Add(newrp);
                            db.SaveChanges();
                        }
                    }
                }
            }

            return Json(new { success = Check });
        }


        public PartialViewResult GetPermissionsWithRolePermission(RolesContainerClass role)
        {
            List<PermissionModel> permissionList = new List<PermissionModel>();
            if (role.RoleId > 0)
            {
                var allPermissions = db.Permissions.ToList();
                foreach (var item in allPermissions)
                {
                    PermissionModel per = new PermissionModel();
                    per.RoleId = role.RoleId;
                    per.PermissionId = item.PermissionId;
                    per.PermissionPage = item.PermissionPage;
                    foreach (var rp in item.RolesPermissions)
                    {
                        if (rp.RoleId == role.RoleId)
                        {
                            per.RolePermission = rp;
                            break;
                        }
                    }
                    permissionList.Add(per);
                }
            }
            else
            {
            }

            return PartialView("_PermissionsTable", permissionList);
        }

    }
}

