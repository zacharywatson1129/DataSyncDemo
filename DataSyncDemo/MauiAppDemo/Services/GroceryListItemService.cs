using DataSyncLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiAppDemo.Services
{

    /// <summary>
    /// This class serves as a "holder" of sorts for items whenever we need
    /// to switch between different views and pass data around. It may not be
    /// the most robust or scalable approach, but it's simple enough for a demo.
    /// It's registered as a Singleton in the MAUIProgram.cs startup method, so 
    /// we have one instance in the application that we can ask for. We use it
    /// everywhere and so we have the same data to use. That way whenever we 
    /// make a new item, we can get it back in the home page, and use the home
    /// page soley for api calls. I want to keep the separate pages doing mostly
    /// just simple things (not api calls).
    /// </summary>
    public class GroceryListItemService
    {
        public GroceryListItem groceryListItem { get; set; } = new GroceryListItem();
        public bool justAddedNewItem { get; set; } = false;
    }
}
