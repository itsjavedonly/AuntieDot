using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace AuntieDot.Engines
{
    public static class ApplicationEngine
    {
        public static string ShopifySecretKey { get; } =
        ConfigurationManager.AppSettings.Get("Shopify_Secret_Key");
        public static string ShopifyApiKey { get; } =
        ConfigurationManager.AppSettings.Get("Shopify_API_Key");

    }
}