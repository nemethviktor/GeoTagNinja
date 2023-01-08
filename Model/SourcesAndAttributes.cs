using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoTagNinja.Model
{
    public class SourcesAndAttributes
    {

        /// <summary>
        /// Lists the defined sources for metadata as string constants
        /// </summary>
        public enum Sources
        {
            ExifImageFile = 0,
            SidecarFile = 1,
            Unknown = 999
        }


        public static Sources[] SOURCES_LIST = new Sources[2] {
            Sources.ExifImageFile,
            Sources.SidecarFile
        };


        public enum ConsolidatedAttributes
        {
            Longitude = 0,
            Latitude = 1,
            Camera = 2,
            Undefined = 999
        }


        /// <summary>
        /// Declaration dictionary for which Tags from the source are mapped
        /// to which consolidated attributes.
        /// </summary>
        private static IDictionary<Sources, IDictionary<string, ConsolidatedAttributes>> _Source2CAMapping =
            new Dictionary<Sources, IDictionary<string, ConsolidatedAttributes>>()
            {
                // Mapping for EXIF Image File
                {
                    Sources.ExifImageFile, new Dictionary<string, ConsolidatedAttributes> ()
                    {
                        { "Tag", ConsolidatedAttributes.Longitude }
                    }
                },

                // Mapping for Sidecar File
                {
                    Sources.SidecarFile, new Dictionary<string, ConsolidatedAttributes> ()
                    {
                        { "DummyTag", ConsolidatedAttributes.Longitude }
                    }
                }
            };


        private static IDictionary<ConsolidatedAttributes, IDictionary<Sources, List<string>>> _CA2SourceMapping = null;

        private static void BuildCA2SourceMapping()
        {
            _CA2SourceMapping = new Dictionary<ConsolidatedAttributes, IDictionary<Sources, List<string>>>();

            // At least have a dict per CA - maybe empty at the end...
            foreach (ConsolidatedAttributes attribute in Enum.GetValues(typeof(ConsolidatedAttributes)).Cast<ConsolidatedAttributes>())
                _CA2SourceMapping[attribute] = new Dictionary<Sources, List<string>>();


            // Iterate over _Source2CAMapping and add values in _CA2SourceMapping
            Sources source = Sources.Unknown;
            foreach (KeyValuePair<Sources, IDictionary<string, ConsolidatedAttributes>> sourceentry in _Source2CAMapping)
            {
                source = sourceentry.Key;
                foreach (KeyValuePair<string, ConsolidatedAttributes> entry in sourceentry.Value)
                {

                    // Ensure the string list is available
                    if (!(_CA2SourceMapping[entry.Value]).ContainsKey(source))
                        _CA2SourceMapping[entry.Value] = new Dictionary<Sources, List<string>>();
                    // Add entry to it
                    (_CA2SourceMapping[entry.Value])[source].Add(entry.Key);
                }
            }
        }


        public static IDictionary<Sources, IDictionary<string, ConsolidatedAttributes>> Source2CAMapping { get => _Source2CAMapping; }

        /// <summary>
        /// Defines which attributes per source are to be identified with a
        /// consolidated attribute.
        /// 
        /// Dict -> Dict:
        /// ConsAttr --> (Source --> Array of Attributes)
        /// </summary>
        public static IDictionary<ConsolidatedAttributes, IDictionary<Sources, List<string>>> GetCA2SourceMapping()
        {
            if (_CA2SourceMapping == null)
            {
                BuildCA2SourceMapping();
            }
            return _CA2SourceMapping;
        }
        

        private static class SourcePriorityRules
        {
            public static Sources[] GENERAL_PRIOS = new Sources[2] {
            Sources.ExifImageFile,
            Sources.SidecarFile
            };
        }

    }
}
