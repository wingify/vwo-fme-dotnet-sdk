# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.14.1] - 2025-12-11

### Added

- Send SDK Name and SDK Version in the settings call

## [1.14.0] - 2025-12-08

### Added

- Added retry logic for network requests (GET and POST) with configurable exponential backoff via the `RetryConfig` initialization option.
- `RetryConfig` supports:
  - `shouldRetry` (bool): enable/disable automatic retry on failures (default: `true`)
  - `maxRetries` (int): maximum number of retry attempts (default: `3`)
  - `initialDelay` (int): initial delay before the first retry in seconds (default: `2`)
  - `backoffMultiplier` (int): multiplier for exponential backoff between retry attempts (default: `2`)

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var retryConfig = new Dictionary<string, object>
{
    { "shouldRetry", true },   // Enable retries (default: true)
    { "maxRetries", 5 },       // Retry up to 5 times
    { "initialDelay", 3 },     // Wait 3 seconds before first retry
    { "backoffMultiplier", 2 } // Double the delay for each subsequent retry
};

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "YOUR_SDK_KEY",
    AccountId = YOUR_ACCOUNT_ID,
    RetryConfig = retryConfig
};

var vwoInstance = VWO.Init(vwoInitOptions);
```

## [1.13.1] - 2025-11-21

### Changed

- Enhanced queue-based network call processing to use a fixed thread pool of 5 threads (reduced from 20) with proper semaphore-based concurrency control. 

## [1.13.0] - 2025-11-17

### Added

- Introduced queue-based POST request processing backed by a connection pool to improve reliability under high concurrency and prevent data loss.

### Fixed

- Handled settings dese

## [1.12.0] - 2025-11-13

### Added

- Support for `Map` in `setAttribute` method to send multiple attributes data.

## [1.11.0] - 2025-09-02

### Added

- Post-segmentation variables are now automatically included as unregistered attributes, enabling post-segmentation without requiring manual setup.
- Added support for built-in targeting conditions, including browser version, OS version, and IP address, with advanced operator support (greaterThan, lessThan, regex).

## [1.10.0] - 2025-09-02

### Added

- Sends usage statistics to VWO servers automatically during SDK initialization

## [1.9.0] - 2025-08-04

### Added

- Added support for sending a one-time initialization event to the server to verify correct SDK setup.


## [1.8.2] - 2025-07-22

### Fixed 

- Fixed: Bugs in polling intervals causing poor thread management


## [1.8.1] - 2025-05-21

### Added

- Added a feature to track and collect usage statistics related to various SDK features and configurations which can be useful for analytics, and gathering insights into how different features are being utilized by end users.

## [1.8.0] - 2025-05-21

### Added

- Added support for `batchEventData` configuration to optimize network requests by batching multiple events together. This allows you to:

  - Configure `requestTimeInterval` to flush events after a specified time interval
  - Set `eventsPerRequest` to control maximum events per batch
  - Implement `flushCallback` to handle batch processing results
  - Manually trigger event flushing via `flushEvents()` method

  - You can also manually flush events using the `flushEvents()` method:

```c#
   using VWOFmeSdk.Models;
   using VWOFmeSdk.Interfaces.Batching;
    IFlushInterface flushCallback = new FlushCallbackImpl();

    var batchEventData = new BatchEventData
    {
        EventsPerRequest = 100,      // Send up to 100 events per
        RequestTimeInterval = 60,   // Flush events every 60 seconds
        FlushCallback = flushCallback,
    };

    var vwoInitOptions = new VWOInitOptions
    {
        SdkKey = "your_sdk_key",
        AccountId = YOUR_ACCOUNT_ID,
        BatchEventData = batchEventData
    };

    var vwoInstance = VWO.Init(vwoInitOptions);


```

- You can also manually flush events using the `flushEvents()` method:

```c#
  vwoInstance.flushEvents();
```

## [1.7.0] - 2025-03-27

### Added

- Added identifiable library to import uuid v5 methods instead of implementing in code

## [1.6.0] - 2025-03-12

### Added

- Added support for sending error logs to VWO server for better debugging.

## [1.5.0] - 2024-03-12

### Added

- Added the support for using salt for bucketing if provided in the rule configuration.

## [1.4.0] - 2024-03-04

### Added

- added new method `updateSettings` to update settings on the vwo client instance.

## [1.3.0] - 2024-11-11

### Fixed

- Fixed: Resolved an issue with logging module to ensure accurate and consistent logs output within the SDK.

## [1.2.0] - 2024-09-27

### Added

- Feat: added support for Personalise rules within `Mutually Exclusive Groups`.

## [1.1.0] - 2024-08-14

### Added

- First release of VWO Feature Management and Experimentation capabilities

  ```c#
  using VWOFmeSdk;
  using VWOFmeSdk.Models.User;

  var vwoInitOptions = new VWOInitOptions
  {
      SdkKey = "YOUR_SDK_KEY",
      AccountId = YOUR_ACCOUNT_ID,
  };

  // Initialize VWO SDK
  var vwoInstance = VWO.Init(vwoInitOptions);

  // Create VWOContext object
  var context = new VWOContext
  {
      Id = "user-id",
  };

  // Get the GetFlag object for the feature key and context
  var getFlag = vwoInstance.GetFlag("feature-key", context);

  // Get the flag value
  var isEnabled = getFlag.IsEnabled();

  // Get the variable value for the given variable key and default value
  var variableValue = getFlag.GetVariable("variable-key", "default-value");

  // Track the event for the given event name and context
  var trackResponse = vwoInstance.TrackEvent("event-name", context, eventProperties);

  // Send attribute data
  vwoInstance.SetAttribute("attribute-key", "attribute-value" , context);
  ```