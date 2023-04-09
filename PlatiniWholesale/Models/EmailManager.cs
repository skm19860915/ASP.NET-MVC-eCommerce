using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace Platini.Models
{
    public class EmailManager
    {
        public static bool SendEmail(string subject, string Messagebody, string attachmentpath, string MailFrom, string mailto, bool iscc, string mailcc)
        {
            bool success = false;
            if (HttpContext.Current.Request.Url.AbsoluteUri.Contains("keyideasglobal") | HttpContext.Current.Request.Url.AbsoluteUri.Contains("localhost:"))
            {
                success = SendSMTPServerEmail(subject, Messagebody, attachmentpath, MailFrom, mailto, iscc, mailcc);
            }
            else
            {
                success = SendRelayServerEmail(subject, Messagebody, attachmentpath, MailFrom, mailto, iscc, mailcc);
            }
            return success;
        }

        public static bool SendLineSheet(string subject, string MessageBody, byte[] pdf, string MailFrom, string[] MailTo)
        {
            MessageBody = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"] + "OrderEmail.html");
            SmtpClient mySmtpClient;
            if (HttpContext.Current.Request.Url.AbsoluteUri.Contains("keyideasglobal") | HttpContext.Current.Request.Url.AbsoluteUri.Contains("localhost:"))
            {
                mySmtpClient = new SmtpClient();
                mySmtpClient.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SMTPUserName"], ConfigurationManager.AppSettings["SMTPPassword"]);
                mySmtpClient.Host = ConfigurationManager.AppSettings["SMTPServer"];
                mySmtpClient.Port = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPPort"]);
                mySmtpClient.EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSSL"]);
                mySmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            }
            else
            {
                mySmtpClient = new SmtpClient(ConfigurationManager.AppSettings["RelyServer"].ToString());
                mySmtpClient.Timeout = 10000000;
                mySmtpClient.UseDefaultCredentials = false;
            }
            MailMessage myMail = new MailMessage();
            MailAddress myFrom = new MailAddress(MailFrom, MailFrom);
            if (pdf != null)
            {
                Attachment attachment = new System.Net.Mail.Attachment(new MemoryStream(pdf), "LineSheet.pdf");
                myMail.Attachments.Add(attachment);
            }
            myMail.IsBodyHtml = true;
            myMail.BodyEncoding = System.Text.Encoding.UTF8;

            foreach (string emladd in MailTo)
            {
                if (!string.IsNullOrEmpty(emladd))
                    myMail.To.Add(emladd);
            }
            myMail.From = myFrom;
            myMail.Subject = subject;
            myMail.Body = MessageBody;
            try
            {
                mySmtpClient.Send(myMail);
            }
            catch
            {
                return false;
            }
            finally
            {
                myMail.Dispose();
            }
            return true;
        }

        public static bool SendRelayServerEmail(string subject, string Messagebody, string attachmentpath, string MailFrom, string mailto, bool iscc, string mailcc)
        {
            bool success = false;
            SmtpClient SmtpServer = new SmtpClient(ConfigurationManager.AppSettings["RelyServer"].ToString());
            SmtpServer.UseDefaultCredentials = false;
            MailMessage mail = new MailMessage();

            mail.From = new MailAddress(MailFrom);
            mail.Subject = subject;
            mail.Body = Messagebody;
            mail.IsBodyHtml = true;
            if ((attachmentpath.Length > 0))
            {
                System.Net.Mail.Attachment attachment = null;
                attachment = new System.Net.Mail.Attachment(attachmentpath);
                mail.Attachments.Add(attachment);
            }

            foreach (string emladd in mailto.Split(','))
            {
                if (!string.IsNullOrEmpty(emladd))
                    mail.To.Add(emladd);
            }

            //mail.To.Add(mailto)
            if (iscc)
            {
                mail.CC.Add(mailcc);
            }
            try
            {
                SmtpServer.Timeout = 10000000;
                SmtpServer.Send(mail);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                mail.Dispose();
            }
            return success;
        }

        public static bool SendSMTPServerEmail(string subject, string Messagebody, string attachmentpath, string MailFrom, string mailto, bool iscc, string mailcc)
        {
            bool success = false;
            SmtpClient mySmtpClient = new SmtpClient();
            MailMessage myMail = new MailMessage();
            MailAddress myFrom = new MailAddress(MailFrom, MailFrom);
            // Dim myTo As New MailAddress(mailto)

            System.Net.Mail.Attachment attachment = null;

            if (!string.IsNullOrEmpty(attachmentpath))
            {
                attachment = new System.Net.Mail.Attachment(attachmentpath);
                myMail.Attachments.Add(attachment);
            }

            myMail.IsBodyHtml = true;
            myMail.BodyEncoding = System.Text.Encoding.UTF8;

            foreach (string emladd in mailto.Split(','))
            {
                if (!string.IsNullOrEmpty(emladd))
                    myMail.To.Add(emladd);
            }
            // myMail.To.Add(myTo)

            myMail.From = myFrom;
            myMail.Subject = subject;
            myMail.Body = Messagebody;
            if (iscc)
            {
                myMail.CC.Add(mailcc);
            }

            mySmtpClient.UseDefaultCredentials = false;
            mySmtpClient.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SMTPUserName"], ConfigurationManager.AppSettings["SMTPPassword"]);
            mySmtpClient.Host = ConfigurationManager.AppSettings["SMTPServer"];
            mySmtpClient.Port = Convert.ToInt32(ConfigurationManager.AppSettings["SMTPPort"]);
            mySmtpClient.EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSSL"]);
            mySmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            try
            {
                mySmtpClient.Send(myMail);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                myMail.Dispose();
            }
            return success;
        }

        public static bool SendEmail1(string subject, string Messagebody, string MailFrom, string MailTo)
        {
            string msg = null;
            SmtpClient mySmtpClient = new SmtpClient();
            MailMessage myMail = new MailMessage();
            MailAddress myFrom = new MailAddress(MailFrom, MailFrom);

            foreach (string emladd in MailTo.Split(','))
            {
                if (!string.IsNullOrEmpty(emladd))
                    myMail.To.Add(emladd);
            }

            myMail.BodyEncoding = System.Text.Encoding.UTF8;

            //myMail.To.Add(myTo)

            myMail.From = myFrom;
            myMail.Subject = subject;
            myMail.Body = Messagebody;
            myMail.IsBodyHtml = true;

            mySmtpClient.EnableSsl = true;
            mySmtpClient.UseDefaultCredentials = false;
            mySmtpClient.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SMTPEmail"], ConfigurationManager.AppSettings["SMTPPassword"]);
            mySmtpClient.Host = ConfigurationManager.AppSettings["SMTP"];
            mySmtpClient.Port = 587;
            mySmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            try
            {
                mySmtpClient.Send(myMail);
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return true;
        }

        public static bool SendEmail2(string subject, string Messagebody, bool ishtmlbody, string MailFrom, string MailTo)
        {
            bool sucess = false;
            SmtpClient SmtpServer = new SmtpClient(ConfigurationManager.AppSettings["RelyServer"].ToString());
            SmtpServer.UseDefaultCredentials = false;
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(MailFrom);
            mail.Subject = subject;
            mail.IsBodyHtml = true;
            mail.Body = Messagebody;

            foreach (string emladd in MailTo.Split(','))
            {
                if (!string.IsNullOrEmpty(emladd))
                    mail.To.Add(emladd);
            }
            //mail.To.Add(MailTo)

            try
            {
                SmtpServer.Timeout = 0;
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;

            }
            finally
            {
                mail.Dispose();
            }



            return sucess;
        }

        public static string GetResponse(string Origin_Link)
        {
            string result = null;
            HttpWebResponse response = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Origin_Link);
                response = (HttpWebResponse)request.GetResponse();
                Encoding responseEncoding = Encoding.GetEncoding(response.CharacterSet);
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), responseEncoding))
                {                   
                    result = sr.ReadToEnd();
                    if(result.Contains("<%baseurl%>"))
                    {
                        result= result.Replace("<%baseurl%>", ConfigurationManager.AppSettings["BaseUrl"]);
                    }
                }
            }
            catch (Exception wexc)
            {
                throw (wexc);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
            return result;
        }

        public static bool SendWelcomeEmail(string Email, string CompanyName)
        {
            bool check = false;
            try
            {
                string message = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"].ToString() + "RegisterMail.html");
                message = message.Replace("<%CompanyName%>", CompanyName);
                message = message.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"].ToString());
                check = SendEmail("Platini Cougar Registration", message, "", ConfigurationManager.AppSettings["SMTPEmail"].ToString(), Email, false, ConfigurationManager.AppSettings["SMTPCC"].ToString());
            }
            catch
            {
                check = false;
            }
            return check;
        }

        public static bool SendForgotEmail(string email, string password)
        {
            bool check = false;
            try
            {
                string message = string.Format("Your password is : {0}   <br /><br /> PLATINI JEANS CO. <br /> Paramount, CA", password);
                check = SendEmail("Platini Jeans Password", message, "", ConfigurationManager.AppSettings["SMTPEmail"].ToString(), email, false, ConfigurationManager.AppSettings["SMTPCC"].ToString());
            }
            catch
            {
                check = false;
            }
            return check;
        }


        public static bool SendForgotPasswordEmail(string Email, string UserName, string Password)
        {
            bool check = false;
            try
            {
                string message = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"].ToString() + "ForgotPassword.html");
                message = message.Replace("<%UserName%>", UserName);
                message = message.Replace("<%Password%>", Password);
                message = message.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"].ToString());
                check = SendEmail("Platini Jeans Password", message, "", ConfigurationManager.AppSettings["SMTPEmail"].ToString(), Email, false, string.Empty);
            }
            catch
            {
                check = false;
            }
            return check;

        }

        public static bool SendAdminEmail(string Company, string Name, string Phone, string Email, string AccountID, string city)
        {
            bool check = false;
            try
            {
                string sMessage = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"].ToString() + "ResendActivationMail.html");

                sMessage = sMessage.Replace("<%Name%>", Name);
                sMessage = sMessage.Replace("<%CompanyName%>", Company);
                sMessage = sMessage.Replace("<%Email%>", Email);

                sMessage = sMessage.Replace("<%Address%>", city);
                sMessage = sMessage.Replace("<%Phone%>", Phone);

                sMessage = sMessage.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"]);
                sMessage = sMessage.Replace("<%ActivateLink%>", ConfigurationManager.AppSettings["baseurl"] + "Home/Index?ActivateAccountId=" + AccountID);
                sMessage = sMessage.Replace("<%DeActivateLink%>", ConfigurationManager.AppSettings["baseurl"] + "Home/Index?DeActivateAccountId=" + AccountID);

                if (HttpContext.Current.Request.Url.AbsoluteUri.ToLower().Contains("platinijeans"))
                    check = SendEmail("Platini Website Registration", sMessage, "", ConfigurationManager.AppSettings["SMTPEmail"], "sales@platinijeans.com", true, "pulsemicro@gmail.com");
                else
                    check = SendEmail("Platini Cougar Website Registration", sMessage, "", ConfigurationManager.AppSettings["SMTPEmail"], "pulsemicro@gmail.com", false, "");//pulsemicro@gmail.com

            }
            catch
            {
                check = false;
            }
            return check;
        }

        public static bool SendWelcomeEmailCustomer(string Email, string ActiveAccountId, string UserName, string password)
        {
            bool check = false;
            try
            {
                string sMessage = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"] + "Customer-Activation.html");
                sMessage = sMessage.Replace("<%UserName%>", UserName);
                sMessage = sMessage.Replace("<%password%>", password);

                sMessage = sMessage.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"]);
                check = SendEmail("Platini Cougar Registration", sMessage, "", ConfigurationManager.AppSettings["SMTPEmail"].ToString(), Email, false, "pulsemicro@gmail.com");//sales@platinijeans.com
            }
            catch
            {
                check = false;
            }
            return check;
        }

        public static bool SendActivationEmail(string Email, string _CompanyName)
        {
            bool check = false;
            try
            {
                string sMessage = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"] + "ActivationEmail.html");
                sMessage = sMessage.Replace("<%CompanyName%>", _CompanyName).Replace("&lt;%CompanyName%&gt;", _CompanyName);
                sMessage = sMessage.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"]);

                check = SendEmail("Platini Cougar Activation", sMessage, "", ConfigurationManager.AppSettings["SMTPEmail"], Email, false, "pulsemicro@gmail.com");
            }
            catch
            {
                check = false;
            }
            return check;
        }

        public static bool SendWelcomeEmail(string Email, string company, string UN, string password)
        {
            bool check = false;
            try
            {
                string sMessage = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"] + "ActivationMail.html");

                sMessage = sMessage.Replace("<%CompanyName%>", company);

                sMessage = sMessage.Replace("<%UserName%>", UN);
                sMessage = sMessage.Replace("<%Password%>", password);
                sMessage = sMessage.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"]);

                check = EmailManager.SendEmail("Platini Cougar Registration", sMessage, "", ConfigurationManager.AppSettings["SMTPEmail"], Email, true, ""); //'sales@platinijeans.com
            }
            catch
            {
                check = false;
            }
            return check;

        }

        public static bool SendOrderEmail(Guid Id, string OrderID, string UserId, string name, string SalespersonEmail)
        {
            bool check = false;
            try
            {
                string msgSub = "Platini Cougar - New Order " + OrderID;
                string sMessage = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"] + "Order.html");

                sMessage = sMessage.Replace("<%Orderno%>", OrderID);
                sMessage = sMessage.Replace("<%Custname%>", name);

                string orderURL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/PrintCart/" + Id;
                //orderURL = String.Format(orderURL, OrderID, UID)
                //orderURL = orderURL.ToString().Replace("UAccountid=0", "UAccountid=" + IsAccount)

                sMessage = sMessage.Replace("<%OrderLink%>", orderURL);
                sMessage = sMessage.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"]);
                string mailToemails = ConfigurationManager.AppSettings["mailToemails"];
                bool ISCC = !string.IsNullOrEmpty(SalespersonEmail);
                string replayMail = ConfigurationManager.AppSettings["SMTPEmail"].ToString();
                if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.SalesPerson.ToString())
                    replayMail = SiteIdentity.UserName;
                check = SendEmail(msgSub, sMessage, "", replayMail, mailToemails, ISCC, SalespersonEmail);
            }
            catch
            {
                check = false;
            }
            return check;
        }

        public static bool SendOrderEmailToCustomer(Guid Id, string OrderID, string CustEmail, string AccountId)
        {
            bool check = false;
            try
            {
                string msgSub = "Platini Jeans - Order Submited  " + OrderID;
                string sMessage = GetResponse(ConfigurationManager.AppSettings["TEMPLATELOC"] + "Order-Customer.html");

                sMessage = sMessage.Replace("<%Orderno%>", OrderID);
                string orderURL = ConfigurationManager.AppSettings["BaseUrl"] + "Home/PrintCart/" + Id;
                //orderURL = String.Format(orderURL, OrderID, AccountId)
                //orderURL = orderURL.ToString().Replace("UAccountid=0", "UAccountid=" + AccountId)
                sMessage = sMessage.Replace("<%OrderLink%>", orderURL);
                sMessage = sMessage.Replace("<%baseurl%>", ConfigurationManager.AppSettings["baseurl"]);
                string replayMail = ConfigurationManager.AppSettings["SMTPEmail"].ToString();
                if (SiteIdentity.Roles == RolesEnum.Customer.ToString() || SiteIdentity.Roles == RolesEnum.SalesPerson.ToString())
                    replayMail = SiteIdentity.UserName;
                check = SendEmail(msgSub, sMessage, "", replayMail, CustEmail, false, "");
            }
            catch
            {
                check = false;
            }
            return check;
        }
    }
}