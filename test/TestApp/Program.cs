using BeyondTech.Extensions.Logging.Timing;
using Microsoft.Extensions.Logging;

var factory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
        });
});

var logger = factory.CreateLogger<Program>();

try
{
    using (logger.TimeOperation("Full operation"))
    {
        logger.LogInformation("Hello, world!");

        using (var op = logger.BeginOperation("Processing data {Data}", 123))
        {
            await System.Threading.Tasks.Task.Delay(400);
            op.Complete("Data processed with status code: {StatusCode}", 200);
        }
    }

    return 0;
}
catch
{
    return -1;
}
finally
{

}
