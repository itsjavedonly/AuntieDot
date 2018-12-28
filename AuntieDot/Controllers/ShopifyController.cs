using AuntieDot.Engines;
using AuntieDot.Extensions;
using AuntieDot.Models;
using Microsoft.AspNet.Identity.Owin;
using ShopifySharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AuntieDot.Controllers
{
    public class ShopifyController : Controller
    {
        public async Task<ActionResult> Handshake(string shop)
        {
            // TODO
            //Store the shop URL in a cookie.
            Response.SetCookie(new HttpCookie(".App.Handshake.ShopUrl", shop)
            {
                Expires = DateTime.Now.AddDays(30)
            });
            //Open a connection to our database
            using (var db = new ApplicationDbContext())
            {
                //Check if any user in the database has already connected this shop.
                if (await db.Users.AnyAsync(user => user.MyShopifyDomain == shop))
                {
                    //This shop already exists, and the user is trying to log in.
                    //Redirect them to the dashboard.
                    return RedirectToAction("Index", "Dashboard");
                }
                //This shop does not exist, and the user is trying to install the app.
                //Redirect them to registration.
                return RedirectToAction("Index", "Register");
            }

        }
        [Authorize]
        public async Task<ActionResult> AuthResult(string shop, string code)
        {
            // TODO
            string apiKey = ApplicationEngine.ShopifyApiKey;
            string secretKey = ApplicationEngine.ShopifySecretKey;
            //Validate the signature of the request to ensure that it's valid
            if (!AuthorizationService.IsAuthenticRequest(Request.QueryString.ToKvps(), secretKey))
            {
                //The request is invalid and should not be processed.
                throw new Exception("Request is not authentic.");
            }
            //The request is valid. Exchange the temporary code for a permanent access token
            string accessToken;
            try
            {
                accessToken = await AuthorizationService.Authorize(code, shop, apiKey, secretKey);
            }
            catch (ShopifyException e)
            {
                // Failed to authorize app installation.
                // TODO: Log or handle exception in whatever way you see fit.
                throw e;
            }
            //Get the user
            var usermanager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            user.ShopifyAccessToken = accessToken;
            user.MyShopifyDomain = shop;
            // TODO: Create the AppUninstalled webhook in Chapter 6.
            var service = new WebhookService(user.MyShopifyDomain, user.ShopifyAccessToken);
            var hook = new Webhook()
            {
                Address = "https://ce80ae67.ngrok.io/webhooks/AppUninstalled?userId=" + user.Id,
                Topic = "app/uninstalled",
            };
            try
            {
                hook = await service.CreateAsync(hook);
            }
            catch (ShopifyException e) when (e.Message.ToLower().Contains("for this topic has already been taken"))
            {
                //Ignore error, webhook has already been created and is still valid.
            }
            catch (ShopifyException e)
            {
                // TODO: Log or handle exception in whatever way you see fit.
                throw e;
            }
            //Delete the shop's status from cache to force a refresh.
          //  CacheEngine.ResetShopStatus(user.Id, HttpContext);
           // return RedirectToAction("Charge", "Register");


            var update = await usermanager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                // TODO: Log or handle exception in whatever way you see fit.
                string message = "Couldn't save a user's access token and shop domain. Reason: " +
                string.Join(", ", update.Errors);
                throw new Exception(message);
            }
            //Delete the shop's status from cache to force a refresh.
            CacheEngine.ResetShopStatus(user.Id, HttpContext);
            return RedirectToAction("Charge", "Register");

        }
        [Authorize]
        public async Task<ActionResult> ChargeResult(string shop, long charge_id)
        {
            // TODO
            //Get the user
            var owinContext = HttpContext.GetOwinContext();
            var usermanager = owinContext.GetUserManager<ApplicationUserManager>();
            var user = await usermanager.FindByNameAsync(User.Identity.Name);
            //Create the billing service, which will be used to pull in the charge id
            var service = new RecurringChargeService(user.MyShopifyDomain, user.ShopifyAccessToken);
            RecurringCharge charge;
            //Try to get the charge. If a "404 Not Found" exception is thrown, the charge has been de
            //leted.
try
            {
                charge = await service.GetAsync(charge_id);
            }
            catch (ShopifyException e)
            when ((int)e.HttpStatusCode == 404 /* Not found */)
            {
                //The charge has been deleted. Redirect the user to accept a new charge.

                return RedirectToAction("Charge", "Register");
            }
            //Activate the charge
            await service.ActivateAsync(charge_id);
            //Get the charge again to refresh its BillingOn date.
            charge = await service.GetAsync(charge_id);
            //Save the charge to the user model
            user.ShopifyChargeId = charge_id;
            user.BillingOn = charge.BillingOn;
            var update = await usermanager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                // TODO: Log or handle exception in whatever way you see fit.
                string message = "Couldn't save a user's activated charge id. Reason: " +
                string.Join(", ", update.Errors);
                throw new Exception(message);
            }

            //Create an OrderCreated and OrderUpdated webhook
            var hookService = new WebhookService(user.MyShopifyDomain, user.ShopifyAccessToken);
            var topics = new string[]
            {
"orders/create",
"orders/updated"
            };
            foreach (var topic in topics)
            {
                var hook = new Webhook()
                {
                    Address = $"https://my-app.com/webhooks/{topic}?userId={user.Id}",
                    Topic = topic
                };
                try
                {
                    await hookService.CreateAsync(hook);
                }
                catch (ShopifyException e) when (e.Message.ToLower().Contains("for this topic has already been taken"))
                {
                    //Ignore error, webhook has already been created and is still valid.
                }
                catch (ShopifyException e)
                {
                    // TODO: Log or handle exception in whatever way you see fit.
                    throw e;
                }
            }


            //Delete the shop's status from cache to force a refresh.
            CacheEngine.ResetShopStatus(user.Id, HttpContext);
            //User's subscription charge has been activated and they can now use the app.
            return RedirectToAction("Index", "Dashboard");
        }

        }
}