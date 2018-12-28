using AuntieDot.Engines;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AuntieDot.Attributes
{
    public class RequireSubscription : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            // base.OnAuthorization(filterContext);
            //Let the attribute perform its default authorization, ensuring the user is logged in.
            base.OnAuthorization(filterContext);
            //If default authorization failed, filterContext.Result will be set.
            //Ensure it's null before continuing.
            if (filterContext.Result == null)
            {
                var context = filterContext.HttpContext;
                var userId = context.User.Identity.GetUserId();

                //Get the shop's status from the CacheEngine.
                var status = CacheEngine.GetShopStatus(userId, context);

              //  var status = ...
if (status.BillingIsConnected && status.ShopIsConnected)
                {
                    //Assume subscription is valid.
                }
                else if (status.BillingIsConnected == false)
                {
                    //User has connected their Shopify shop, but they haven't accepted a subscription charge.
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary()
{
{ "controller", "Register" },
{ "action", "Charge" }
});
                }
                else
                {
                    //User has created an account, but they haven't connected their Shopify shop.
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary()
{
{ "controller", "Register" },
{ "action", "Connect" }
});
                }

            }
        }
    }
}