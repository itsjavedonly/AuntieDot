using AuntieDot.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;

namespace AuntieDot.Extensions
{
    public static class ApplicationDbContextExtension
    {
        /// <summary>
        /// Gets a reference to the user's <see cref="ApplicationUser.Orders" /> collection in the database,
        /// without querying the entire collection from the database. Note: you can add new orders to this        /// /// collection with `ref.CurrentValue.Add()`; however, the current value WILL ALWAYSBE EMPTY.Use
/// `ref.Query()` to query and filter collection values from the database.
/// </summary>
public static DbCollectionEntry<ApplicationUser, DbOrder>
GetUserOrdersReference(this ApplicationDbContext db, string userId)
        {
            var userRef = db.Users.Attach(new ApplicationUser() { Id = userId });
            var ordersRef = db.Entry(userRef).Collection(u => u.Orders);
            return ordersRef;
        }
    }
}