using AuntieDot.Engines;
using AuntieDot.Extensions;
using AuntieDot.Models;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AuntieDot.Controllers
{
    public class WebHooksController : Controller
    {
        public async Task<string> AppUninstalled(string userId)
        {
            var isValidRequest = await AuthorizationService.IsAuthenticWebhook(
            Request.Headers.ToKvps(),
            Request.InputStream,
            ApplicationEngine.ShopifySecretKey);
            if (!isValidRequest)
            {
                throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
            }

            //Pull in the user
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByIdAsync(userId);
            //Delete their subscription charge and Shopify details
            user.ShopifyChargeId = null;
            user.ShopifyAccessToken = null;
            user.MyShopifyDomain = null;
            //Save changes
            var update = await usermanager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                // TODO: Log or handle exception in whatever way you see fit.
                string message = "Couldn't delete a user's Shopify details. Reason: " +
                string.Join(", ", update.Errors);
                throw new Exception(message);
            }
            CacheEngine.ResetShopStatus(user.Id, HttpContext);
            return "Successfully handled AppUninstalled webhook.";
        }

        [HttpPost]
        public async Task<string> OrderCreated(string userId)
        {
            // TODO
            var isValidRequest = await AuthorizationService.IsAuthenticWebhook(
Request.Headers.ToKvps(),
Request.InputStream,
ApplicationEngine.ShopifySecretKey);
            if (!isValidRequest)
            {
                throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
            }
            //ShopifySharp has just read the input stream. We must always reset the inputstream
            //before reading it again.
            Request.InputStream.Position = 0;
            //Do not dispose the StreamReader or input stream. The controller will do that itself.
            string bodyText = await new StreamReader(Request.InputStream).ReadToEndAsync();
            //Parse the Order from the body text
            var shopifyOrder = JsonConvert.DeserializeObject<ShopifySharp.Order>(bodyText);
            //Convert the order to one that can be stored in the database
            var order = shopifyOrder.ToDatabaseOrder();//MJ-1 Pg:90
            //Get a reference to the user's .Orders collection and save this one to the database
            using (var db = new ApplicationDbContext())
            {
                var ordersRef = db.GetUserOrdersReference(userId);
                ordersRef.CurrentValue.Add(order);
                await db.SaveChangesAsync();
            }
            return "Successfully handled OrderCreated webhook.";
        }
        [HttpPost]
        public async Task<string> OrderUpdated(string userId)
        {
            var isValidRequest = await AuthorizationService.IsAuthenticWebhook(
Request.Headers.ToKvps(),
Request.InputStream,
ApplicationEngine.ShopifySecretKey);
            if (!isValidRequest)
            {
                throw new UnauthorizedAccessException("This request is not an authentic webhook request.");
            }
            //ShopifySharp has just read the input stream. We must always reset the inputstream
            //before reading it again.
            Request.InputStream.Position = 0;
            //Do not dispose the StreamReader or input stream. The controller will do that itself.
            string bodyText = await new StreamReader(Request.InputStream).ReadToEndAsync();
            //Parse the Order from the body text
            var shopifyOrder = JsonConvert.DeserializeObject<ShopifySharp.Order>(bodyText);
            using (var db = new ApplicationDbContext())
            {
                Order objOrder = new Order();//MJ
                var ordersRef = db.GetUserOrdersReference(userId);
                var order = await ordersRef.Query().FirstOrDefaultAsync(o => o.ShopifyId == objOrder.Id.Value);
                //Shopify often sends this webhook before the OrderCreated webhook.
                //Check that the order actually exists in the database before trying to update it.
                if (order == null)
                {
                    return "Order does not exist in database.";
                }
                //Transfer the updated order's properties to the database order
                var updatedOrder = shopifyOrder.ToDatabaseOrder();
                order.CustomerName = updatedOrder.CustomerName;
                order.DateCreated = updatedOrder.DateCreated;
                order.DisplayId = updatedOrder.DisplayId;
                order.IsOpen = updatedOrder.IsOpen;
                order.LineItemSummary = updatedOrder.LineItemSummary;
                await db.SaveChangesAsync();


            }
            return "Successfully handled OrderUpdated webhook";
        }
        protected override void OnException(ExceptionContext filterContext)
        {
            Exception ex = filterContext.Exception;
            // TODO: Log or report your exception.
            //Let the base controller finish this execution
            base.OnException(filterContext);
        }
    }
}
