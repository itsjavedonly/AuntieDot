using AuntieDot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AuntieDot.Extensions
{
    public static class OrderExtensions
    {
        public static DbOrder ToDatabaseOrder(this ShopifySharp.Order shopifyOrder)
        {
            // TODO
            //Create a summary of the order's line items
            var firstItem = shopifyOrder.LineItems.FirstOrDefault();
            string summary = shopifyOrder.LineItems.Count() > 1 ?
            firstItem?.Title + $" and {shopifyOrder.LineItems.Count() - 1} other items." :
            firstItem?.Title;
            //Build an order that we can store in our database.
            var order = new DbOrder()
            {
                ShopifyId = shopifyOrder.Id.Value,
                DisplayId = shopifyOrder.Name,
                LineItemSummary = summary,
                CustomerName = shopifyOrder.Customer.FirstName + " " + shopifyOrder.Customer.LastName
            ,
                DateCreated = shopifyOrder.CreatedAt.HasValue ? shopifyOrder.CreatedAt.Value : DateTimeOffset.Now,

                IsOpen = shopifyOrder.ClosedAt == null
            };
            return order;
        }
    }
}