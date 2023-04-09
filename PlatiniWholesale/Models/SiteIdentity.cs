using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Configuration;
using System.Security.Principal;
using System.Collections.Generic;
using System.Web.Security;
using System.Web;

namespace Platini.Models
{
    public class SiteIdentity
    {
        private static string _UserId = string.Empty;
        private static string _Name = string.Empty;
        private static string _Email = string.Empty;
        private static string _UserName = string.Empty;
        private static string _IsAdmin = "FALSE";
        private static string _Roles = string.Empty;
        private static string _Type = string.Empty;

        public static string UserId
        {
            get
            {
                //if (_UserId == string.Empty)
                    Load();
                return _UserId;
            }
            set
            {
                _UserId = value;
            }
        }

        public static string Name
        {
            get
            {
                //if (_UserId == string.Empty)
                    Load();
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }

        public static string Email
        {
            get
            {
                //if (_UserId == string.Empty)
                    Load();
                return _Email;
            }
            set
            {
                _Email = value;
            }
        }

        public static string UserName
        {
            get
            {
                //if (_UserId == string.Empty)
                Load();
                return _UserName;
            }
            set
            {
                _UserName = value;
            }
        }

        public static string IsAdmin
        {
            get
            {
                //if (_UserId == string.Empty)
                    Load();
                return _IsAdmin;
            }
            set
            {
                _IsAdmin = value;
            }
        }

        public static string Roles
        {
            get
            {
                //if (_UserId == string.Empty)
                    Load();
                return _Roles;
            }
            set
            {
                _Roles = value;
            }
        }

        public static string Type
        {
            get
            {
                //if (_UserId == string.Empty)
                    Load();
                return _Type;
            }
            set
            {
                _Type = value;
            }
        }

        public SiteIdentity()
        {
            _UserId = string.Empty;
        }

        private static void Load()
        {
            FormsIdentity ident = HttpContext.Current.User.Identity as FormsIdentity;
            if (ident != null)
            {
                FormsAuthenticationTicket ticket = ident.Ticket;
                string userDataString = ticket == null ? string.Empty : ticket.UserData;
                string[] userDataPieces = string.IsNullOrEmpty(userDataString) == true ? new string[] { "0", "", "", "FALSE", "", "0" } : userDataString.Split("|".ToCharArray());
                _UserId = string.IsNullOrEmpty(userDataPieces[0]) == true ? string.Empty : userDataPieces[0];
                _Name = string.IsNullOrEmpty(userDataPieces[1]) == true ? string.Empty : userDataPieces[1];
                _Email = string.IsNullOrEmpty(userDataPieces[2]) == true ? string.Empty : userDataPieces[2];
                _UserName = string.IsNullOrEmpty(userDataPieces[3]) == true ? string.Empty : userDataPieces[3];
                _IsAdmin = string.IsNullOrEmpty(userDataPieces[4]) == true ? string.Empty : userDataPieces[4];
                _Roles = string.IsNullOrEmpty(userDataPieces[5]) == true ? string.Empty : userDataPieces[5];                
                if(userDataPieces.Length> 6)
                    _Type = string.IsNullOrEmpty(userDataPieces[6]) == true ? string.Empty : userDataPieces[6];
            }
            else
            {
                _UserId = string.Empty;
                _Name = string.Empty;
                _Email = string.Empty;
                _UserName = string.Empty;
                _IsAdmin = "FALSE";
                _Roles = string.Empty;
                _Type = string.Empty;
            }
        }

        public static int RoleId()
        {
            int roleId = 0;
            if (!string.IsNullOrEmpty(Roles))
            {
                RolesEnum roleEnum;
                bool check = Enum.TryParse(Roles, true, out roleEnum);
                if (check)
                    roleId = (int)roleEnum;
            }
            return roleId;
        }
    }
}

