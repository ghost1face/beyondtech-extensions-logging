# Timing Extensions

This library is a direct port of @nblumhardt 's [Serilog Timing](https://github.com/nblumhardt/serilog-timings) for usage directly with Microsoft's `ILogger`.  This library provides ***almost*** all the same benefits of the Serilog Timing, but expands the support for any implementation of `ILogger`.

### Installation

```
dotnet add package BeyondTech.Extensions.Logging.Timing
```

### Getting started

First, an `ILogger` must be available.  Whether through a `LoggerFactory`, dependency injection, or registration of a logging provider.  The extension methods are available in the `BeyondTech.Extensions.Logging.Timing` namespace.

```cs
using BeyondTech.Extensions.Logging.Timing;
```

```cs
using (logger.TimeOperation("Processing data for action: {ActionId}", action.Id))
{
    // Operation to time here
}
```

At the completion of the `using` block, a message will be written to the log like:

```
info: Processing data for action: 87654 completed in 109.4 ms
```

The operation description passed to `TimeOperation()` is a message template; the event written to the log
extends it with `" {Outcome} in {Elapsed} ms"`.

 * All messages logged have an `Elapsed` property in milliseconds
 * `Outcome` will always be `"completed"` when the `TimeOperation()` method is used

Operations that can either _succeed or fail_, or _that produce a result_, can be created with
`BeginOperation()`:

```csharp
using (var op = logger.BeginOperation("Processing data for action: {ActionId}", action.Id))
{
	// Operation to time here

	op.Complete();
}
```

Using `op.Complete()` will produce the same kind of result as in the first example:

```
info: Processing data for action: 4506 completed in 22.1 ms
```

Additional methods on `Operation` allow more detailed results to be captured:

```csharp
op.Complete("{AffectedRows} rows affected", rowCount);
```

This will create a logging scope around the outcome message with the log scope of "2 rows affected".  For this to work effectively, be sure to enable logging scopes on your `ILogger` configuration.

If the operation is not completed by calling `Complete()`, it is assumed to have failed and a
warning-level event will be written to the log instead:

```
warn: Processing data for action: 87654 abandoned in 109.4 ms
```

In this case the `Outcome` property will be `"abandoned"`.

To suppress this message, for example when an operation turns out to be inapplicable, use
`op.Cancel()`. Once `Cancel()` has been called, no event will be written by the operation on
either completion or abandonment.

### Leveling

Timings are most useful in production, so timing events are recorded at the `Information` level and
higher, which should generally be collected all the time.

If you truly need `Verbose`- or `Debug`-level timings, you can trigger them with the `OperationAt()` extension method on `ILogger`:

```csharp
using (logger.OperationAt(LogEventLevel.Debug).TimeOperation("Preparing zip archive"))
{
    // ...
```

When a level is specified, both completion and abandonment events will use it. To configure a different
abandonment level, pass the second optional parameter to the `OperationAt()` method.

### Caveats

One important usage note: because the event is not written until the completion of the `using` block
(or call to `Complete()`), arguments to `BeginOperation()` or `TimeOperation()` are not captured until then; don't
pass parameters to these methods that mutate during the operation.





















