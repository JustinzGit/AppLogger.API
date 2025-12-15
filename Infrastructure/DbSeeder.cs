using Sauron.API.Infrastructure.Entities;

namespace Sauron.API.Infrastructure;

public static class DbSeeder
{
    private static readonly Random _random = new();

    public static void Seed(SauronContext context)
    {

        var today = DateTime.UtcNow.Date;
        var servers = new[] { "Server1", "Server2", "Server3", "Server4", "Server5" };
        var apps = new[] { "Sauron.API", "AnotherApp", "ThirdApp", "FourthApp", "FifthApp" };
        var sourceContexts = new[] { "Controllers.LoggingController", "Features.Logging.GetLogs", "Infrastructure.SauronContext", "Controllers.HomeController", "Features.Auth.Login" };
        var levels = new[] { "Information", "Warning", "Error", "Debug" };
        var accounts = new[] { "admin", "user1", "user2", "user3", null };

        // Seed 1,000,000 logs if the database is empty
        if (!context.Logs.Any())
        {
            var initialLogs = new List<Log>();
            for (int i = 0; i < 1_000_000; i++)
            {
                var logTime = DateTime.UtcNow.AddDays(-_random.Next(0, 30)).AddHours(-_random.Next(0, 24)).AddMinutes(-_random.Next(0, 60));
                var messageLength = _random.Next(100, 2001); // Up to 2000 characters
                var message = GenerateRandomMessage(messageLength);

                var log = new Log
                {
                    LogTime = logTime,
                    LogDay = (byte)logTime.Day,
                    Server = servers[_random.Next(servers.Length)],
                    App = apps[_random.Next(apps.Length)],
                    SourceContext = sourceContexts[_random.Next(sourceContexts.Length)],
                    Level = levels[_random.Next(levels.Length)],
                    Message = message,
                    Exception = _random.Next(0, 10) < 3 ? "System.Exception: Something went wrong" : null, // 30% chance of exception
                    Account = accounts[_random.Next(accounts.Length)]
                };

                initialLogs.Add(log);
            }
            context.Logs.AddRange(initialLogs);
            context.SaveChanges();
        }

        // Every day, if no logs exist for today, add 50,000 logs for today
        bool anyLogsForToday = context.Logs.Any(l => l.LogTime.Date == today);
        if (!anyLogsForToday)
        {
            var todaysLogs = new List<Log>();
            for (int i = 0; i < 50_000; i++)
            {
                var logTime = today
                    .AddHours(_random.Next(0, 24))
                    .AddMinutes(_random.Next(0, 60))
                    .AddSeconds(_random.Next(0, 60));
                var messageLength = _random.Next(100, 2001); // Up to 2000 characters
                var message = GenerateRandomMessage(messageLength);

                var log = new Log
                {
                    LogTime = logTime,
                    LogDay = (byte)logTime.Day,
                    Server = servers[_random.Next(servers.Length)],
                    App = apps[_random.Next(apps.Length)],
                    SourceContext = sourceContexts[_random.Next(sourceContexts.Length)],
                    Level = levels[_random.Next(levels.Length)],
                    Message = message,
                    Exception = _random.Next(0, 10) < 3 ? "System.Exception: Something went wrong" : null, // 30% chance of exception
                    Account = accounts[_random.Next(accounts.Length)]
                };

                todaysLogs.Add(log);
            }
            context.Logs.AddRange(todaysLogs);
            context.SaveChanges();
        }

        var logs = new List<Log>();
        for (int i = 0; i < 50_000; i++)
        {
            var logTime = today
                .AddHours(_random.Next(0, 24))
                .AddMinutes(_random.Next(0, 60))
                .AddSeconds(_random.Next(0, 60));
            var messageLength = _random.Next(100, 2001); // Up to 2000 characters
            var message = GenerateRandomMessage(messageLength);

            var log = new Log
            {
                LogTime = logTime,
                LogDay = (byte)logTime.Day,
                Server = servers[_random.Next(servers.Length)],
                App = apps[_random.Next(apps.Length)],
                SourceContext = sourceContexts[_random.Next(sourceContexts.Length)],
                Level = levels[_random.Next(levels.Length)],
                Message = message,
                Exception = _random.Next(0, 10) < 3 ? "System.Exception: Something went wrong" : null, // 30% chance of exception
                Account = accounts[_random.Next(accounts.Length)]
            };

            logs.Add(log);
        }

        context.Logs.AddRange(logs);
        context.SaveChanges();
    }

    private static string GenerateRandomMessage(int length)
    {
        int messageType = _random.Next(0, 4); // 0: random text, 1: exception, 2: JSON, 3: mixed text + JSON
        return messageType switch
        {
            0 => GenerateRandomText(length),
            1 => GenerateExceptionMessage(),
            2 => GenerateJsonMessage(),
            3 => GenerateRandomText(length / 2) + " " + GenerateJsonMessage(),
            _ => GenerateRandomText(length),
        };
    }

    private static string GenerateRandomText(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[_random.Next(chars.Length)];
        }
        return new string(result);
    }

    private static string GenerateExceptionMessage()
    {
        var exceptions = new[]
        {
            "System.ArgumentNullException: Value cannot be null.\nParameter name: input\n   at SomeNamespace.SomeClass.ValidateInput(String input) in C:\\Projects\\App\\Utils.cs:line 25\n   at SomeNamespace.Controller.HandleRequest() in C:\\Projects\\App\\Controllers\\MainController.cs:line 45",
            "System.InvalidOperationException: Operation is not valid due to the current state of the object.\n   at System.Collections.Generic.List`1.Insert(Int32 index, T item)\n   at SomeApp.Services.DataService.ProcessData() in C:\\App\\Services\\DataService.cs:line 78",
            "System.IO.FileNotFoundException: Could not find file 'C:\\Logs\\app.log'.\n   at System.IO.FileStream..ctor(String path, FileMode mode, FileAccess access, FileShare share, Int32 bufferSize, FileOptions options)\n   at Logging.Logger.WriteLog(String message) in C:\\App\\Logging\\Logger.cs:line 12",
            "System.Net.WebException: The remote server returned an error: (404) Not Found.\n   at System.Net.HttpWebRequest.GetResponse()\n   at ApiClient.FetchData(String url) in C:\\App\\ApiClient.cs:line 33"
        };
        return exceptions[_random.Next(exceptions.Length)];
    }

    private static string GenerateJsonMessage()
    {
        var jsons = new[]
        {
            "{\"userId\": 12345, \"action\": \"login\", \"timestamp\": \"2023-10-01T12:00:00Z\", \"details\": {\"ip\": \"192.168.1.100\", \"browser\": \"Chrome\", \"version\": \"118.0\"}}",
            "{\"event\": \"error\", \"code\": 500, \"message\": \"Internal server error\", \"stack\": [\"at Controller.cs:45\", \"at Service.cs:22\"]}",
            "{\"requestId\": \"abc-123-def\", \"method\": \"POST\", \"endpoint\": \"/api/logs\", \"responseTime\": 150, \"status\": 200}",
            "{\"logLevel\": \"Warning\", \"component\": \"AuthService\", \"data\": {\"user\": \"admin\", \"attempts\": 3}}"
        };
        return jsons[_random.Next(jsons.Length)];
    }
}