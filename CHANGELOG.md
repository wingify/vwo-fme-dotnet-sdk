# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.65.0] - 2026-09-10

### Added

- Enhanced `vwo_sdkUsageStats` event payload with `settingsFetchTime`, `sdkInitTime`, and `initConfig` (the SDK initialization options) under `data` for better observability into SDK initialization. These fields were removed from the `vwo_fmeSdkInit` event payload.

## [1.62.0] - 2026-08-30

### Added

- Implemented events sampling to reduce the volume of internal events going to server

## [1.60.0] - 2026-07-01

### Added

- Support for **Web Testing pre-segmentation**: campaign segmentation can use the `campaignVariation` operand. The SDK evaluates it against **`context.PlatformVariables["webTestingCampaigns"]`**, a map of Web Testing campaign ID → variation ID (plain object or JSON string). Supported operand values in settings: `122` (user in campaign), `122_2` (exact variation), `122_!1` (in campaign but not variation 1), `!122` (not in campaign).

  ```csharp
  using WingifyFmeSdk;
  using WingifyFmeSdk.Models.User;

  var options = new WingifyInitOptions
  {
      AccountId = 123456,
      SdkKey = "32-alpha-numeric-sdk-key"
  };

  var client = Wingify.Init(options);

  // Correctly passing webTestingCampaigns
  var webTestingCampaigns = new Dictionary<string, object>
  {
      { "2", 23 } // Assigned to campaign 2, variation 23
  };

  var platformVariables = new Dictionary<string, object>
  {
      { "webTestingCampaigns", webTestingCampaigns }
  };

  var context = new WingifyContext
  {
      Id = "user-123",
      PlatformVariables = platformVariables
  };

  var flag = client.GetFlag("feature-key", context);
  ```

## [1.55.1] - 2026-06-29

### Fixed

- `setAttribute` now accepts only `string`, `int`, and `bool` values; `float` and `double` are rejected
- Event requests now correctly route through `ProxyUrl` (previously only settings fetch was proxied)
- Polling no longer triggers a settings update when settings are unchanged (fixed incorrect equality check)
- Settings fetch URL no longer includes `collectionPrefix`, fixing polling for EU/regional accounts
- `DataTypeUtil.IsObject` no longer misclassifies primitives as `Object` in error logs

## [1.55.0] - 2026-06-16

### Added

- Added support for tracking usage. When tracking is enabled (`isMAU` flag in settings), the SDK will automatically trigger a `vwo_feTrackUsage` event whenever a feature flag is evaluated via the `GetFlag` API, provided a primary `vwo_variationShown` impression call is not otherwise dispatched.
## [1.50.0] - 2026-05-29

This release introduces **Wingify** as the primary SDK branding and package namespace, while keeping existing **VWO** integrations fully supported.

### Added

- **Wingify public API** — use `Wingify`, `WingifyInitOptions`, and `WingifyContext` from the `WingifyFmeSdk` namespace as the recommended entry point for new integrations.

  ```csharp
  using WingifyFmeSdk;
  using WingifyFmeSdk.Models.User;

  var options = new WingifyInitOptions
  {
      AccountId = 123456,
      SdkKey = "32-alpha-numeric-sdk-key"
  };

  var client = Wingify.Init(options);

  var context = new WingifyContext
  {
      Id = "user-123"
  };

  var flag = client.GetFlag("feature-key", context);
  ```

### Changed

- The SDK implementation now lives under the `WingifyFmeSdk` namespace.
- Log messages and documentation have been updated to reflect Wingify branding.
- **No breaking changes for existing integrations** — server event names, payload keys, and runtime behavior remain compatible with the VWO platform.

### Deprecated

The following **VWO** classes in `VWOFmeSdk` are deprecated but **continue to work without modification**:

| Deprecated (still supported) | Use instead |
|---|---|
| `VWOFmeSdk.VWO` | `WingifyFmeSdk.Wingify` |
| `VWOFmeSdk.Models.User.VWOInitOptions` | `WingifyFmeSdk.Models.User.WingifyInitOptions` |
| `VWOFmeSdk.Models.User.VWOContext` | `WingifyFmeSdk.Models.User.WingifyContext` |
| `VWOFmeSdk.Interfaces.Logger.LogTransport` | `WingifyFmeSdk.Interfaces.Logger.LogTransport` |
| `VWOFmeSdk.Packages.Logger.Enums.LogLevelEnum` | `WingifyFmeSdk.Packages.Logger.Enums.LogLevelEnum` |
| `VWOFmeSdk.Interfaces.Integration.IntegrationCallback` | `WingifyFmeSdk.Interfaces.Integration.IntegrationCallback` |
| `VWOFmeSdk.Packages.Storage.Connector` | `WingifyFmeSdk.Packages.Storage.Connector` |

Existing code does not need to change immediately. We recommend adopting the Wingify API for new projects and migrating when convenient:

```csharp
// Still supported — no action required today
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var options = new VWOInitOptions
{
    AccountId = 123456,
    SdkKey = "32-alpha-numeric-sdk-key"
};

var client = VWO.Init(options);

var context = new VWOContext
{
    Id = "user-123"
};

client.GetFlag("feature-key", context);
```

**Migration tip:** Replace `VWO` → `Wingify`, `VWOInitOptions` → `WingifyInitOptions`, and `VWOContext` → `WingifyContext`, and update `using` statements from `VWOFmeSdk` to `WingifyFmeSdk`. Method signatures and SDK behavior are unchanged.


