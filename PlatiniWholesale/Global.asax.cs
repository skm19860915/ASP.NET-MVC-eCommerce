using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace Platini
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            //HttpApplication application = (HttpApplication)sender;
            //HttpContext context = application.Context;

            //string culture = null;
            //if (context.Request.UserLanguages != null && Request.UserLanguages.Length > 0)
            //{
            //    culture = Request.UserLanguages[0];
            //    Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
            //    Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
            //}
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            if (HttpContext.Current.User != null)
            {
                if (HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    if (HttpContext.Current.User.Identity is FormsIdentity)
                    {
                        FormsIdentity id = (FormsIdentity)HttpContext.Current.User.Identity;
                        FormsAuthenticationTicket ticket = id.Ticket;
                        string userData = ticket.UserData;
                        string[] roles = userData.Split('|');
                        HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(id, roles);
                    }
                }
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            try
            {
                var err = Server.GetLastError() as HttpException;
                if (err != null)
                {
                    string validationErrors = "";
                    var err2 = Server.GetLastError() as System.Data.Entity.Validation.DbEntityValidationException;
                    if (err2 != null)
                    {
                        foreach (var failure in err2.EntityValidationErrors)
                        {
                            foreach (var error in failure.ValidationErrors)
                            {
                                validationErrors += error.PropertyName + "  " + error.ErrorMessage;
                            }
                        }
                    }
                    this.Response.Clear();
                    this.Response.Write("Error: \n\n" + err.Message);
                    if (!string.IsNullOrEmpty(validationErrors))
                        this.Response.Write("Validation Error: \n\n" + validationErrors);
                    this.Response.Write("Stack Trace: \n" + err.StackTrace);
                    if (!string.IsNullOrEmpty(err.GetHtmlErrorMessage()))
                    {
                        this.Response.Write("HTML Error: \n\n" + err.WebEventCode + " " + err.GetHtmlErrorMessage());
                    }
                    if (!ReferenceEquals(err.InnerException, null))
                    {
                        this.Response.Write("Inner Error: \n\n" + err.InnerException.Message);
                        this.Response.Write("Inner Stack Trace: \n" + err.InnerException.StackTrace);
                    }
                    if (!ReferenceEquals(err.InnerException.InnerException, null))
                    {
                        this.Response.Write("Inner Error2: \n\n" + err.InnerException.InnerException.Message);
                        this.Response.Write("Inner Stack Trace2: \n" + err.InnerException.InnerException.StackTrace);
                    }
                }
                else
                {
                    string validationErrors = "";
                    var err2 = Server.GetLastError() as System.Data.Entity.Validation.DbEntityValidationException;
                    if (err2 != null)
                    {
                        foreach (var failure in err2.EntityValidationErrors)
                        {
                            foreach (var error in failure.ValidationErrors)
                            {
                                validationErrors += error.PropertyName + "  " + error.ErrorMessage;
                            }
                        }
                    }
                    this.Response.Clear();
                    this.Response.Write("Error: \n\n" + Server.GetLastError().Message);
                    if (!string.IsNullOrEmpty(validationErrors))
                        this.Response.Write("Validation Error: \n\n" + validationErrors);
                    this.Response.Write("Stack Trace: \n" + Server.GetLastError().StackTrace);
                    if (!ReferenceEquals(Server.GetLastError().InnerException, null))
                    {
                        this.Response.Write("Inner Error: \n\n" + Server.GetLastError().InnerException.Message);
                        this.Response.Write("Inner Stack Trace: \n" + Server.GetLastError().InnerException.StackTrace);
                    }
                    if (!ReferenceEquals(Server.GetLastError().InnerException.InnerException, null))
                    {
                        this.Response.Write("Inner Error2: \n\n" + Server.GetLastError().InnerException.InnerException.Message);
                        this.Response.Write("Inner Stack Trace2: \n" + Server.GetLastError().InnerException.InnerException.StackTrace);
                    }
                }
                this.Response.Flush();
                this.Response.Close();
                return;
            }
            catch { return; }
        }
    }
}