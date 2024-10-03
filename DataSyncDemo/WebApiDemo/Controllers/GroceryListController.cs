using DataSyncLibrary;
using DataSyncLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;


namespace WebApiDemo.Controllers
{
    [Route("api/[controller]")] // api/GroceryList
    [ApiController]
    public class GroceryListController : ControllerBase
    {
        DataAccess dataAccess;

        public GroceryListController(DataAccess _dataAccess)
        {
            dataAccess = _dataAccess;
            string currentDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine("Inside GroceryListController class, Current Working Directory: " + currentDirectory);
            Console.WriteLine("using connection string: " + _dataAccess.ConnectionString);
        }

        

        // GET: api/<GroceryList>
        [HttpGet]
        public ActionResult<List<GroceryListItem>>  GetGroceryList()
        {
            List<GroceryListItem> groceryList = dataAccess.GetGroceryList();
            Console.WriteLine($"We are retrieving the grocery list. It has {groceryList.Count} items");
            return Ok(groceryList);
        }


        // POST api/GroceryList
        [HttpPost]
        public ActionResult<GroceryListItem> AddGroceryListItem([FromBody] GroceryListItem value)
        {
            if (System.IO.File.Exists("groceryListDB.db"))
            {
                Console.WriteLine("Database file found.");
            }
            else
            {
                Console.WriteLine("Database file not found.");
            }

            List<GroceryListItem> currList = dataAccess.GetGroceryList();

            if (!currList.Contains(value))
            {
                dataAccess.AddItem(value);
                return CreatedAtAction(nameof(GetGroceryList), new { id = value.Id }, value);
            }
            return BadRequest("This item already exists in the list.");
        }

        // PUT api/Users/5
        /*[HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }*/

        // DELETE api/Users/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            dataAccess.DeleteItem(id);
        }
    }
}