## [1.23.0] - 2026-04-29

### Added

- Add support for user aliasing (works when Gateway is configured)

  ```c#
  using VWOFmeSdk;
  using VWOFmeSdk.Models.User;

  var vwoInitOptions = new VWOInitOptions
  {
      SdkKey = "32-alpha-numeric-sdk-key",
      AccountId = YOUR_ACCOUNT_ID,
      GatewayService = new Dictionary<string, object>
      {
          { "url", "http://your-custom-gateway-url" }
      },
      IsAliasingEnabled = true,
  };

  var vwoClient = VWO.Init(vwoInitOptions);

  // Using context
  var context = new VWOContext { Id = "user-id" };
  var successFromContext = vwoClient.SetAlias(context, "aliasId");

  // Alternatively, pass userId directly
  var successFromUserId = vwoClient.SetAlias("user-id", "aliasId");
  ```

## [1.22.0] - 2026-04-28

### Added

- Added support for holdout groups to exclude users from features based on segmentation and traffic allocation.

## [1.21.0] - 2026-03-18

### Added

- Added support for custom bucketing seed on the `VWOContext` so that bucketing can be driven by a caller-provided seed instead of the raw `userId`, with automatic fallback to `userId` when the seed is not set.

```csharp

  using VWOFmeSdk;
  using VWOFmeSdk.Models.User;

  var vwoInitOptions = new VWOInitOptions
  {
      SdkKey = "YOUR_SDK_KEY",
      AccountId = YOUR_ACCOUNT_ID,
  };

  // Initialize VWO SDK
  var vwoInstance = VWO.Init(vwoInitOptions);

  // Use custom bucketing seed so different userIds can share bucketing
  var context = new VWOContext
  {
      Id = "user-id",
      BucketingSeed = "custom_seed"
  };

  var getFlag = vwoInstance.GetFlag("feature-key", context);
  // If bucketingSeed is invalid (non-string, empty or whitespace-only),
  // the SDK logs INVALID_PARAM for bucketingSeed and falls back to userId.
```

## [1.20.0] - 2026-02-26

### Added

- Added support to add multiple `transports` in logger.

```csharp
using VWOFmeSdk.Interfaces.Logger;
using VWOFmeSdk.Packages.Logger.Enums;

public class CustomLogTransport : LogTransport
{
    public void Log(LogLevelEnum level, string message)
    {
        // your custom logging logic here
    }
}

var logger = new Dictionary<string, object>
{
    { "level", "DEBUG" },
    { "transports", new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "level", "DEBUG" }, // optional: per-transport level filter, defaults to global level
                { "log", new CustomLogTransport() }
            }
        }
    }
};

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "YOUR_SDK_KEY",
    AccountId = YOUR_ACCOUNT_ID,
    Logger = logger
};

var vwoInstance = VWO.Init(vwoInitOptions);
```



## [1.19.0] - 2026-02-11

### Added

- Added `IsBatchingDisabled` parameter in `VWOInitOptions` to allow opting out of the default event batching mechanism.
- Optimized event batching by enabling it by default with the following configuration:
  - `EventsPerRequest`: 100
  - `RequestTimeInterval`: 3 seconds

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "YOUR_SDK_KEY",
    AccountId = YOUR_ACCOUNT_ID,
    IsBatchingDisabled = true,  //false by default
};

var vwoInstance = VWO.Init(vwoInitOptions);
```


## [1.18.0] - 2026-01-29

### Added

- Added `MaxConcurrentThreads` initialization option to control the maximum number of concurrent network threads used by the SDK, with safe clamping based on the machine's logical processors.
- Introduced a bounded channel–backed request queue, with capacity configurable via `MaxRequestQueueCapacity` on `VWOInitOptions`.
- Enhanced graceful shutdown to drain the request queue and wait for all in-flight network calls.

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "YOUR_SDK_KEY",
    AccountId = YOUR_ACCOUNT_ID,

    // Controls how many worker tasks can process queued requests concurrently
    MaxConcurrentThreads = 10, // defaults to Environment.ProcessorCount - 1

    // Controls how many requests can be buffered in-memory before dropping oldest
    MaxRequestQueueCapacity = 20000 // defaults to 10,000
};

var vwoInstance = VWO.Init(vwoInitOptions);
```

## [1.17.0] - 2026-02-05

### Added

- Added support for redirecting all network calls through a custom proxy URL. This feature allows users to route all SDK network requests (settings, tracking, etc.) through their own proxy server.

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    ProxyUrl = "http://custom.proxy.com",
};

var vwoInstance = VWO.Init(vwoInitOptions);
```

## [1.16.0] - 2025-01-21

### Added

- Added new `Shutdown()` API method to gracefully stop SDK background activities:
  - Stops polling
  - If batching is enabled, flushes all pending events synchronously and clears timers
  - Includes error handling for safe shutdown operations

```csharp
var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "YOUR_SDK_KEY",
    AccountId = YOUR_ACCOUNT_ID
};

var vwoInstance = VWO.Init(vwoInitOptions);

// Gracefully shut down the SDK
vwoInstance.Shutdown();
```

## [1.15.0] - 2025-01-08

### Added

- Enhanced Logging capabilities at VWO by sending `vwo_sdkDebug` event with additional debug properties.

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