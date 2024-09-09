
namespace WebApiDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            //Directory.SetCurrentDirectory(@"the directory of WebApi Project");
            //var builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = "Development", ApplicationName = "WebApi" });


            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure Kestrel to use HTTPS
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(7140); // HTTP
                options.ListenAnyIP(7141, listenOptions =>
                {
                    listenOptions.UseHttps(); // HTTPS
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();


            /**
             * 
             *  var builder = WebApplication.CreateBuilder(args);

                // Configure Kestrel to use HTTPS
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(5000); // HTTP
                    options.ListenAnyIP(5001, listenOptions =>
                    {
                        listenOptions.UseHttps(); // HTTPS
                    });
                });

                var app = builder.Build();

                // Configure the HTTP request pipeline here...

                app.Run();
             * 
             */

            app.Run();
        }
    }
}
