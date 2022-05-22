
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Timing;

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

    var x = logger.OperationAt(LogLevel.Debug, LogLevel.Critical, TimeSpan.FromSeconds(1));
    using (var y = x.Begin("Test"))
    {
        y.Abandon();
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
