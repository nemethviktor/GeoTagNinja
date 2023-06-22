using NLog;
using System;
using System.Collections.Generic;

namespace GeoTagNinja.Model
{

    /// <summary>
    /// This class provides a subset of the functionality of Threading.Monitor, but
    /// extends the traceability who holds the lock on a resource by allowing to set
    /// an ID for the requester. It does not by default separate threads.
    /// </summary>
    internal static class GTN_Monitor
    {
        internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The list of held locks per object
        /// </summary>
        private static IDictionary<Object, string> lock_list = new Dictionary<Object, string>();

        /// <summary>
        /// Local lock object for acessing methods of this class / make them
        /// synchronized.
        /// 
        /// Cf. https://stackoverflow.com/questions/541194/c-sharp-version-of-javas-synchronized-keyword
        /// </summary>
        private readonly static object myLock = new object();

        /// <summary>
        /// Tries to acquire the lock on the given object for the given
        /// requester.
        /// 
        /// Access is thread safe.
        /// </summary>
        /// <param name="objectToLock">The object to request the lock for</param>
        /// <param name="requesterID">The ID of the requester</param>
        /// <param name="allowReentry">Whether success should be returned
        /// if the lock is already held by the same requester</param>
        /// <returns>Whether the lock was acquired</returns>
        public static bool TryEnter(Object objectToLock, string requesterID, bool allowReentry = false)
        {
            Logger.Debug("Lock object of type " + objectToLock.GetType().FullName +
                " by requester " + requesterID + " - wait for method sync");
            lock (myLock)
            {
                Logger.Debug("Lock object of type " + objectToLock.GetType().FullName +
                " by requester " + requesterID + " - start try");
                string currentHolder = null;
                if (!lock_list.TryGetValue(objectToLock, out currentHolder))
                    currentHolder = null;

                // Two cases in which it ends here
                if (currentHolder != null)
                {
                    Logger.Info("Lock object: current holder is " + currentHolder);
                    // Someone else is holding the lock
                    if (currentHolder != requesterID)
                        return false;
                    // Its the same requester but no reentry allowed
                    else if (!allowReentry)
                        return false;
                }

                Logger.Info("Lock object: success");
                // Success!!
                lock_list[objectToLock] = requesterID;
                return true;
            }
        }


        /// <summary>
        /// Releases the lock on the given object.
        /// 
        /// Access is thread safe.
        /// </summary>
        /// <param name="objectToUnlock">The object to release the lock on</param>
        public static void Exit(Object objectToUnlock, string requesterID)
        {
            Logger.Debug("Unlock object of type " + objectToUnlock.GetType().FullName +
                " by requester " + requesterID + " - wait for method sync");
            lock (myLock)
            {
                Logger.Debug("Unlock object of type " + objectToUnlock.GetType().FullName +
                    " by requester " + requesterID + " - start try");

                // Check if the requester is the right one...
                string currentHolder = null;
                if (!lock_list.TryGetValue(objectToUnlock, out currentHolder))
                    currentHolder = null;

                if (!string.IsNullOrEmpty(currentHolder))
                {
                    if (currentHolder != requesterID)
                        throw new ArgumentException("Trying to unlock object of type " +
                            objectToUnlock.GetType().FullName + " by requester '" + requesterID + "', but the " +
                            "lock is held by '" + currentHolder + "'");
                }

                lock_list.Remove(objectToUnlock);
                Logger.Info("Try to lock: current holder is " + currentHolder);
            }
        }
    

        /// <summary>
        /// For the given object, return the owner that is locking it.
        /// 
        /// Access is thread safe.
        /// </summary>
        /// <param name="lockedObject">The object to return the lock owner for</param>
        /// <returns>The owner or null of no owner / no lock</returns>
        public static string GetCurrentOwner(Object lockedObject)
        {
            lock (myLock)
            {
                string currentOwner = null;
                if (!lock_list.TryGetValue(lockedObject, out currentOwner))
                    return null;
                return currentOwner;
            }
        }
    }
}
