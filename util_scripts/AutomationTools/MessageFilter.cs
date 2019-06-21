using System;
using System.Runtime.InteropServices;


namespace BeckhoffBuilder
{
    /// <summary>
    /// Message Handler implementiation to ensure a retry on the E_REJECTED_XXX errors caused when a COM object is busy. (See Msdn documentation for Visual Studio)
    /// </summary>
    public class MessageFilter : IOleMessageFilter
    {
        /// <summary>
        /// Start the filter
        /// </summary>
        /// <returns>The error code resulting from registering. 0 if successful.</returns>
        public static int Register()
        {
            IOleMessageFilter newFilter = new MessageFilter();
            IOleMessageFilter oldFilter = null;
            return CoRegisterMessageFilter(newFilter, out oldFilter);
        }

        /// <summary>
        /// Done with the filter, close it.
        /// </summary>
        public static void Revoke()
        {
            IOleMessageFilter oldFilter = null;
            int test = CoRegisterMessageFilter(null, out oldFilter);
        }

        // IOleMessageFilter functions.

        /// <summary>
        /// Handles the in coming thread requests.
        /// </summary>
        /// <param name="dwCallType">Type of the dw call.</param>
        /// <param name="hTaskCaller">The h task caller.</param>
        /// <param name="dwTickCount">The dw tick count.</param>
        /// <param name="lpInterfaceInfo">The lp interface info.</param>
        /// <returns></returns>
        int IOleMessageFilter.HandleInComingCall(int dwCallType,
          System.IntPtr hTaskCaller, int dwTickCount, System.IntPtr
          lpInterfaceInfo)
        {
            return 0;
        }

        /// <summary>
        /// Retries the rejected call.
        /// </summary>
        /// <param name="hTaskCallee">The h task callee.</param>
        /// <param name="dwTickCount">The dw tick count.</param>
        /// <param name="dwRejectType">Type of the dw reject.</param>
        /// <returns></returns>
        int IOleMessageFilter.RetryRejectedCall(System.IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == 2) // flag = SERVERCALL_RETRYLATER.
            {
                // The number of milliseconds until retry.
                return 99;
            }
            // Too busy; cancel call.
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hTaskCallee">The h task callee.</param>
        /// <param name="dwTickCount">The dw tick count.</param>
        /// <param name="dwPendingType">Type of the dw pending.</param>
        /// <returns></returns>
        int IOleMessageFilter.MessagePending(System.IntPtr hTaskCallee,int dwTickCount, int dwPendingType)
        {
            //Return the flag PENDINGMSG_WAITDEFPROCESS.
            return 2;
        }

        // Implement the IOleMessageFilter interface.
        [DllImport("Ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);
    }

    /// <summary>
    /// Definition of the IOleMessageFilter interface
    /// </summary>
    [ComImport(), Guid("00000016-0000-0000-C000-000000000046"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    interface IOleMessageFilter
    {
        /// <summary>
        /// Handles the in coming call.
        /// </summary>
        /// <param name="dwCallType">Type of the dw call.</param>
        /// <param name="hTaskCaller">The h task caller.</param>
        /// <param name="dwTickCount">The dw tick count.</param>
        /// <param name="lpInterfaceInfo">The lp interface info.</param>
        /// <returns></returns>
        [PreserveSig]
        int HandleInComingCall(
            int dwCallType,
            IntPtr hTaskCaller,
            int dwTickCount,
            IntPtr lpInterfaceInfo);

        /// <summary>
        /// Retries the rejected call.
        /// </summary>
        /// <param name="hTaskCallee">The h task callee.</param>
        /// <param name="dwTickCount">The dw tick count.</param>
        /// <param name="dwRejectType">Type of the dw reject.</param>
        /// <returns></returns>
        [PreserveSig]
        int RetryRejectedCall(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwRejectType);

        /// <summary>
        /// Messages the pending.
        /// </summary>
        /// <param name="hTaskCallee">The h task callee.</param>
        /// <param name="dwTickCount">The dw tick count.</param>
        /// <param name="dwPendingType">Type of the dw pending.</param>
        /// <returns></returns>
        [PreserveSig]
        int MessagePending(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwPendingType);
    }
}
