using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoTagNinja.Model
{
    public class DirectoryElementCollection: List<DirectoryElement>
    {

        /// <summary>
        ///     Searches through the list of directory elements for the element
        ///     with the given item name. If nothing is found, return null.
        /// </summary>
        /// <param name="itemName">The file name to search for</param>
        public DirectoryElement FindElementByItemName(string itemName)
        {
            foreach (DirectoryElement item in this)
            {
                if (item.ItemName == itemName)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        ///     Adds a DirectoryElement to this list. Hereby it is checked, whether
        ///     already an item with the same name exists. If this is the case,
        ///     either replace it with the one passed (replaceIfExists must be se to
        ///     true) or an ArgumentException is thrown.
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="replaceIfExists">Whether in case of already existing item the existing
        /// item should be replace (or an exception thrown)</param>
        public void Add(DirectoryElement item, bool replaceIfExists)
        {
            DirectoryElement exstgElement = FindElementByItemName(item.ItemName);
            if (exstgElement != null)
            {
                if (replaceIfExists) {
                    this.Remove(exstgElement);
                }
                else {
                    throw new ArgumentException(
                        string.Format("Error when adding element '{0}': the item must be unique but already exists in collection.",
                        item.ItemName));
                }
                base.Add(item);
            }
        }
    }
}
