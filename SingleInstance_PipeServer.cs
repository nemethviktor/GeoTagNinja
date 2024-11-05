using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using NLog;

namespace GeoTagNinja;

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
    internal static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     The single threaded server thread
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
        myUserName = WindowsIdentity.GetCurrent().Name;

        serverThread = new Thread(start: PipeServer_ServingThread);
        serverThread.Start();
    }


    /// <summary>
    /// Signal the server to stop serving. Asynchronously stops the server.
    /// </summary>
    public void stopServing()
    {
        Log.Info(message: "Stopping pipe serving ...");
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
        Log.Info(message: $"Starting pipe serving under user '{myUserName}' ...");

        // Create a cancelation token to abort the task
        cTokenSource = new CancellationTokenSource();

        // Security:
        PipeSecurity npSec = new PipeSecurity();
        // Deny all
        npSec.AddAccessRule(
            rule: new PipeAccessRule(
                identity: new SecurityIdentifier(sidType: WellKnownSidType.NetworkSid,
                                                 domainSid: null),
                rights: PipeAccessRights.FullControl, type: AccessControlType.Deny));
        // Allow .... me :)
        npSec.AddAccessRule(
            rule: new PipeAccessRule(identity: myUserName,
                                     rights: PipeAccessRights.FullControl,
                                     type: AccessControlType.Allow));

        while (true)
        {
            // Note: has to be set to async, as otherwise cancelation token
            // is ignored, cf. https://stackoverflow.com/questions/53695427/cancellationtoken-not-working-with-waitforconnectionasync
            NamedPipeServerStream npServer = new NamedPipeServerStream(
                pipeName: Program.singleInstance_PipeName, direction: PipeDirection.In,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous, inBufferSize: 0, outBufferSize: 0,
                pipeSecurity: npSec);

            int threadID = Thread.CurrentThread.ManagedThreadId;
            Log.Info(message: $"Server: started with Thread ID {threadID.ToString()}");

            // Async wait for connection that can be canceled
            IAsyncResult npConnectionResult =
                npServer.WaitForConnectionAsync(cancellationToken: cTokenSource.Token);

            // Wait for either connection or cancelation
            int res =
                WaitHandle.WaitAny(waitHandles: new[]
                                       { npConnectionResult.AsyncWaitHandle });
            if (npServer.IsConnected)
            {
                PipeServer_HandleConnection(npServer: npServer, threadID: threadID);
                npServer.Close();
            }

            if ((cTokenSource == null) || (cTokenSource.IsCancellationRequested))
            {
                Log.Info(message: $"Server ({threadID.ToString()}): cancellation requested.");
                return;
            }
        }
    }

    /*
     // this is for future reference because i don't want to go searching for it again
     // in .NET 6+ the below will work.

     private void PipeServer_ServingThread()
       {
           Log.Info(message: $"Starting pipe serving under user '{myUserName}' ...");

           // Create a cancelation token to abort the task
           cTokenSource = new CancellationTokenSource();

           // Security:
           PipeSecurity npSec = new PipeSecurity();
           // Deny all
           npSec.AddAccessRule(
               rule: new PipeAccessRule(
                   identity: new SecurityIdentifier(sidType: WellKnownSidType.NetworkSid,
                                                    domainSid: null),
                   rights: PipeAccessRights.FullControl, type: AccessControlType.Deny));
           // Allow .... me :)
           npSec.AddAccessRule(
               rule: new PipeAccessRule(identity: myUserName,
                                        rights: PipeAccessRights.FullControl,
                                        type: AccessControlType.Allow));

           while (true)
           {
               // Note: has to be set to async, as otherwise cancelation token
               // is ignored, cf. https://stackoverflow.com/questions/53695427/cancellationtoken-not-working-with-waitforconnectionasync

               // also re: update to .NET --> https://stackoverflow.com/questions/59969943/how-to-set-pipesecurity-of-namedpipeserverstream-in-net-core
               NamedPipeServerStream npServer = NamedPipeServerStreamAcl.Create(
                   pipeName: Program.singleInstance_PipeName,
                   direction: PipeDirection.In,
                   maxNumberOfServerInstances: 1,
                   transmissionMode: PipeTransmissionMode.Byte,
                   options: PipeOptions.Asynchronous,
                   inBufferSize: 0,
                   outBufferSize: 0,
                   pipeSecurity: npSec
               );

               int threadID = Thread.CurrentThread.ManagedThreadId;
               Log.Info(message: $"Server: started with Thread ID {threadID.ToString()}");

               // Async wait for connection that can be canceled
               IAsyncResult npConnectionResult =
                   npServer.WaitForConnectionAsync(cancellationToken: cTokenSource.Token);

               // Wait for either connection or cancelation
               int res =
                   WaitHandle.WaitAny(waitHandles: new[]
                                          { npConnectionResult.AsyncWaitHandle });
               if (npServer.IsConnected)
               {
                   PipeServer_HandleConnection(npServer: npServer, threadID: threadID);
                   npServer.Close();
               }

               if ((cTokenSource == null) || (cTokenSource.IsCancellationRequested))
               {
                   Log.Info(message: $"Server ({threadID.ToString()}): cancellation requested.");
                   return;
               }
           }
       }

     */

    /// <summary>
    ///     Method to handle the information transmission upon
    ///     a successful connection through the named pipe.
    /// </summary>
    /// <param name="npServer">The NamedPipeServerStream connected with</param>
    /// <param name="threadID">The thread ID (for logging)</param>
    private void PipeServer_HandleConnection(NamedPipeServerStream npServer,
                                             int threadID)
    {
        try
        {
            StreamReader streamer = new StreamReader(stream: npServer);
            // We only read one line...
            string inputLine = streamer.ReadLine();

            // Username only available after reading from pipe
            // But access is limited to current user only ...
            string sendingUser = npServer.GetImpersonationUserName();
            Log.Info(message: $"Server ({threadID.ToString()}): connected to user {sendingUser}");

            msgCallback.Invoke(obj: $"Message from user '{sendingUser}': {inputLine}");
            Console.WriteLine(value: inputLine);
        }
        catch (IOException e)
        {
            // IOException raised if pipe is broken / disconnected...
            Log.Info(message: $"Server ({threadID.ToString()}): Server session ended - " +
                              e.Message);
        }
    }

}