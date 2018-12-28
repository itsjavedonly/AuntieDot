using AuntieDot.Attributes;
using AuntieDot.Engines;
using AuntieDot.Extensions;
using AuntieDot.Models;
using Microsoft.AspNet.Identity;
using ShopifySharp;
using ShopifySharp.Filters;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AuntieDot.Controllers
{
    public class DashboardController : Controller
    {
        
        // GET: Dashboard
        

        ApplicationDbContext db = new ApplicationDbContext();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        // TODO
        [RequireSubscription]
        public async Task<ActionResult> Index(int page = 1)
        {
            var pageSize = 50;
            //Get a reference and a query for the user's orders
            var ordersRef = db.GetUserOrdersReference(User.Identity.GetUserId());
            var query = ordersRef.Query();
            // TODO
            //Figure out how many orders the user has, and how many pages of 50 they'll create
            var totalOrders = await query.CountAsync();
            var totalPages = totalOrders % pageSize > 0 ?
            (totalOrders / pageSize) + 1 :
            totalOrders / pageSize;
            //Pass those values to the ViewBag
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalPages = totalPages;
            // TODO
            //Page must be at least 1
            page = page < 1 ? 1 : page;
            //Get the current page of orders
            var orders = await query
            .OrderByDescending(o => o.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            return View(orders);

        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Import()
        {
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            //Get a total count of open orders on the shop
            var service = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            var count = await service.CountAsync(new OrderFilter()
            {
                Status = "open"
            });
            // TODO
            //Import up to 250 orders at at ime
            int page = 0;
            var importedOrders = new List<ShopifySharp.Order>();
            while (page <= count / 250)
            {
                page += 1;
                //To reduce bandwidth/time, only return the fields that we'll be using.
                //These field names MUST match the Shopify API, NOT the ones in ShopifySharp.
                var filterOptions = new OrderFilter()
                {
                    Limit = 250,
                    Status = "open",
                    Fields = "id,created_at,name,line_items,customer",
                    Page = page
                };
                importedOrders.AddRange(await service.ListAsync(filterOptions));
            }
            // TODO
            //Get a reference to the user's orders.
            var orderRef = db.GetUserOrdersReference(User.Identity.GetUserId());
            //Get a list of all of the user's orders in this list that already exist in the database
            var ids = importedOrders.Select(i => i.Id.Value);
            var existing = await orderRef.Query()
            .Where(o => ids.Contains(o.ShopifyId))
            .Select(o => o.ShopifyId)
            .ToListAsync();
            //Use the list of existing order ids to determine which ones need to be added
            importedOrders = importedOrders
            .Where(s => existing.Contains(s.Id.Value) == false)
            .ToList();
            //Convert the final list of orders into a database order and save them
            foreach (var order in importedOrders)
            {
                orderRef.CurrentValue.Add(order.ToDatabaseOrder());
            }
            await db.SaveChangesAsync();
            return RedirectToAction("Index");

        }


        public async Task<ActionResult> Order(int id)
        {
            //Get a reference to the user's orders, then find the requested order's ShopifyId.
            var orderRef = db.GetUserOrdersReference(User.Identity.GetUserId());
            var shopifyId = await orderRef.Query().Where(o => o.Id == id).Select(o => o.ShopifyId).FirstAsync();
            //Get their cached shop data so we can use their access tokens without making another
            //query to the database.
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            //Pull in the full Shopify order, which includes the Shopify customer.
            var orderService = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            var fullOrder = await orderService.GetAsync(shopifyId);
            //Pass the database order id to the ViewBag
            ViewBag.OrderId = id;
            return View(fullOrder);
        }        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> SetStatus(int id, string status = "closed")
        {
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            var orderService = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            //Get a reference to the user's orders, then find the requested order's id.
            var orderRef = db.GetUserOrdersReference(User.Identity.GetUserId());
            var shopifyId = await orderRef.Query().Where(o => o.Id == id).Select(o => o.ShopifyId).FirstAsync();
            if (status.Equals("open", StringComparison.OrdinalIgnoreCase))
            {
                await orderService.OpenAsync(shopifyId);
            }
            else
            {
                await orderService.CloseAsync(shopifyId);
            }
            return RedirectToAction("Order", new { id = id });
        }        public async Task<ActionResult> Fulfill(int id, IEnumerable<long> items, string company, string number, string url)
        {
            // TODO
            var shop = CacheEngine.GetShopStatus(User.Identity.GetUserId(), HttpContext);
            var fulfillService = new FulfillmentService(shop.MyShopifyDomain, shop.ShopifyAccessToken
            );
            var orderService = new OrderService(shop.MyShopifyDomain, shop.ShopifyAccessToken);
            Fulfillment fulfillment;
            //Get a reference to the user's orders, then find the requested order's id.
            var orderRef = db.GetUserOrdersReference(User.Identity.GetUserId());
            var shopifyId = await orderRef.Query().Where(o => o.Id == id).Select(o => o.ShopifyId).FirstAsync();
            //Pull in the full Shopify order, which includes the Shopify customer.
            var fullOrder = await orderService.GetAsync(shopifyId);
            if (fullOrder.FulfillmentStatus == "success")
            {
                return RedirectToAction("Order", new { id = id });
            }
            //Get a list of all of the line items that are going to be fulfilled but aren't part of an
            //already pending fulfillment
            var toFulfill = fullOrder.LineItems
                .Where(li => items.Contains(li.Id.Value))
.Where(li => fullOrder.Fulfillments.Where(f => f.Status == "pending")
.Any(f => f.LineItems.Any(fLineItem => fLineItem.Id == li.Id)) == false);
            // TODO
            //Create a new fulfillment that will fulfill the remaining line items
            var trackingNumber = Guid.NewGuid().ToString();
            fulfillment = new Fulfillment()
            {
                TrackingCompany = company ?? "AuntieDot Shipping, LLC.",
                TrackingNumber = number ?? trackingNumber,
                TrackingUrl = url ?? ("https://shipping-company.com/track/" + trackingNumber),
                OrderId = fullOrder.Id.Value,
                LineItems = toFulfill
            };
            //Send the fulfillment to Shopify
            var notifyCustomer = true;
            fulfillment = await fulfillService.CreateAsync(shopifyId, fulfillment, notifyCustomer);
            if (fulfillment.Status != "pending" && fulfillment.Status != "success")
            {
                // Status = 'cancelled': The fulfillment was cancelled.
                // Status = 'error': There was an error with the fulfillment. (No details provided!)
                // Status = 'failure': The fulfillment request failed. (No details provided!)
                throw new Exception("Failed to fulfill line items.");
            }
            return RedirectToAction("Order", new { id = id });
        }


    }
     

}
