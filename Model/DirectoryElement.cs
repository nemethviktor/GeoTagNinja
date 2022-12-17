using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace GeoTagNinja.Model
{
    public  class DirectoryElement
    {

        public enum ElementType
        {
            Drive = 0,
            SubDirectory = 1,
            ParentDirectory = 2,
            File = 3,
            Unknown = 99
        }

        public ElementType Type { get; }
        public string FullPathAndName { get; }
        public string ItemName { get; }
        private string _DisplayName = null;

        /// <summary>
        ///     Returns the set display name (text to display). If it was not
        ///     set, it returns the ItemName.
        /// </summary>
        public string DisplayName
        {
            get {
                if (_DisplayName == null) return ItemName;
                return _DisplayName;
            }
            set => _DisplayName = value;
        }

        private string _extension = null;
        public string Extension
        {
            get => _extension;
        }
        public override string ToString() { return ItemName; }

        public DirectoryElement(string itemName, ElementType type, string fullPathAndName)
        {
            ItemName = itemName;
            Type = type;
            FullPathAndName = fullPathAndName;
            _extension = Path.GetExtension(path: FullPathAndName); ;
        }

    }
}
