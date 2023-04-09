using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Platini.Models
{
    [Serializable()]
    public class AccessRequest
    {
        #region Fields
        private string m_AccessLicenseNumber;
        private string m_UserId;
        private string m_Password;
        #endregion

        #region Constructor
        public AccessRequest()
        {

        }

        public AccessRequest(string accessLicenseNumber, string userId, string password)
        {
            m_AccessLicenseNumber = accessLicenseNumber;
            m_UserId = userId;
            m_Password = password;
        }
        #endregion

        #region Properties
        public string Password
        {
            get { return m_Password; }
            set { m_Password = value; }
        }

        public string UserId
        {
            get { return m_UserId; }
            set { m_UserId = value; }
        }

        public string AccessLicenseNumber
        {
            get { return m_AccessLicenseNumber; }
            set { m_AccessLicenseNumber = value; }
        }
        #endregion

    }

    public class UPSAddress
    {
        private string m_City;
        private string m_StateProvinceCode;
        private string m_PostalCode;

        public string PostalCode
        {
            get { return m_PostalCode; }
            set { m_PostalCode = value; }
        }

        public string StateProvinceCode
        {
            get { return m_StateProvinceCode; }
            set { m_StateProvinceCode = value; }
        }


        public string City
        {
            get { return m_City; }
            set { m_City = value; }
        }



    }
    public class UpsRequest
    {
        public string AddressValidateRequest(AccessRequest accRequest, UPSAddress addr, string AddressUrl)
        {
            string result = "";
            string requestString = "";

            requestString += SerializeObj(accRequest).InnerXml;

            // TODO: Use serailize object instead of fix xml code.
            requestString += string.Format(@"<?xml version='1.0'?>
                                    <AddressValidationRequest xml:lang='en-US'>
                                       <Request>
                                          <TransactionReference>
                                             <CustomerContext>Customer Data</CustomerContext>
                                             <XpciVersion>1.0001</XpciVersion>
                                          </TransactionReference>
                                          <RequestAction>AV</RequestAction>
                                       </Request>
                                       <Address>
                                          <City>{0}</City>
                                          <StateProvinceCode>{1}</StateProvinceCode>
                                          <PostalCode>{2}</PostalCode>
                                       </Address>
                                    </AddressValidationRequest>", addr.City, addr.StateProvinceCode, addr.PostalCode);

            // Return data in xml format

            result = UPSRequest(AddressUrl, requestString);

            // TODO: Serialize return string to object

            return result;

        }

        private string UPSRequest(string url, string requestText)
        {
            string result = "";

            ASCIIEncoding encodedData = new ASCIIEncoding();
            byte[] byteArray = encodedData.GetBytes(requestText);

            // open up da site
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.Method = "POST";
            wr.KeepAlive = false;
            wr.UserAgent = "Benz";
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.ContentLength = byteArray.Length;
            try
            {
                // send xml data
                Stream SendStream = wr.GetRequestStream();
                SendStream.Write(byteArray, 0, byteArray.Length);
                SendStream.Close();

                // get da response
                HttpWebResponse WebResp = (HttpWebResponse)wr.GetResponse();
                using (StreamReader sr = new StreamReader(WebResp.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                    sr.Close();
                }


                WebResp.Close();
            }
            catch (Exception ex)
            {
                // Unhandle exception occure
                result = ex.Message;
            }

            return result;
        }

        // Serialize Object to XML
        private System.Xml.XmlDocument SerializeObj(Object obj)
        {
            System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();

            try
            {
                using (System.IO.MemoryStream myStream = new System.IO.MemoryStream())
                {
                    System.Xml.Serialization.XmlSerializer xmlSer = new System.Xml.Serialization.XmlSerializer(obj.GetType());
                    xmlSer.Serialize(myStream, obj);
                    myStream.Position = 0;
                    xmlDoc.Load(myStream);
                }
            }
            catch (Exception ex)
            {
            }
            return xmlDoc;
        }
    }
}