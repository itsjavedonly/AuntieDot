using AuntieDot.Models;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace AuntieDot.Engines
{
    public class CacheEngine
    {
        /// <summary>
        /// Creates a key to be used by other methods in the CacheEngine. Keys are case-sensi
        /// tive.
        /// Use this method to ensure that the cache key is always the same for each user.
        /// </summary>
        public static string CreateCacheKey(string userId)
        {
            return userId.ToLower() + "-ShopIsConnected";
        }


        /// <summary>
        /// Sets the cache value for the key.
        /// </summary>
        public static void SetCacheValue(string cacheKey, HttpContextBase context, object value)
        {
            var expiration = DateTime.Now.AddHours(1);
            context.Cache.Insert(cacheKey, value, null, expiration, Cache.NoSlidingExpiration);
        }
        /// <summary>
        /// Deletes a shop's status from cache.
        /// </summary>
        public static void ResetShopStatus(string userId, HttpContextBase context)
        {
            string key = CreateCacheKey(userId);
            context.Cache.Remove(key);
        }

        public static CachedShopStatus GetShopStatus(string userId, HttpContextBase context)
        {
            var cacheKey = CreateCacheKey(userId);
            var status = context.Cache.Get(cacheKey) as CachedShopStatus;
            if (status != null)
            {
                return status;
            }
            //Shop status is not in the current cache. Grab the user to check values against database.
            var db = context.GetOwinContext().Get<ApplicationDbContext>();
            var user = db.Users.Find(userId);
            status = new CachedShopStatus()
            {
                Username = user.UserName,
                MyShopifyDomain = user.MyShopifyDomain,
                ShopifyAccessToken = user.ShopifyAccessToken,
                ShopifyChargeId = user.ShopifyChargeId
            };
            //Store the result in cache to prevent querying the database next time.
            SetCacheValue(cacheKey, context, status);
            return status;
        }
    }
}