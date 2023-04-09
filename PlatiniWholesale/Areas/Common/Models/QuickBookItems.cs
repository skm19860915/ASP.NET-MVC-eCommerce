using System.Data;
using System.IO;
using SD = System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Web.UI.HtmlControls;
using System.Collections;
using Intuit.Ipp.Security;
using Intuit.Ipp.Core;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Data;
using Intuit.Ipp.QueryFilter;
using System.Data.SqlClient;
using Platini.DB;

namespace Platini.Areas.Common.Models
{
    public class QuickBookItems
    {
        #region QuickBook objects declaration
        static OAuthRequestValidator oauthValidator = new OAuthRequestValidator(System.Configuration.ConfigurationManager.AppSettings["accessToken"].ToString(), System.Configuration.ConfigurationManager.AppSettings["accessTokenSecret"].ToString(), System.Configuration.ConfigurationManager.AppSettings["consumerKey"].ToString(), System.Configuration.ConfigurationManager.AppSettings["consumerSecret"].ToString());
        static string appToken = System.Configuration.ConfigurationManager.AppSettings["appToken"].ToString();
        static string companyID = System.Configuration.ConfigurationManager.AppSettings["companyID"].ToString();
        static ServiceContext Objcontext = new ServiceContext(appToken, companyID, IntuitServicesType.QBO, oauthValidator);
        DataService service = new DataService(Objcontext); 
        #endregion
        private Entities db = new Entities();

        public QuickBookItems()
        {
        }

        /// <summary>
        /// get service category
        /// </summary>
        /// <returns></returns>
        public static Item getQuickIncomeAccount()
        {
            QueryService<Item> account = new QueryService<Item>(Objcontext);
            Item id = account.ExecuteIdsQuery("Select * From Item where Name='" + System.Configuration.ConfigurationManager.AppSettings["quickProductCategoryId"].ToString() + "'").Where(c => c.FullyQualifiedName == System.Configuration.ConfigurationManager.AppSettings["quickProductCategoryId"].ToString()).FirstOrDefault<Item>();
            return id;
        }

        public DataTable getCustomerDetails(string accountId)
        {
            string strQuery = string.Format("select a.*,t.Name from Accounts a left join CustomerOptionalInfoes coi on a.Id = coi.AccountId  left join Terms t on a.TermId=t.ID   where AccountID={1}", accountId);
            DataTable dt = db.Database.SqlQuery<DataTable>(strQuery).FirstOrDefault();
            return dt;
        }
    }
}