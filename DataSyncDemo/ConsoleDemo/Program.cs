using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DataSyncLibrary;
using DataSyncLibrary.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using WebApiDemo;

namespace ConsoleDemo
{
    public class Program
    {
        /// <summary>
        /// This acts as our database for this demo. However, 
        /// </summary>
        private static DataAccess dataAccess;
        private static List<GroceryListItem> groceryList;  
        private static IHost _apiHost;
        private static CancellationTokenSource _cancellationTokenSource;
        private static string apiUrl { get; set; } = string.Empty;
        private static bool isAPIRunning = false;

        private static string LoadIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "";
        }

        private static string LoadPortNumber()
        {
            // This basically loads our appsettings.json file
            // and creates an IConfigurationRoot object that holds
            // the settings.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string portNumber = "";

            // Now we set the api url.
            if (configuration["AppSettings:PortNumber"] != null)
                return configuration["AppSettings:PortNumber"];
            
            return "";
        }

        /// <summary>
        /// Loads the IP Address and Port Number, and puts them together to generate the URL to start the API at.
        /// The result is to set the value of apiUrl. 
        /// </summary>
        private static void LoadApiURL()
        {
            string ipAddress = LoadIPAddress();
            string portNumber = LoadPortNumber();
            
            apiUrl = $"{ipAddress}:{portNumber}";
        }

        /// <summary>
        /// Gets the directory containing the WebApiDemo project.
        /// </summary>
        /// <returns>A string containing the directory of the WebApiDemo project.</returns>
        private static string GetWebApiProjectDirectory()
        {
            // Get the base directory (where the console app is running from)
            var baseDir = AppContext.BaseDirectory;

            // Go up two levels from the bin folder to the solution directory
            var solutionDir = Path.Combine(baseDir, "../../../../");

            // Combine the solution directory with the WebApiDemo folder
            var webApiProjectDir = Path.Combine(solutionDir, "WebApiDemo");

            return Path.GetFullPath(webApiProjectDir); // Get the full path
        }

        private static WebApplication? _webApp;

        /// <summary>
        /// Starts the Web API asynchronously--we do not await it, we allow it to run in background. 
        /// This method also Configures Swagger, Kestrel, etc. It performs all basic Program.cs tasks
        /// of the WebAPI project.
        /// </summary>
        private static void StartWebApi()
        {
            // First, let's change our current directory to be that of the web api.
            var webApiProjectDir = GetWebApiProjectDirectory();
            Directory.SetCurrentDirectory(webApiProjectDir);
            
            // Create a builder with some WebApplicationOptions
            // that we specify. 
            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions() 
                { 
                    EnvironmentName = "Development", 
                    ApplicationName = "WebApiDemo" 
                });
            
            // Configure Kestrel to use HTTPS
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(7140); // HTTP
                options.ListenAnyIP(7141, listenOptions =>
                {
                    listenOptions.UseHttps(); // HTTPS
                });
            });

            // We could inject the ConnectionString. However, we need the SAME dataaccess object.

            builder.Services.AddSingleton(dataAccess);

            // Add services to the container
            builder.Services.AddControllers(); // Enable controllers

            // -------------------------------------------
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            _webApp = builder.Build();

            // Configure the HTTP request pipeline (middleware)
            _webApp.UseRouting();

#pragma warning disable ASP0014
            _webApp.UseEndpoints(endpoints =>
            {
                // Map controller routes
                endpoints.MapControllers();
            });
