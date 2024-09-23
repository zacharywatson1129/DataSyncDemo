using System;
using System.Linq.Expressions;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApiDemo;

namespace ConsoleDemo
{
    public class Program
    {
        private static List<string> groceryList = new List<String>();
        private static IHost _apiHost;
        private static CancellationTokenSource _cancellationTokenSource;
        private static string apiUrl { get; set; } = string.Empty;

        private static bool isAPIRunning = false;


        private static void LoadDemoGroceryList()
        {
            groceryList.Add("milk");
            groceryList.Add("eggs");
            groceryList.Add("orange juice");
            groceryList.Add("cheese");
        }

        private static void LoadAppSettings()
        {
            // This basically loads our appsettings.json file
            // and creates an IConfigurationRoot object that holds
            // the settings.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Now we set the api url.
            if (configuration["AppSettings:ApiURL"] != null)
                apiUrl = configuration["AppSettings:ApiURL"] as string;
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

            // Add services to the container
            builder.Services.AddControllers(); // Enable controllers

            // -------------------------------------------
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            _webApp = builder.Build();

            // Configure the HTTP request pipeline (middleware)
            _webApp.UseRouting();

            _webApp.UseEndpoints(endpoints =>
            {
                // Map controller routes
                endpoints.MapControllers();
            });


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
                // _webApp.Run(apiUrl);
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

        static void PrintGroceryList()
        {
            Console.Clear();
            for (int i = 0; i < groceryList.Count; i++)
            {
                Console.WriteLine($"{i + 1} {groceryList[i]}");
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
            Console.WriteLine("\t a) Connect to API");
            Console.WriteLine("\t b) Disconnect from API");
            Console.WriteLine("\t c) Issue POST command");
            Console.WriteLine("\t d) Print Grocery List");
            Console.WriteLine("\t e) Add item to Grocery List");
            Console.WriteLine("\t f) Remove item from Grocery List");
            Console.WriteLine("\t q) Exit program");
        }

        /// <summary>
        /// Takes in input by the user and peroforms correct action.
        /// If bad input is entered, the console displays an error.
        /// </summary>
        /// <returns>Return 0 if user did not enter exit key. Return 1 if exit key was pressed.</returns>
        static async Task<int> ParseInput()
        {
            try
            {
                string input = Console.ReadLine();
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
                Console.WriteLine($"Error parsing input. Please enter valid choice.");
                return 0;
            }
        }

        private static async Task UseApiGet()
        {
            Console.Clear();
            // We do this first always to ensure we have the correct url.
            Console.WriteLine("Loading settings (apiUrl)");
            LoadAppSettings();

            await Task.Delay(1000);

            // Now calling the api just assumes that api is running. So we still do all of this.
            // We ARE assuming the api is running, we will get an exception if it is not.
            var client = new HttpClient();
            client.BaseAddress = new Uri(apiUrl);

            Console.WriteLine("Attempted to connect to the Web API....");

            Console.WriteLine("Enter a user number to retrieve that user: ");
            int numberInput = Convert.ToInt32(Console.ReadLine());

            try
            {
                //string text = await client.GetStringAsync($"{ apiUrl}/api/users/{numberInput}");
                string text = await client.GetStringAsync($"/api/users/{numberInput}");
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

        static async Task Main(string[] args)
        {
            LoadDemoGroceryList();

            int codeReturned = 0;

            while (codeReturned == 0)
            {
                DisplayOptions();
                codeReturned = await ParseInput();
            }

            
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
                    Console.WriteLine("Please enter a valid item number...");
                }
            }
        }

        /// <summary>
        /// Displays Menu to Enter a new item.
        /// </summary>
        private static void EnterNewItem()
        {
            Console.Clear();

            Console.WriteLine("Current grocery list:\n");
            for (int i = 0; i < groceryList.Count; i++)
            {
                Console.WriteLine($"{i + 1} {groceryList[i]}");
            }

            Console.WriteLine("\nPlease enter a new grocery list item: ");
            string item = Console.ReadLine();
            groceryList.Add(item);
            Console.Clear();
            Console.WriteLine("\nUpdated list:\n");
            for (int i = 0; i < groceryList.Count; i++)
            {
                Console.WriteLine($"{i + 1} {groceryList[i]}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadLine();
        }
    }
}
