using Microsoft.Identity.Client;

using System.Threading;
using System.Web;

namespace Rwp
{
    // largely drawn from https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/token-cache-serialization
    public class MSALSessionCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string UserId = string.Empty;
        string CacheId = string.Empty;
        HttpContextBase httpContext = null;

        TokenCache cache = new TokenCache();

        public MSALSessionCache(string userId, HttpContextBase httpcontext)
        {
            // not object, we want the SUB
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;
            Load();
        }

        public TokenCache GetMsalCacheInstance()
        {
            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            Load();
            return cache;
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session[CacheId + "_state"] = state;
            SessionLock.ExitWriteLock();
        }
        public string ReadUserStateValue()
        {
            string state = string.Empty;
            SessionLock.EnterReadLock();
            state = (string)httpContext.Session[CacheId + "_state"];
            SessionLock.ExitReadLock();
            return state;
        }

        // because we store the cache in SessionState, occasionally we will have a cookie
        // for a user but we won't have a token cache (happens most often when a user logs
        // in, but the session resets (e.g. the application is deployed again); this means
        // we won't have anything cached.

        // we need to provide a way for the page to know that there is no cache, and to 
        // treat that like a logged-out state. the user will login again, which will
        // populate the cache
        public static bool CacheExists(string userId, HttpContextBase httpContext)
        {
            string cacheId = userId + "_TokenCache";
            bool exists = false;

            SessionLock.EnterReadLock();
            if (httpContext.Session[cacheId] != null)
                exists = true;

            SessionLock.ExitReadLock();
            return exists;
        }

        public void Load()
        {
            SessionLock.EnterReadLock();
            cache.Deserialize((byte[])httpContext.Session[CacheId]);
            SessionLock.ExitReadLock();
        }

        public void Persist()
        {
            SessionLock.EnterWriteLock();

            // Optimistically set HasStateChanged to false. We need to do it early to avoid losing changes made by a concurrent thread.
            cache.HasStateChanged = false;

            // Reflect changes in the persistent store
            httpContext.Session[CacheId] = cache.Serialize();
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after MSAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                Persist();
            }
        }
    }
}