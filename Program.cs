using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using CommandLine;

namespace GeoTagNinja;

internal static class Program
{
    private const string PipeErrorAbandonedMutexException = "AbandonedMutexException";
    private const string PipeErrorConnectionTimeout = "Connection timed out: ";
    private const string PipeErrorIoError = "IO Error: ";
    private const string PipeErrorAccessDenied = "Access Denied: ";
    private const string PipeErrorCouldNotConnect = "Could not connect to other GTN instance";
    private const string PipeErrorUnexpectedError = "Unexpected Error: ";
    public static string CollectionFileLocation;
    public static bool CollectionModeEnabled;

    public const string SingleInstancePipeName = "GeoTagNinjaSingleInstance";

    /// <summary>
    ///     If true, this is the "first" instance of the program
    /// </summary>
    public static bool SingleInstanceHighlander;

    /// <summary>
    ///     A Mutex that is held by the first instance of the application
    /// </summary>
    private static Mutex _singleInstanceMutex = new(initiallyOwned: false, name: "GeoTagNinja.SingleInstance");

    /// <summary>
    ///     The main entry point for the application.
    ///     Usage: either leave the args blank or do: geotagninja.exe --collection="c:\temp\collection.txt" or some such.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(defaultValue: false);

        Parser.Default.ParseArguments<Options>(args: args)
            .WithParsed(action: o =>
            {
                if (File.Exists(path: o.Collection))
                {
                    CollectionFileLocation = o.Collection;
                    CollectionModeEnabled = true;
                }
            });

        // Brief wait in case current instance is exiting
        try
        {
            SingleInstanceHighlander = _singleInstanceMutex.WaitOne(millisecondsTimeout: 1000);
        }
        catch (AbandonedMutexException)
        {
            // Other application did not release Mutex properly
            MessageBox.Show(text: PipeErrorAbandonedMutexException);
        }

        if (!SingleInstanceHighlander)
        {
            PassInfoToRunningInstance();
        }
        else
        {
            Application.Run(mainForm: new FrmMainApp());
            _singleInstanceMutex.ReleaseMutex();
        }
    }


    /// <summary>
    ///     Passes information to the already running first instance of
    ///     GTN using a named pipe.
    /// </summary>
    private static void PassInfoToRunningInstance()
    {
        NamedPipeClientStream pipeClient = new(
            serverName: ".", pipeName: SingleInstancePipeName, direction: PipeDirection.Out, options: PipeOptions.None,
            impersonationLevel: TokenImpersonationLevel.Identification);

        try
        {
            pipeClient.Connect(timeout: 3000);
        }
        catch (TimeoutException ex)
        {
            MessageBox.Show(text: PipeErrorConnectionTimeout + ex.Message);
            return;
        }
        catch (IOException ex)
        {
            MessageBox.Show(text: PipeErrorIoError + ex.Message);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(text: PipeErrorAccessDenied + ex.Message);
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: PipeErrorUnexpectedError + ex.Message);
            return;
        }

        // Could not get hold of server - abort
        if (!pipeClient.IsConnected)
        {
            MessageBox.Show(text: PipeErrorCouldNotConnect);
            return;
        }

        try
        {
            using StreamWriter streamer = new(stream: pipeClient);
            streamer.WriteLine(value: "Could not connect to other GTN instance");
            streamer.Flush();
            streamer.Close();
        }
        catch (IOException ex)
        {
            MessageBox.Show(text: PipeErrorIoError + ex.Message);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: PipeErrorUnexpectedError + ex.Message);
        }
        finally
        {
            pipeClient.Close();
        }
    }


    private class Options
    {
        [Option(shortName: 'c', longName: "collection", Required = false, HelpText = "Location (full path) of a text file that has the list of files to process, one per line.")]
        public string Collection { get; set; }
    }
}