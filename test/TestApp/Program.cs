
using System;
using BeyondTech.Extensions.Logging.Timing;
using Microsoft.Extensions.Logging;

var factory = LoggerFactory.Create(builder =>
{
    builder
        .AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
        });
});

var logger = factory.CreateLogger<Program>();

try
{
    logger.LogInformation("Hello, world!");

    //using (var operation = logger.TimeOperation("Timed operation"))
    //using (logger.BeginScope(new Dictionary<string, object>
    //{
    //    { "Huh", "Wut" }
    //}))
    //{
    //    logger.LogError("Logged error in scope");

    //    //operation.Complete("Blah", 1);
    //}

    var leveledOperation = logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromSeconds(1));
    using (var timedOperation = leveledOperation.Begin("Test"))
    {
        // long operation/code here

        timedOperation.Abandon();
    }

    return 0;
}
catch (Exception ex)
{
    return -1;
}
finally
{

}
