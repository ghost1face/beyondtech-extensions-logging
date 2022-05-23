# BeyondTech Logging Extensions

Extensions for Microsoft's `ILogger` for common use cases.

## BeyondTech.Extensions.Logging.Timing 
[![.NET Actions Status](https://github.com/ghost1face/beyondtech-extensions-logging/workflows/.NET/badge.svg?branch=master)](https://github.com/ghost1face/beyondtech-extensions-logging/actions) [![Coverage Status](https://coveralls.io/repos/github/ghost1face/beyondtech-extensions-logging/badge.svg?branch=master)](https://coveralls.io/github/ghost1face/beyondtech-extensions-logging?branch=master)
[![Nuget](https://img.shields.io/nuget/v/BeyondTech.Extensions.Logging.Timing.svg)](https://www.nuget.org/packages/BeyondTech.Extensions.Logging.Timing)


Timing extensions for logging operations: [here](./src/BeyondTech.Extensions.Logging.Timing/README.md)

This lets you perfom simple logging for timed operations, while simplifying the boilerplate:

```cs
ILogger logger = // assign instance

using (var operation = logger.BeginOperation("Processing large file {FilePath}", filePath))
{
   operation.Complete();
}
```

Yields:

```
info: Processing large file /d/test/image.png completed in 822.5 ms
```

### License

Licensed under [Apache 2.0](LICENSE.md)