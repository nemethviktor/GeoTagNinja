using CommandLine;
using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

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
    public static string FolderToLaunchIn;
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

        _ = Parser.Default.ParseArguments<OptionsMutuallyExclusive>(args: args)
              .WithParsed(action: o =>
               {
                   if (File.Exists(path: o.Collection))
                   {
                       CollectionFileLocation = o.Collection;
                       CollectionModeEnabled = true;
                   }

                   if (!string.IsNullOrWhiteSpace(value: o.Folder))
                   {
                       // o.Folder can be a number of things depending on how the user executes, ie could be -f C:\temp or -f=C:\temp or "" enclosed etc
                       // yes i know this can be doen in one step but it's easier to debug/follow like thi

                       // 1. remove any ""s
                       string trimmedPath = o.Folder.Replace(oldValue: "\"", newValue: "");
                       // 2. remove "=" if any
                       trimmedPath = trimmedPath.TrimStart('=');
                       // 3. remove "\" from the end.
                       trimmedPath = trimmedPath.TrimEnd(Convert.ToChar(value: "\\"));

                       if (Directory.Exists(path: trimmedPath))
                       {
                           FolderToLaunchIn = trimmedPath;
                       }
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
            _ = MessageBox.Show(text: PipeErrorAbandonedMutexException);
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
            _ = MessageBox.Show(text: PipeErrorConnectionTimeout + ex.Message);
            return;
        }
        catch (IOException ex)
        {
            _ = MessageBox.Show(text: PipeErrorIoError + ex.Message);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            _ = MessageBox.Show(text: PipeErrorAccessDenied + ex.Message);
            return;
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show(text: PipeErrorUnexpectedError + ex.Message);
            return;
        }

        // Could not get hold of server - abort
        if (!pipeClient.IsConnected)
        {
            _ = MessageBox.Show(text: PipeErrorCouldNotConnect);
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
            _ = MessageBox.Show(text: PipeErrorIoError + ex.Message);
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show(text: PipeErrorUnexpectedError + ex.Message);
        }
        finally
        {
            pipeClient.Close();
        }
    }

    /// <summary>
    ///     Note to self / from https://github.com/commandlineparser/commandline/wiki/Mutually-Exclusive-Options
    ///     "If you combine a SetName1 option with a SetName2 one, parsing will fail. Options in the SAME set can be combined
    ///     together,
    ///     but options cannot be combined across sets"
    /// </summary>
    private class OptionsMutuallyExclusive
    {
        [Option(shortName: 'c', longName: "collection", Required = false, SetName = "collection",
            HelpText = "Location (full path) of a text file that has the list of files to process, one per line.")]
        public string Collection { get; set; }

        [Option(shortName: 'f', longName: "folder", Required = false, SetName = "folder",
            HelpText = "Folder to open GTN with (likely use with a SendTo command)")]
        public string Folder { get; set; }
    }
}