using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public string FullPathAndName;
        public string ItemName { get; }
        private string _DisplayName = null;
        public string DisplayName
        {
            get {
                if (_DisplayName == null) return ItemName;
                return _DisplayName;
            }
            set => _DisplayName = value;
        }
        public string Extension;
        public override string ToString() { return ItemName; }

        public DirectoryElement(string itemName, ElementType type)
        {
            ItemName = itemName;
            Type = type;
        }

    }
}
