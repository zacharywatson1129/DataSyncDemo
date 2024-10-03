using Dapper;
using DataSyncLibrary.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSyncLibrary
{
    /// <summary>
    /// A demo data access utility class, which connects to a SQLite database.
    /// We will only have add and delete methods for simplicity. Updates aren't
    /// be included in this demo.
    /// </summary>
    public class DataAccess
    {
        public DataAccess(string cnnString)
        {
            ConnectionString = cnnString;
        }

        public string ConnectionString = "";

        // We will populate this list with some demo default values.
        private List<GroceryListItem> GroceryList = new List<GroceryListItem>() 
        {
            /*new GroceryListItem() { Id = 1, Name = "Milk", Quantity = "1/2 Gallon" },
            new GroceryListItem() { Id = 2, Name = "Eggs", Quantity = "2 Dozen" },
            new GroceryListItem() { Id = 3, Name = "Bread", Quantity = "2 Loaves" }*/
        };

        private long idCounter = 4;

        // An extremely basic method which is really going to just add an item to the grocery list.
        // We utilize an internal counter for creating ids. In real code, we would probably generate
        // some type of GUID for the id.
        public void AddItem(GroceryListItem item)
        {
            using (IDbConnection cnn = new SQLiteConnection(ConnectionString))
            {
                int rows = cnn.Execute("insert into GroceryList (Name, Quantity) values (@Name, @Quantity)", item);
                if (rows >= 1)
                {
                    Console.WriteLine($"We just added item: {item.Quantity} {item.Name}");
                }
            }
            
        }

        public void DeleteItem(long id)
        {
            GroceryList.RemoveAll(x => x.Id == id);
        }

        public GroceryListItem GetItem(long id)
        {
            return GroceryList.Where(x => x.Id == id).First();
        }

        public List<GroceryListItem> GetGroceryList()
        {
            List<GroceryListItem> output;
            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                Console.WriteLine("Inside DataAccess class, Current Working Directory: " + currentDirectory);
                //ConnectionString = currentDirectory + ".\\ConsoleDemo\\bin\\Debug\\net8.0\\groceryListDB.db";
                
                //ConnectionString = Path.Combine(Directory.GetCurrentDirectory(), "..\\ConsoleDemo\\bin\\Debug\\net8.0\\groceryListDB.db");

                // Console.WriteLine("Need to be using: " + currentDirectory + ".\\ConsoleDemo\\bin\\Debug\\net8.0\\groceryListDB.db");
                using (IDbConnection cnn = new SQLiteConnection(ConnectionString))
                {
                    
                    cnn.Open(); // Manually open the connection to check if it's successful

                    

                    string connectionString = "Data Source=groceryListDB.db;";
                    string databasePath = Path.Combine(Directory.GetCurrentDirectory(), "groceryListDB.db");
                    Console.WriteLine("Full Database Path: " + databasePath);


                    Console.WriteLine("Database connection opened successfully.");
                    cnn.Execute("PRAGMA journal_mode = 'WAL';");  // Example PRAGMA command to turn on journaling or debugging features

                    

                    // Check if the table exists
                    var checkTableCmd = "SELECT name FROM sqlite_master WHERE type='table' AND name='GroceryList';";
                    var tableName = cnn.ExecuteScalar<string>(checkTableCmd);

                    if (tableName == null)
                    {
                        Console.WriteLine("Table 'GroceryList' does not exist.");
                        return new List<GroceryListItem>(); // Return empty list if table doesn't exist
                    }

                    Console.WriteLine("Table 'GroceryList' exists, proceeding with query.");

                    

                    output = cnn.Query<GroceryListItem>("select * from GroceryList").ToList();
                    return output;
                }
            }
            catch (Exception ex)
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                Console.WriteLine("Inside DataAccess class, Current Working Directory: " + currentDirectory);
                Console.WriteLine("*********************************");
                Console.WriteLine(ex.Message);
                Console.WriteLine("-----------------------------");
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                    Console.WriteLine("*********************************");
                }
                return new List<GroceryListItem>(); //just return a blank.
            }
        }
    }
}
