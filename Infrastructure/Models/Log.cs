namespace Sauron.API.Infrastructure.Models;

// TODO: check DB schema
public class Log
{
    public int Id { get; set; }

    public DateTime LogTime { get; set; }

    public byte LogDay { get; set; }

    public required string Server { get; set; }

    public required string App { get; set; } 

    public string? SourceContext { get; set; }

    public required string Level { get; set; }

    public required string Message { get; set; }

    public string? Exception { get; set; }

    public string? Account { get; set; }
}