#pragma warning restore ASP0014


            // ----------------------------------------------------------
            // Configure the HTTP request pipeline.
            if (_webApp.Environment.IsDevelopment())
            {
                // We need these to be able to explore the API through the browser.
                _webApp.UseSwagger();
                _webApp.UseSwaggerUI();
            }

            _webApp.UseHttpsRedirection();

            _webApp.UseAuthorization();

            int success = 1;

            Console.Clear();
            Console.WriteLine("Attempting to start the Web API...\n\n");
            try
            {
                _webApp.RunAsync(apiUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting Web API: {ex.Message}");
                success = 0;
            }

            if (success == 1)
            {
                isAPIRunning = true;
                Console.WriteLine("\n\nThe Web api has started successfully!");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }

        /// <summary>
        /// Shut down the Web API.
        /// </summary>
        /// <returns></returns>
        private static async Task StopWebApiAsync()
        {
            if (_webApp != null)
            {
                Console.WriteLine("Shutting down the Web API...");
                await _webApp.StopAsync(); // Gracefully stop the web API
                await _webApp.DisposeAsync(); // Dispose of the WebApplication
                isAPIRunning = false;
                Console.WriteLine("Web API has stopped. Press any key to continue...");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Print the grocery list as a numbered list. Numbers do not correspond to ids.
        /// </summary>
        static async void PrintGroceryList()
        {
            Console.WriteLine("Loading grocery list via the API from the 'database'...");
            LoadGroceryList();
            Console.Clear();

            Console.WriteLine("Current grocery list:\n");

            for (int i = 0; i < groceryList.Count; i++)
            {
                Console.WriteLine($"{i + 1} {groceryList[i].DisplayText}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }

        /// <summary>
        /// Clears out the console and displays the options.
        /// </summary>
        static void DisplayOptions()
        {
            Console.Clear();
            Console.WriteLine("Options (type letter of choice):");
            Console.WriteLine("\t a) Start the API");
            Console.WriteLine("\t b) Stop the API");
            Console.WriteLine("\t c) Issue POST command");
            Console.WriteLine("\t d) Print Grocery List");
            Console.WriteLine("\t e) Add item to Grocery List");
            Console.WriteLine("\t f) Remove item from Grocery List");
            Console.WriteLine("\t q) Exit program");
        }

        /// <summary>
        /// Takes in input by the user and performs correct action.
        /// If bad input is entered, the console displays an error.
        /// </summary>
        /// <returns>Return 0 if user did not enter exit key. Return 1 if exit key was pressed.</returns>
        static async Task<int> ParseInput()
        {
            try
            {
                // Yes, could return null. Yes, we handle that. Hence, null forgiving operator (!).
                string input = Console.ReadLine()!;
                if (input == null)
                {
                    Console.WriteLine("Input string was null.");
                    return 0;
                }
                if (input.Length == 0 || input.Length > 1)
                {
                    throw new Exception("Input should be one character.");
                }
                char keyEntered = input.First();
                switch (keyEntered)
                {
                    case 'a':
                        if (!isAPIRunning)
                            StartWebApi();
                        else
                        {
                            Console.WriteLine("API is already running. Press any key to continue...");
                            Console.ReadLine();
                        }
                        return 0;
                    case 'b':
                        if (isAPIRunning)
                            await StopWebApiAsync();
                        else
                        {
                            Console.WriteLine("API is not running. Press any key to continue....");
                            Console.ReadLine();
                        }
                        return 0;
                    case 'c':
                        if (!isAPIRunning)
                        {
                            Console.WriteLine("Cannot issue a POST command until the API is running. Press any key to continue...");
                            Console.ReadLine();
                            return 0;
                        }
                        await UseApiGet();
                        return 0;
                    case 'd':
                        if (!isAPIRunning)
                        {
                            Console.WriteLine("API is not running, so database calls are not possible right now. Press any key to continue...");
                            Console.ReadLine();
                            return 0;
                        }
                        PrintGroceryList();
                        return 0;
                    case 'e':
                        EnterNewItem();
                        return 0;
                    case 'f':
                        DeleteExistingItem();
                        return 0;
                    case 'q':
                        if (isAPIRunning) // We'll go ahead and shut it down, for safety.
                        {
                            await StopWebApiAsync();
                        }
                        Environment.Exit(0);
                        return 1;
                    default:
                        throw new Exception("Input must be character a-f.");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing input: {ex.Message}. Please enter valid choice.");
                return 0;
            }
        }

        /// <summary>
        /// Calls the GET command to retrieve an item by ID.
        /// </summary>
        /// <returns></returns>
        private static async Task UseApiGet()
        {
            Console.Clear();
            // We do this first always to ensure we have the correct url.
            Console.WriteLine("Loading settings (apiUrl)");
            LoadApiURL();

            await Task.Delay(1000);

            // Now calling the api just assumes that api is running. So we still do all of this.
            // We ARE assuming the api is running, we will get an exception if it is not.
            var client = new HttpClient();
            client.BaseAddress = new Uri(apiUrl);

            Console.WriteLine("Attempted to connect to the Web API....");

            Console.WriteLine("Enter a grocery item number to retrieve that item: ");
            int numberInput = Convert.ToInt32(Console.ReadLine());

            try
            {
                //string text = await client.GetStringAsync($"{ apiUrl}/api/users/{numberInput}");
                string text = await client.GetStringAsync($"/api/GroceryList/{numberInput}");
                Console.WriteLine($"Retrieving {text}\n");
                Console.WriteLine("Press any key to continue...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " and inner ex: " + ex.InnerException);
                Console.WriteLine("Press any key to continue...");
                Console.ReadLine();
                return;
            }

            client.Dispose();

            Console.WriteLine("Shutting down connection to the API...Press any key to continue...");
            return;
        }

        /// <summary>
        /// Displays menu to Delete an existing item.
        /// </summary>
        private static void DeleteExistingItem()
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine("Current grocery list:\n");
                for (int i = 0; i < groceryList.Count; i++)
                {
                    Console.WriteLine($"{i + 1} {groceryList[i]}");
                }

                Console.WriteLine("\nPlease enter item # to remove: ");
                try
                {
                    int itemNumber = Convert.ToInt32(Console.ReadLine());
                    groceryList.RemoveAt(itemNumber - 1);
                    Console.WriteLine("\nUpdated list:\n");
                    for (int i = 0; i < groceryList.Count; i++)
                    {
                        Console.WriteLine($"{i + 1} {groceryList[i]}");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}. Please enter a valid item number...");
                }
            }
        }

        /// <summary>
        /// Displays Menu to Enter a new item.
        /// Then, we use the API to actually save the item because
        /// for demo purposes, we don't currently have persistent data that
        /// we could synchronize between the console and MAUI app.
        /// </summary>
        private static async void EnterNewItem()
        {
            Console.Clear();

            Console.WriteLine("Current grocery list:\n");
            for (int i = 0; i < groceryList.Count; i++)
            {
                Console.WriteLine($"{i + 1} {groceryList[i].Quantity} {groceryList[i]}");
            }

            Console.WriteLine("\nPlease enter a new grocery list item: ");
            string item = Console.ReadLine();
            
            Console.WriteLine("\nPlease enter the amount (you can also enter units): ");
            string itemAmt = Console.ReadLine();
            // groceryList.Add(item);
            GroceryListItem groceryListItem = new GroceryListItem() { Name = item, Quantity = itemAmt };


            dataAccess.AddItem(groceryListItem);

            LoadGroceryList();


            

            /*await Task.Delay(1000);

            // Now calling the api just assumes that api is running. So we still do all of this.
            // We ARE assuming the api is running, we will get an exception if it is not.
            var client = new HttpClient();
            client.BaseAddress = new Uri(apiUrl);


            try
            {
                var json = JsonSerializer.Serialize(groceryListItem);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/GroceryList", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Finished sending response to api.");
                    Console.WriteLine(responseData);

                    PrintGroceryList();
                }
            }
            catch (Exception ex)
            {
                printException(ex, "There was an error with the API.");
                pauseUntilKeypress();
                return;
            }*/

            
            Console.Clear();
            Console.WriteLine("\nUpdated list:\n");
            groceryList = dataAccess.GetGroceryList();
            for (int i = 0; i < groceryList.Count; i++)
            {
                Console.WriteLine($"{i + 1} {groceryList[i].DisplayText}");
            }
            pauseUntilKeypress();
        }

        /// <summary>
        /// The entry point of the application. All that main() does is load the app settings and display the main
        /// menu options until the user enters the exit key.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            LoadConnectionString();
            dataAccess = new DataAccess(ConnectionString);
            LoadApiURL();

            int codeReturned = 0;

            while (codeReturned == 0)
            {
                DisplayOptions();
                codeReturned = await ParseInput();
            }
        }

        private static string ConnectionString = "";

        private static void LoadConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Now we set the api url.
            if (configuration["AppSettings:ConnectionString"] != null)
                ConnectionString = configuration["AppSettings:ConnectionString"];

            // dataAccess = new DataAccess(ConnectionString);
        }

        private static void LoadGroceryList() => groceryList = dataAccess.GetGroceryList();
        /*{
            groceryList = dataAccess.GetGroceryList();


            /*LoadApiURL();

            await Task.Delay(1000);

            // Now calling the api just assumes that api is running. So we still do all of this.
            // We ARE assuming the api is running, we will get an exception if it is not.
            var client = new HttpClient();
            client.BaseAddress = new Uri(apiUrl);

            Console.WriteLine("Attempted to connect to the Web API....");

            try
            {
                var response = await client.GetAsync($"/api/GroceryList/");

                response.EnsureSuccessStatusCode();

                var responseBody = response.Content.ReadAsStream();

                // We use this method to convert a group of single items into an IAsyncEnumerable type.
                IAsyncEnumerable<GroceryListItem> groceriesAsyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<GroceryListItem>(responseBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));

                // Now we convert to a regular List.
                List<GroceryListItem> groceries = new List<GroceryListItem>(groceriesAsyncEnumerable.ToBlockingEnumerable());

                return groceries;
            }
            catch (Exception ex)
            {
                printException(ex);
                pauseUntilKeypress();
                
                return new List<GroceryListItem>();
            }*/
        /*}*/
        public static void pauseUntilKeypress()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }
        public static void printException(Exception ex, string generalMessage = "")
        {
            Console.WriteLine(generalMessage);
            Console.WriteLine($"Exception Message---\n{ex.Message}");
            Console.WriteLine($"Inner Exception Message---\n{ex.InnerException?.Message}");
        }
    }
}
