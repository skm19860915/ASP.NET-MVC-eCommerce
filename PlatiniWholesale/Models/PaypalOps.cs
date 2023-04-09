using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using PayPal;
using PayPal.Api;

namespace Platini.Models
{
    public class PaypalOps
    {
        private string ClientId
        {
            get
            {
                return ConfigurationManager.AppSettings["PaypalClientId"];
            }
        }

        private string ClientSecret
        {
            get
            {
                return ConfigurationManager.AppSettings["PaypalClientSecret"];
            }
        }

        private Dictionary<string, string> Config
        {
            get
            {
                Dictionary<string, string> config = new Dictionary<string, string>();
                config.Add("mode", ConfigurationManager.AppSettings["PaypalMode"]);
                config.Add("connectionTimeout", "360000");
                return config;
            }
        }

        private string AccessToken
        {
            get
            {
                System.Net.ServicePointManager.Expect100Continue = true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                System.Net.ServicePointManager.DefaultConnectionLimit = 9999;
                string token = new PayPal.Api.OAuthTokenCredential(ClientId, ClientSecret, Config).GetAccessToken();
                return token;
            }
        }
        
        public PayPal.Api.APIContext Api
        {
            get
            {
               // System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                PayPal.Api.APIContext context = new PayPal.Api.APIContext(AccessToken);
                context.Config = Config;
                return context;
            }
        }

        public static string ModTest(string ccNum, bool pad)
        {
            string retCCNum = string.Empty;
            if (ccNum.Length > 13)
            {
                int checkDigit = 0;
                int.TryParse(ccNum.Last().ToString(), out checkDigit);
                string num = ccNum;
                var baseArr = num.Substring(0, ccNum.Length - 1).Reverse().Select(x => int.Parse(x.ToString()));
                var oddArr = baseArr.Where((item, index) => index % 2 != 0);
                var evenArr = baseArr.Where((item, index) => index % 2 == 0).Select(x => x * 2).Select(x => x > 9 ? x - 9 : x);
                int Sum = oddArr.Sum() + evenArr.Sum();
                if ((checkDigit + Sum) % 10 == 0)
                {
                    if (pad)
                        retCCNum = ccNum.Substring(ccNum.Length - 4).PadLeft(16, 'X');
                    else
                        retCCNum = ccNum;
                }
            }
            return retCCNum;
        }

        public PayPal.Api.CreditCard SetUpCard(string fname, string lname, string number, string cvv, int month, int year, string type)
        {
            PayPal.Api.CreditCard cc = null;
            number = ModTest(number, false);
            if (!string.IsNullOrEmpty(number))
            {
                cc = new PayPal.Api.CreditCard();
                cc.number = number;
                cc.cvv2 = cvv;
                cc.expire_month = month;
                cc.expire_year = year;
                cc.first_name = fname;
                cc.last_name = lname;
                cc.type = type;
            }
            return cc;
        }

        //public PayPal.Api.Address CheckAddress()
        //{
        //    PayPal.Api.Address address = null;
        //    //address.
        //    return address;
        //}
    }


    public class ErrorDetail
    {
        public string field { get; set; }
        public string issue { get; set; }
    }

    public class PaypalResponse
    {
        public string name { get; set; }
        public List<ErrorDetail> details { get; set; }
        public string message { get; set; }
        public string information_link { get; set; }
        public string debug_id { get; set; }
    }
}