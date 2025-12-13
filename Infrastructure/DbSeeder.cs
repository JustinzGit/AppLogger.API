using Sauron.API.Infrastructure;
using Sauron.API.Infrastructure.Models;

namespace Sauron.API.Infrastructure;

public static class DbSeeder
{
    private static readonly Random _random = new Random();

    public static void Seed(SauronContext context)
    {
        if (context.Logs.Any())
        {
            return; // Already seeded
        }

        var servers = new[] { "Server1", "Server2", "Server3", "Server4", "Server5" };
        var apps = new[] { "Sauron.API", "AnotherApp", "ThirdApp", "FourthApp", "FifthApp" };
        var sourceContexts = new[] { "Controllers.LoggingController", "Features.Logging.GetLogs", "Infrastructure.SauronContext", "Controllers.HomeController", "Features.Auth.Login" };
        var levels = new[] { "Information", "Warning", "Error", "Debug" };
        var accounts = new[] { "admin", "user1", "user2", "user3", null };

        var logs = new List<Log>();

        for (int i = 0; i < 500; i++)
        {
            var logTime = DateTime.UtcNow.AddDays(-_random.Next(0, 30)).AddHours(-_random.Next(0, 24)).AddMinutes(-_random.Next(0, 60));
            var messageLength = _random.Next(50, 1001); // Up to 1000 characters
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
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[_random.Next(chars.Length)];
        }
        return new string(result);
    }
}