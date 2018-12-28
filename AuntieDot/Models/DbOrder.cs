using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AuntieDot.Models
{
    public class DbOrder
    {
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// The full Shopify order's API id.
        /// </summary>
        public long ShopifyId { get; set; }
        /// <summary>
        /// The full Shopify order's display id, e.g. #1001. This is
        /// the id that merchants see in their dashboards.
        /// </summary>
        public string DisplayId { get; set; }
        /// <summary>
        /// A string that summarizes the line items in the Shopify order.
        /// </summary>
        public string LineItemSummary { get; set; }
        /// <summary>
        /// The name of the customer that placed the order.
        /// </summary>
        public string CustomerName { get; set; }
        /// <summary>
        /// The date the order was created. Can be used to sort orders on
        /// the dashboard. DateTimeOffset is used to preserve timezone information.        /// /// </summary>
        public DateTimeOffset DateCreated { get; set; }
        /// <summary>
        /// Tracks whether the order is currently open or closed (archived).
        /// </summary>
        public bool IsOpen { get; set; }
    }
}