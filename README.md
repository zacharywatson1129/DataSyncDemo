# DataSyncDemo
Demo for synchronizing data between Maui app and a simple console app via ASP.Net Core Web API.

Originally, the intent of this demo was to allow a console app to communicate with some other app, which I eventually decided on
a MAUI app. Then, I decided I would keep some data in each app that I wanted to synchronize. The point of this demo was to build a conceptual demo, a template of source
to piece into an existing app, where I want to build a mobile app to synchronize with the desktop app, for on the go access. But synchronization would just happen locally.
And if other users download and install that app (it's open source), 

At current, the console app starts up the Web API which is a project in the solution--however, it is not configured via Visual Studio to start up with the console.
Instead the console app contains code that spins up a server that starts up the Web API project. There is also a .NET MAUI app. So in total, there are three projects:
1. The Console App:
      You can start and stop the Web API from the console app through some menu options. By default it is disable. You can also update/delete items from a demo grocery list. 
2. The Web API App
      This will act as the glue to synchronize the data between the apps. At current, it just has the default Controllers, a WeatherForecastController which generates random
      demo weather data and a UsersController, which I created, however it is just demo code that doesn't have real functionality either. It will soon become the GroceriesController.
3. The .NET MAUI App
      At current, it just connects to the Web API. Soon it will be able to update/delete those same items from the list. It will receieve a copy of a list when it connects, and anytime 
      changes are made via either app, the changes will be POSTed through the Web API, where the other app will call a GET command (each app will call both GET commands periodically), to 
      retrieve the changes and update its own data, so essentially the changes will be synchronized in pseudo-real-time.

Likely, the Web API will contain a POST for addition of an item, and POST for deletion of a item, and then there will be a corresponding GET for additions and GET for deletions. However, those items will have to be also be marked so I know whether to ignore them (aka, if the console calls a GET after a POST, and the item was a change it just made, not the MAUI app, ignore the change). Essentially it will be much like a very simple version control system. 

A bonus to this demo would be to make the lists be stored in a database such that
changes would persist and allow offline tweaks to happen to be synchronized when a reconnection occurs should the connection drop. In that case the web server may have some 
database of changes that it would keep, or the console app may have some database that would keep track of the changes until 
Then, some way to publish messages or update a sample database will be figured out.
