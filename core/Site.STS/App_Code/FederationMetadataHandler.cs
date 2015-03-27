//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Text;
using System.Web;

namespace Microsoft.EMIC.Cloud.STS
{
    public class FederationMetadataHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ClearHeaders(); 
            context.Response.Clear(); 

            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "text/xml";

            SampleVENUSSecurityTokenServiceConfiguration.Current.SerializeMetadata(context.Response.OutputStream);
        }      
        
        public bool IsReusable { get { return false; } }
    }
}