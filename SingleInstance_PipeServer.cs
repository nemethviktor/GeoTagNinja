using System;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using NLog;
using System.Security.AccessControl;
using System.Security.Principal;

namespace GeoTagNinja.Model
{

    /// <summary>
    /// Single threaded Named Pipe Server for GTN
    /// 
    /// Upon creation, a serving thread is started (serverThread) which provides
    /// (single threaded) one connection possbility at a time.
    /// 
    /// To stop serving, stopServing has to be called.
    /// </summary>
    internal class SingleInstance_PipeServer
    {

        internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The single threaded server thread
        /// </summary>
        internal Thread serverThread = null;

        /// <summary>
        /// Method that receives the message from the other end of the pipe
        /// </summary>
        internal Action<string> msgCallback = null;

        /// <summary>
        /// The cancelation token used to cancel the server.
        /// Initialized during server startup.
        /// </summary>
        internal CancellationTokenSource cTokenSource = null;


        /// <summary>
        /// The user name this GTN instance runs as
        /// </summary>
        internal string myUserName = "";

        /// <summary>
        /// Constructor for the SingleInstance_PipeServer, automatically
        /// starting the named pipe server.
        /// </summary>
        /// <param name="messageCallback">Set a method that receives the message
        /// from the other end of the pipe</param>
        public SingleInstance_PipeServer( Action<string> messageCallback) {
            msgCallback = messageCallback;
            myUserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            serverThread = new Thread(PipeServer_ServingThread);
            serverThread.Start();
        }


        /// <summary>
        /// Signal the server to stop serving. Asynchronously stops the server.
        /// </summary>
        public void stopServing()
        {
            Logger.Info(message: "Stopping pipe serving ...");
            if (cTokenSource != null)
            {
                cTokenSource.Cancel();
                cTokenSource.Dispose();
                cTokenSource = null;
            }
        }


        /// <summary>
        /// Single threaded server that continuously provides one
        /// connection possibility to the named pipe for clients.
        /// 
        /// Started by the constructor of this class.
        /// Use stopServing to stop the server.
        /// </summary>
        private void PipeServer_ServingThread()
        {
            Logger.Info(message: $"Starting pipe serving under user '{myUserName}' ...");

            // Create a cancelation token to abort the task
            cTokenSource = new CancellationTokenSource();

            // Security:
            PipeSecurity npSec = new PipeSecurity();
            // Deny all
            npSec.AddAccessRule(
                new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null),
                    PipeAccessRights.FullControl, AccessControlType.Deny));
            // Allow .... me :)
            npSec.AddAccessRule(
                new PipeAccessRule(myUserName, PipeAccessRights.FullControl,
                            System.Security.AccessControl.AccessControlType.Allow));

            while (true)
            {
                // Note: has to be set to async, as otherwise cancelation token
                // is ignored, cf. https://stackoverflow.com/questions/53695427/cancellationtoken-not-working-with-waitforconnectionasync
                NamedPipeServerStream npServer = new NamedPipeServerStream(
                    Program.singleInstance_PipeName, PipeDirection.In,
                    1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous, 0, 0, npSec);

                int threadID = Thread.CurrentThread.ManagedThreadId;
                Logger.Info(message: $"Server: started with Thread ID {threadID.ToString()}");

                // Async wait for connection that can be canceled
                IAsyncResult npConnectionResult = npServer.WaitForConnectionAsync(cTokenSource.Token);

                // Wait for either connection or cancelation
                int res = WaitHandle.WaitAny(new[] { npConnectionResult.AsyncWaitHandle });
                if (npServer.IsConnected)
                {
                    PipeServer_HandleConnection(npServer, threadID);
                    npServer.Close();
                }

                if ((cTokenSource == null) || (cTokenSource.IsCancellationRequested))
                {
                    Logger.Info(message: $"Server ({threadID.ToString()}): cancellation requested.");
                    return;
                }
            }
        }


        /// <summary>
        /// Method to handle the information transmission upon
        /// a successful connection through the named pipe.
        /// </summary>
        /// <param name="npServer">The NamedPipeServerStream connected with</param>
        /// <param name="threadID">The thread ID (for logging)</param>
        private void PipeServer_HandleConnection(NamedPipeServerStream npServer,
            int threadID)
        {
            try
            {
                StreamReader streamer = new StreamReader(npServer);
                // We only read one line...
                string inputLine = streamer.ReadLine();

                // Username only available after reading from pipe
                // But access is limited to current user only ...
                string sendingUser = npServer.GetImpersonationUserName();
                Logger.Info(message: $"Server ({threadID.ToString()}): connected to user {sendingUser}");

                msgCallback.Invoke($"Message from user '{sendingUser}': {inputLine}");
                Console.WriteLine(inputLine);
            }
            catch (IOException e)
            {
                // IOException raised if pipe is broken / disconnected...
                Logger.Info(message: $"Server ({threadID.ToString()}): Server session ended - " +
                    e.Message);
            }
        }

    }
}
