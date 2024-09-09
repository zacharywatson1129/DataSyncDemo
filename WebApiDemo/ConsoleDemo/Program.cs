using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApiDemo;

namespace ConsoleDemo
{
    public class Program
    {
        private static IHost _apiHost;
        private static string apiUrl { get; set; } = string.Empty;
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

        /*private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel()
                              .UseUrls(apiUrl) // Adjust port if needed
                              .UseStartup<WebApiDemo.Program>(); // Reference to your Web API's Startup class
                });
        }*/

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

        /// <summary>
        /// Starts the Web API. Configures Swagger, Kestrel, etc.
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

            var app = builder.Build();

            // Configure the HTTP request pipeline (middleware)
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // Map controller routes
                endpoints.MapControllers();
            });


            // ----------------------------------------------------------
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // We need these to be able to explore the API through the browser.
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            try
            {
                app.Run(apiUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting Web API: {ex.Message}");
            }

            Console.WriteLine("Web api has started...");
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("This is our console demo that starts and uses the Web API.");

            // We do this first always to ensure we have the correct url.
            Console.WriteLine("Loading settings (apiUrl)");
            LoadAppSettings();
            // _apiHost = CreateHostBuilder(args).Build();
            var apiTask = Task.Run(() => StartWebApi());

            await Task.Delay(1000);

            // Now calling the api just assumes that api is running. So we still do all of this.
            // We ARE assuming the api is running, we will get an exception if it is not.
            var client = new HttpClient();
            client.BaseAddress = new Uri(apiUrl);

            Console.WriteLine("Enter a user number to retrieve that user: ");
            int numberInput = Convert.ToInt32(Console.ReadLine());

            try
            {
                //string text = await client.GetStringAsync($"{ apiUrl}/api/users/{numberInput}");
                string text = await client.GetStringAsync($"/api/users/{numberInput}");
                Console.WriteLine($"Retrieving {text}");
                //errorString = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " and inner ex: " + ex.InnerException);
                //errorString = $"There was an error getting our forecast: {ex.Message}";
            }

            Console.WriteLine("Shutting down Web API...");
            Environment.Exit(0); // Shuts down both the console and the web API
        }
    }
}
