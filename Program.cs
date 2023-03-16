using System;
using System.IO;
using System.Windows.Forms;
using CommandLine;

namespace GeoTagNinja;

internal static class Program
{
    public static string collectionFileLocation = null;
    public static bool collectionModeEnabled = false;

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

        Application.Run(mainForm: new FrmMainApp());
    }

    private class Options
    {
        [Option(shortName: 'c', longName: "collection", Required = false, HelpText = "Location (full path) of a text file that has the list of files to process, one per line.")]
        public string Collection { get; set; }
    }
}