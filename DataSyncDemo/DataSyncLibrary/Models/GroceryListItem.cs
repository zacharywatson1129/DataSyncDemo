using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSyncLibrary.Models
{
    public class GroceryListItem
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        // Quantity will be kept a string here to avoid the need for units.
        // For example, to avoid confusion if grocery item is "eggs", this
        // will keep from being confused as to whether the item is 2 eggs
        // or 2 dozen eggs--that would probably require a unit of some type.
        public string? Quantity { get; set; }

        /// <summary>
        /// Used for displaying, such as in list printouts.
        /// </summary>
        public string DisplayText { get { return ($"{Quantity} {Name}"); } }
    }
}
