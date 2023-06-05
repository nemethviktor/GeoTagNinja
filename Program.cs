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
    public static string collectionFileLocation = null;
    public static bool collectionModeEnabled = false;

    public static string singleInstance_PipeName = "GeoTagNinjaSingleInstance";

    /// <summary>
    /// If true, this is the "first" instance of the program
    /// </summary>
    public static bool singleInstance_Highlander = false;

    /// <summary>
    /// A Mutex that is held by the first instance of the application
    /// </summary>
    static Mutex singleInstance_Mutext = new Mutex(false, "GeoTagNinja.SingleInstance");

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
                    collectionFileLocation = o.Collection;
                    collectionModeEnabled = true;
                }
            });

        // Brief wait in case current instance is exiting
        try
        {
            singleInstance_Highlander = singleInstance_Mutext.WaitOne(1000);
        }
        catch (AbandonedMutexException) {
            // Other application did not release Mutex properly
            Console.WriteLine("AbandonedMutexException");
        }

        if (!singleInstance_Highlander)
        {
            passInfoToRunningInstance();
        } else
        {
            Application.Run(mainForm: new FrmMainApp());
            singleInstance_Mutext.ReleaseMutex();
        }
    }


    /// <summary>
    /// Passes information to the already running first instance of
    /// GTN using a named pipe.
    /// </summary>
    private static void passInfoToRunningInstance()
    {
        NamedPipeClientStream pipeClient = new NamedPipeClientStream(
            ".", singleInstance_PipeName, PipeDirection.Out, PipeOptions.None,
            TokenImpersonationLevel.Identification);

        try
        {
            pipeClient.Connect(3000);
        } catch (TimeoutException) {
        }

        // Could not get hold of server - abort
        if (!pipeClient.IsConnected)
        {
            Console.WriteLine("Could not connect to other GTN instance");
            return;
        }

        using (StreamWriter streamer = new StreamWriter(pipeClient))
        {
            streamer.WriteLine("Test");
            streamer.Flush();
            streamer.Close();
        }

        pipeClient.Close();
    }


    private class Options
    {
        [Option(shortName: 'c', longName: "collection", Required = false, HelpText = "Location (full path) of a text file that has the list of files to process, one per line.")]
        public string Collection { get; set; }
    }
}