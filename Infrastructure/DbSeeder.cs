using Microsoft.EntityFrameworkCore;
using AppLogger.API.Infrastructure.Entities;

namespace AppLogger.API.Infrastructure;

public static class DbSeeder
{
    private static readonly Random _random = new();

    // previously 1_000_000
    private const int InitialLogCount = 5_000;

    // previously 50_000
    private const int DailyLogCount = 500;

    public static void Seed(AppLoggerContext context)
    {
        var cst = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var nowCst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cst);
        var today = nowCst.Date;
        var servers = new[] { "Server1", "Server2", "Server3", "Server4", "Server5" };
        var apps = new[] { "AppLogger.API", "AnotherApp", "ThirdApp", "FourthApp", "FifthApp" };
        var appsNoErrors = new[] { "HealthCheck.Service", "Metrics.Reporter", "Scheduler.Service", "Queue.Processor", "Cache.Cleaner", "Backup.Manager", "Monitor.Agent" };
        var sourceContexts = new[] { 
            "Controllers.LoggingController", 
            "Features.Logging.GetLogs", 
            "Infrastructure.AppLoggerContext", 
            "Controllers.HomeController", 
            "Features.Auth.Login",
            "Startup",
            "Program",
            "Middleware.ErrorHandler",
            "Services.UserService",
            "Repositories.LogRepository",
            "Api.v1.Controllers.SearchController",
            "Api.v1.Controllers.AdminController",
            "Core.Domain.Entities.User",
            "Core.Application.Services.Authentication.TokenService",
            "Core.Application.Services.Authentication.PasswordHasher",
            "Infrastructure.Persistence.Repositories.GenericRepository",
            "Infrastructure.ExternalServices.EmailService",
            "Infrastructure.ExternalServices.NotificationHub",
            "Features.Users.Commands.CreateUser.CreateUserCommandHandler",
            "Features.Users.Queries.GetUserById.GetUserByIdQueryHandler",
            "Features.Dashboard.Queries.GetStatistics.GetStatisticsQueryHandler",
            "Presentation.WebAPI.Controllers.v2.Analytics.AdvancedReportsController",
            "Core.Application.BusinessLogic.OrderProcessing.OrderValidation.InventoryValidator",
            "Infrastructure.Integration.ThirdParty.PaymentGateways.Stripe.StripePaymentProcessor",
            "Core.Application.Features.Orders.EventHandlers.OrderCreated.SendConfirmationEmailHandler",
            "Infrastructure.Messaging.RabbitMQ.Consumers.OrderProcessing.OrderCreatedConsumer",
            "Core.Application.Services.Notifications.Email.Templates.OrderConfirmation.OrderConfirmationTemplateBuilder",
            "Infrastructure.ExternalServices.CloudStorage.AWS.S3.Managers.FileUpload.LargeFileMultipartUploadManager",
            "Core.Application.BusinessLogic.Inventory.StockManagement.AutomatedReordering.SupplierIntegration.PurchaseOrderGenerator"
        };
        var levels = new[] { "Information", "Warning", "Error", "Debug" };
        var levelsNoError = new[] { "Information", "Warning", "Debug" };
        var accounts = new[] { "admin", "user1", "user2", "user3", null };

        // Seed logs spread over the last 7 days with strictly increasing timestamps, if the database is empty
        if (!context.Logs.Any())
        {
            var initialLogs = new List<Log>(capacity: InitialLogCount);
            var start = today.AddDays(-7); // start 7 days ago at 00:00 CST
            var end = today.AddDays(1);    // exclusive end (tomorrow 00:00 CST)
            var totalSeconds = (end - start).TotalSeconds; // seconds across 8 days boundary but we want 7 days inclusive to before today end; adjust
            start = today.AddDays(-7);
            end = today; // end at start of today for 7 days window (CST)
            totalSeconds = (end - start).TotalSeconds; // exactly 7 days
            var increment = totalSeconds / InitialLogCount; // seconds per log

            double offsetSeconds = 0d;
            for (int i = 0; i < InitialLogCount; i++)
            {
                var logTime = start.AddSeconds(offsetSeconds);
                offsetSeconds += increment;

                var messageLength = _random.Next(100, 2001);
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
                    Exception = _random.Next(0, 10) < 3 ? "System.Exception: Something went wrong" : null,
                    Account = accounts[_random.Next(accounts.Length)]
                };

                initialLogs.Add(log);
            }
            context.Logs.AddRange(initialLogs);
            context.SaveChanges();
        }

        // Every day, if no logs exist for today, add logs for today with strictly increasing timestamps
        bool anyLogsForToday = context.Logs.Any(l => l.LogTime.Date == today);
        if (!anyLogsForToday)
        {
            var todaysLogs = new List<Log>(capacity: DailyLogCount);
            var start = today;                 // today 00:00 CST
            var end = today.AddDays(1);        // tomorrow 00:00 CST
            var totalSeconds = (end - start).TotalSeconds; // 86400 seconds typically
            var increment = totalSeconds / DailyLogCount;

            double offsetSeconds = 0d;
            for (int i = 0; i < DailyLogCount; i++)
            {
                var logTime = start.AddSeconds(offsetSeconds);
                offsetSeconds += increment;

                var messageLength = _random.Next(100, 2001);
                var message = GenerateRandomMessage(messageLength);

                // 70% chance of regular app, 30% chance of no-error app
                bool useNoErrorApp = _random.Next(0, 100) < 30;
                string selectedApp;
                string selectedLevel;

                if (useNoErrorApp)
                {
                    selectedApp = appsNoErrors[_random.Next(appsNoErrors.Length)];
                    selectedLevel = levelsNoError[_random.Next(levelsNoError.Length)];
                }
                else
                {
                    selectedApp = apps[_random.Next(apps.Length)];
                    selectedLevel = levels[_random.Next(levels.Length)];
                }

                var log = new Log
                {
                    LogTime = logTime,
                    LogDay = (byte)logTime.Day,
                    Server = servers[_random.Next(servers.Length)],
                    App = selectedApp,
                    SourceContext = sourceContexts[_random.Next(sourceContexts.Length)],
                    Level = selectedLevel,
                    Message = message,
                    Exception = _random.Next(0, 10) < 3 ? "System.Exception: Something went wrong" : null,
                    Account = accounts[_random.Next(accounts.Length)]
                };

                todaysLogs.Add(log);
            }
            context.Logs.AddRange(todaysLogs);
            context.SaveChanges();
        }
    }

    // Delete logs older than retentionDays to keep the demo database from growing unbounded
    public static void PruneOldLogs(AppLoggerContext context, int retentionDays = 14)
    {
        var cst = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var nowCst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cst);
        var cutoff = nowCst.Date.AddDays(-retentionDays);

        context.Logs.Where(l => l.LogTime < cutoff).ExecuteDelete();
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