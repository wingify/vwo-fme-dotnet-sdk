# VWO Feature Management and Experimentation SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/VWO.FME.Sdk.svg?style=plastic)](https://www.nuget.org/packages/VWO.FME.Sdk/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](http://www.apache.org/licenses/LICENSE-2.0)

## Overview

The **VWO Feature Management and Experimentation SDK** (VWO FME Dotnet SDK) enables dotnet developers to integrate feature flagging and experimentation into their applications. This SDK provides full control over feature rollout, A/B testing, and event tracking, allowing teams to manage features dynamically and gain insights into user behavior.

---

## Requirements

- **.NET Standard 2.0** or higher
- Compatible with the following **.NET** implementations:
  - **.NET Core 3.1+** (LTS, supported)
  - **.NET Framework 4.6.1+**
  - **.NET 5+** (End-of-life, not recommended for new projects)
  - **.NET 6+** (LTS, recommended for new projects)
  - **.NET 7+** (latest stable version)

---

## Installation

Install the SDK using the .NET CLI or NuGet Package Manager:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

### Using .NET CLI
```bash
> dotnet add package VWO.FME.Sdk
```

### Using Package Manager
```bash
PM> Install-Package VWO.FME.Sdk
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

### Using .NET CLI
```bash
> dotnet add package Wingify.FME.Sdk
```

### Using Package Manager
```bash
PM> Install-Package Wingify.FME.Sdk
```

</details>

---

## Basic Usage Example

The following example demonstrates initializing the SDK, creating a user context, checking if a feature flag is enabled, and tracking a custom event:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

class Program
{
    static void Main(string[] args)
    {
        // Initialize VWO SDK with your account details
        var vwoInitOptions = new VWOInitOptions
        {
            SdkKey = "32-alpha-numeric-sdk-key", // Replace with your SDK key
            AccountId = 123456 // Replace with your account ID
        };

        var vwoInstance = VWO.Init(vwoInitOptions);

        // Create user context
        var context = new VWOContext
        {
            Id = "unique_user_id" // Set a unique user identifier
        };

        // Check if a feature flag is enabled
        var getFlag = vwoInstance.GetFlag("feature_key", context);
        bool isFeatureEnabled = getFlag.IsEnabled();
        Console.WriteLine($"Is feature enabled? {isFeatureEnabled}");

        // Get a variable value with a default fallback
        var variableValue = getFlag.GetVariable("feature_variable", "default_value");
        Console.WriteLine($"Variable value: {variableValue}");

        // Track a custom event
        var eventProperties = new Dictionary<string, object> { { "revenue", 100 } };
        var trackResponse = vwoInstance.TrackEvent("event_name", context, eventProperties);
        Console.WriteLine("Event tracked: " + trackResponse);

        // Set a custom attribute
        vwoInstance.SetAttribute("attribute_key", "attribute_value", context);
    }
}
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
using WingifyFmeSdk;
using WingifyFmeSdk.Models.User;

class Program
{
    static void Main(string[] args)
    {
        // Initialize Wingify SDK with your account details
        var wingifyInitOptions = new WingifyInitOptions
        {
            SdkKey = "32-alpha-numeric-sdk-key", // Replace with your SDK key
            AccountId = 123456 // Replace with your account ID
        };

        var wingifyInstance = Wingify.Init(wingifyInitOptions);

        // Create user context
        var context = new WingifyContext
        {
            Id = "unique_user_id" // Set a unique user identifier
        };

        // Check if a feature flag is enabled
        var getFlag = wingifyInstance.GetFlag("feature_key", context);
        bool isFeatureEnabled = getFlag.IsEnabled();
        Console.WriteLine($"Is feature enabled? {isFeatureEnabled}");

        // Get a variable value with a default fallback
        var variableValue = getFlag.GetVariable("feature_variable", "default_value");
        Console.WriteLine($"Variable value: {variableValue}");

        // Track a custom event
        var eventProperties = new Dictionary<string, object> { { "revenue", 100 } };
        var trackResponse = wingifyInstance.TrackEvent("event_name", context, eventProperties);
        Console.WriteLine("Event tracked: " + trackResponse);

        // Set a custom attribute
        wingifyInstance.SetAttribute("attribute_key", "attribute_value", context);
    }
}
```

</details>

---

## Advanced Configuration Options

To customize the SDK further, additional parameters can be passed to the `Init()` API using the `VWOInitOptions` object (or `WingifyInitOptions` for SDK version 1.50.0 and later). Here's a table describing each option:

| **Parameter**          | **Description**                                                                                                     | **Required** | **Type**        | **Example**                     |
|------------------------|---------------------------------------------------------------------------------------------------------------------|--------------|-----------------|---------------------------------|
| `SdkKey`               | SDK key for authenticating your application with VWO.                                                              | Yes          | `string`        | `"32-alpha-numeric-sdk-key"`   |
| `AccountId`            | VWO Account ID for authentication.                                                                                 | Yes          | `int`           | `123456`                        |
| `PollInterval`         | Time interval (in milliseconds) for fetching updates from VWO servers.                                              | No           | `int`           | `60000`                         |
| `Storage`              | Custom storage mechanism for persisting user decisions and campaign data.                                           | No           | `IStorage`      | See [Storage](#storage) section |
| `Logger`               | Configure log levels and transport for debugging purposes.                                                          | No           | `Dictionary<string, object>` | See [Logger](#logger) section   |
| `Integrations`          | Callback function for integrating with third-party analytics services.                                             | No           | `IntegrationCallback` | See [Integrations](#integrations) section |
| `MaxConcurrentThreads`  | Maximum number of concurrent network worker threads used for processing queued requests.                      | No           | `int?`          | See [Network concurrency and queue configuration](#network-concurrency-and-queue-configuration)                             |
| `MaxRequestQueueCapacity` | Maximum number of queued requests buffered in memory before oldest events are dropped.                    | No           | `int?`          | See [Network concurrency and queue configuration](#network-concurrency-and-queue-configuration) |
| `ProxyUrl`         | Custom proxy URL for redirecting all SDK network requests (settings, tracking, etc.) through your own proxy server | No           | `string`        | See [Proxy](#proxy) section |

Refer to the [official VWO documentation](https://developers.vwo.com/v2/docs/fme-dotnet-install) for additional parameter details.

---

## User Context

The `VWOContext` object (or `WingifyContext` for SDK version 1.50.0 and later) uniquely identifies users and supports targeting and segmentation. It includes parameters like user ID, custom variables, user agent, and IP address.

### Parameters Table
| **Parameter**         | **Description**                                                              | **Required** | **Type**             |
|-----------------------|------------------------------------------------------------------------------|--------------|----------------------|
| `Id`                  | Unique identifier for the user.                                              | Yes          | `string`             |
| `CustomVariables`     | Custom attributes for targeting.                                             | No           | `Dictionary<string, object>` |
| `UserAgent`           | User agent string for identifying the user's browser and operating system.   | No           | `string`             |
| `IpAddress`           | IP address of the user.                                                      | No           | `string`             |
| `PlatformVariables`   | Platform specific variables like web testing campaigns.                      | No           | `Dictionary<string, object>` |

### Example

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
var context = new VWOContext
{
    Id = "unique_user_id",
    CustomVariables = new Dictionary<string, object> { { "age", 25 }, { "location", "US" } },
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
    IpAddress = "1.1.1.1",
    PlatformVariables = new Dictionary<string, object>
    {
        // Reference example only.
        // In production, fetch campaign assignments using script run in frontend and pass that object to backend as webTestingCampaigns.
        { "webTestingCampaigns", "{\"122\":\"1\",\"130\":\"2\"}" }
    }
};

```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
var context = new WingifyContext
{
    Id = "unique_user_id",
    CustomVariables = new Dictionary<string, object> { { "age", 25 }, { "location", "US" } },
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
    IpAddress = "1.1.1.1",
    PlatformVariables = new Dictionary<string, object>
    {
        // Reference example only.
        // In production, fetch campaign assignments using script run in frontend and pass that object to backend as webTestingCampaigns.
        { "webTestingCampaigns", "{\"122\":\"1\",\"130\":\"2\"}" }
    }
};

```

</details>

## Web testing pre-segmentation

Server-side flag decisions can align with **Web Testing** (browser) experiments. **Campaign assignments are typically read on the frontend** (for example, VWO cookies) and sent to your server; pass them in `PlatformVariables["webTestingCampaigns"]` as a map of **campaign ID -> variation ID** (strings), or as a **JSON string** of that object.

Pre-segment rules in the VWO dashboard can use the **`campaignVariation`** operator:

| Operand pattern | Meaning |
| --------------- | ------- |
| `C` | User is in campaign `C` (any variation). |
| `!C` | User is **not** in campaign `C`. |
| `C_V` | User is in campaign `C` with variation `V`. |
| `C_!V` | User is in campaign `C` and assigned variation is **not** `V`. |

---

## Basic Feature Flagging

Feature Flags serve as the foundation for all testing, personalization, and rollout rules within FME.
To implement a feature flag, first use the `getFlag` API to retrieve the flag configuration.
The `getFlag` API provides a simple way to check if a feature is enabled for a specific user and access its variables. It returns a feature flag object that contains methods for checking the feature's status and retrieving any associated variables.

| Parameter    | Description                                                      | Required | Type   | Example              |
| ------------ | ---------------------------------------------------------------- | -------- | ------ | -------------------- |
| `featureKey` | Unique identifier of the feature flag                            | Yes      | String | `'new_checkout'`     |
| `context`    | Object containing user identification and contextual information | Yes      | VWOContext / WingifyContext | `{ id: 'user_123' }` |


### Example

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
var getFlag = vwoInstance.GetFlag("feature_key", context);

if (getFlag.IsEnabled())
{
    Console.WriteLine("Feature is enabled!");

    // Get and use feature variable
    var variableValue = getFlag.GetVariable("feature_variable", "default_value");
    Console.WriteLine("Variable value: " + variableValue);
}
else
{
    Console.WriteLine("Feature is not enabled!");
}
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
var getFlag = wingifyInstance.GetFlag("feature_key", context);

if (getFlag.IsEnabled())
{
    Console.WriteLine("Feature is enabled!");

    // Get and use feature variable
    var variableValue = getFlag.GetVariable("feature_variable", "default_value");
    Console.WriteLine("Variable value: " + variableValue);
}
else
{
    Console.WriteLine("Feature is not enabled!");
}
```

</details>

---

## Custom Event Tracking

Feature flags can be enhanced with connected metrics to track key performance indicators (KPIs) for your features. These metrics help measure the effectiveness of your testing rules by comparing control versus variation performance, and evaluate the impact of personalization and rollout campaigns. Use the trackEvent API to track custom events like conversions, user interactions, and other important metrics:

| Parameter         | Description                                                            | Required | Type   | Example                |
| ----------------- | ---------------------------------------------------------------------- | -------- | ------ | ---------------------- |
| `eventName`       | Name of the event you want to track                                    | Yes      | String | `'purchase_completed'` |
| `context`         | Object containing user identification and other contextual information | Yes      | Object | `{ id: 'user_123' }`   |
| `eventProperties` | Additional properties/metadata associated with the event               | No       | Object | `{ amount: 49.99 }`    |


### Example

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
var eventProperties = new Dictionary<string, object> { { "revenue", 100 } };
var trackResponse = vwoInstance.TrackEvent("event_name", context, eventProperties);
Console.WriteLine("Event tracked: " + trackResponse);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
var eventProperties = new Dictionary<string, object> { { "revenue", 100 } };
var trackResponse = wingifyInstance.TrackEvent("event_name", context, eventProperties);
Console.WriteLine("Event tracked: " + trackResponse);
```

</details>

See [Tracking Conversions](https://developers.vwo.com/v2/docs/fme-dotnet-metrics#usage) documentation for more information.

### Pushing Attributes

User attributes provide rich contextual information about users, enabling powerful personalization. The `setAttribute` method provides a simple way to associate these attributes with users in VWO for advanced segmentation. Here's what you need to know about the method parameters:

| Parameter        | Description                                                            | Required | Type   | Example                 |
| ---------------- | ---------------------------------------------------------------------- | -------- | ------ | ----------------------- |
| `attributeKey`   | The unique identifier/name of the attribute you want to set            | Yes      | String | `'plan_type'`           |
| `attributeValue` | The value to be assigned to the attribute                              | Yes      | Any    | `'premium'`, `25`, etc. |
| `context`        | Object containing user identification and other contextual information | Yes      | Object | `{ id: 'user_123' }`    |

Example usage:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
vwoInstance.SetAttribute("attribute_key", "attribute_value", context);

```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
wingifyInstance.SetAttribute("attribute_key", "attribute_value", context);

```

</details>

See [Pushing Attributes](https://developers.vwo.com/v2/docs/fme-dotnet-attributes#usage) documentation for additional information.

---

### Polling Interval Adjustment

The `pollInterval` is an optional parameter that allows the SDK to automatically fetch and update settings from the VWO server at specified intervals. Setting this parameter ensures your application always uses the latest configuration.

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
var vwoClient = VWO.Init(new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    PollInterval = 60000 // Fetch updates every 60 seconds
});
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
var wingifyClient = Wingify.Init(new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    PollInterval = 60000 // Fetch updates every 60 seconds
});
```

</details>

### Gateway

The VWO FME Gateway Service is an optional but powerful component that enhances VWO's Feature Management and Experimentation (FME) SDKs. It acts as a critical intermediary for pre-segmentation capabilities based on user location and user agent (UA). By deploying this service within your infrastructure, you benefit from minimal latency and strengthened security for all FME operations.

#### Why Use a Gateway?

The Gateway Service is required in the following scenarios:

- When using pre-segmentation features based on user location or user agent.
- For applications requiring advanced targeting capabilities.
- It's mandatory when using any thin-client SDK (e.g., Go).

#### How to Use the Gateway

The gateway can be customized by passing the `gatewayService` parameter in the `init` configuration.

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = logger,
    GatewayService = new Dictionary<string, object> { { "url", "https://custom.gateway.com" } },
};
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp

var wingifyInitOptions = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = logger,
    GatewayService = new Dictionary<string, object> { { "url", "https://custom.gateway.com" } },
};
```

</details>

Refer to the [Gateway Documentation](https://developers.vwo.com/v2/docs/gateway-service) for further details.

### Proxy

The `ProxyUrl` parameter allows you to redirect all SDK network calls through a custom proxy URL. This feature enables you to route all SDK network requests (settings, tracking, etc.) through your own proxy server, providing better control over network traffic and security.

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    ProxyUrl = "http://custom.proxy.com"
};

var vwoInstance = VWO.Init(vwoInitOptions);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
using WingifyFmeSdk;
using WingifyFmeSdk.Models.User;

var wingifyInitOptions = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    ProxyUrl = "http://custom.proxy.com"
};

var wingifyInstance = Wingify.Init(wingifyInitOptions);
```

</details>

### Retry Config

The `RetryConfig` parameter allows you to customize the retry behavior for network requests. This is particularly useful for applications that need to handle network failures gracefully with an exponential backoff strategy.

| **Parameter**       | **Description**                                           | **Required** | **Type** | **Default** | **Validation**                      |
| ------------------- | --------------------------------------------------------- | ------------ | -------- | ----------- | ----------------------------------- |
| `shouldRetry`       | Whether to enable automatic retry on network failures     | No           | `bool`   | `true`      | Must be a boolean value             |
| `maxRetries`        | Maximum number of retry attempts before giving up         | No           | `int`    | `3`         | Must be a non-negative integer >= 1 |
| `initialDelay`      | Initial delay (in seconds) before the first retry attempt | No           | `int`    | `2`         | Must be a non-negative integer >= 1 |
| `backoffMultiplier` | Multiplier for exponential backoff between retry attempts | No           | `int`    | `2`         | Must be a non-negative integer >= 2 |

#### How Retry Logic Works

The SDK implements an exponential backoff strategy for retrying failed network requests:

1. **Initial Request**: The SDK attempts the initial network request.
2. **On Failure**: If the request fails and `shouldRetry` is `true`, the SDK waits for `initialDelay` seconds.
3. **Exponential Backoff**: For subsequent retries, the delay is calculated as: `initialDelay × (backoffMultiplier ^ attempt)`.
4. **Maximum Attempts**: The SDK will retry up to `maxRetries` times before giving up.

#### Example Usage

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

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
    SdkKey = "32-alpha-numeric-sdk-key", // Replace with your SDK key
    AccountId = 123456,                  // Replace with your account ID
    RetryConfig = retryConfig
};

var vwoClient = VWO.Init(vwoInitOptions);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
using WingifyFmeSdk;
using WingifyFmeSdk.Models.User;

var retryConfig = new Dictionary<string, object>
{
    { "shouldRetry", true },   // Enable retries (default: true)
    { "maxRetries", 5 },       // Retry up to 5 times
    { "initialDelay", 3 },     // Wait 3 seconds before first retry
    { "backoffMultiplier", 2 } // Double the delay for each subsequent retry
};

var wingifyInitOptions = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key", // Replace with your SDK key
    AccountId = 123456,                  // Replace with your account ID
    RetryConfig = retryConfig
};

var wingifyClient = Wingify.Init(wingifyInitOptions);
```

</details>

### User Aliasing

User aliasing lets you associate an existing user ID with an alternate ID (alias) so future evaluations and tracking use a unified identity across systems.

Requirements:

- Gateway must be configured
- Aliasing must be enabled during initialization: `IsAliasingEnabled = true`

Initialization example:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var vwoInitOptions = new VWOInitOptions
{
    AccountId = 123456,
    SdkKey = "32-alpha-numeric-sdk-key",
    IsAliasingEnabled = true,
    GatewayService = new Dictionary<string, object> { { "url", "https://custom.gateway.com" } }
};

var vwoClient = VWO.Init(vwoInitOptions);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
using WingifyFmeSdk;
using WingifyFmeSdk.Models.User;

var wingifyInitOptions = new WingifyInitOptions
{
    AccountId = 123456,
    SdkKey = "32-alpha-numeric-sdk-key",
    IsAliasingEnabled = true,
    GatewayService = new Dictionary<string, object> { { "url", "https://custom.gateway.com" } }
};

var wingifyClient = Wingify.Init(wingifyInitOptions);
```

</details>

Usage examples:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
// Using VWOContext
var context = new VWOContext { Id = "user-123" };
bool success1 = vwoClient.SetAlias(context, "alias-abc");

// Using direct userId
bool success2 = vwoClient.SetAlias("user-123", "alias-abc");
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
// Using WingifyContext
var context = new WingifyContext { Id = "user-123" };
bool success1 = wingifyClient.SetAlias(context, "alias-abc");

// Using direct userId
bool success2 = wingifyClient.SetAlias("user-123", "alias-abc");
```

</details>

Behavior and validations:

- Returns true on success, false otherwise
- Requires aliasing to be enabled and gateway configured
- aliasId must be a non-empty string; it is trimmed before use
- When passing context, `context.Id` must be a non-empty string; it is trimmed before use
- userId and aliasId cannot be the same

### Storage

The SDK operates in a stateless mode by default, meaning each `getFlag` call triggers a fresh evaluation of the flag against the current user context.

To optimize performance and maintain consistency, you can implement a custom storage mechanism by passing a `storage` parameter during initialization. This allows you to persist feature flag decisions in your preferred database system (like Redis, MongoDB, or any other data store).

Key benefits of implementing storage:

- Improved performance by caching decisions
- Consistent user experience across sessions
- Reduced load on your application

The storage mechanism ensures that once a decision is made for a user, it remains consistent even if campaign settings are modified in the VWO Application. This is particularly useful for maintaining a stable user experience during A/B tests and feature rollouts.

### Example

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
using System;
using System.Collections.Generic;
using VWOFmeSdk.Packages.Storage;

public class StorageConnector : Connector
{
    public override object Get(string featureKey, string userId)
    {
        // Retrieve data based on featureKey and userId
        return null;
    }

    public override void Set(Dictionary<string, object> data)
    {
        // Store data based on data["featureKey"] and data["userId"]
    }
}

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Storage = new StorageConnector()
};

```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
using System;
using System.Collections.Generic;
using WingifyFmeSdk.Packages.Storage;

public class StorageConnector : Connector
{
    public override object Get(string featureKey, string userId)
    {
        // Retrieve data based on featureKey and userId
        return null;
    }

    public override void Set(Dictionary<string, object> data)
    {
        // Store data based on data["featureKey"] and data["userId"]
    }
}

var wingifyInitOptions = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Storage = new StorageConnector()
};

```

</details>

---

### Logger

VWO by default logs all `ERROR` level messages to your server console.
To gain more control over VWO's logging behaviour, you can use the `logger` parameter in the `init` configuration.

The `Logger` property accepts a `Dictionary<string, object>` with the following keys:

| **Parameter** | **Description**                        | **Required** | **Type** | **Example**           |
| ------------- | -------------------------------------- | ------------ | -------- | --------------------- |
| `level`       | Log level to control verbosity of logs | Yes          | `string`    | `"DEBUG"`             |
| `prefix`      | Custom prefix for log messages  | No           | `string`    | `"CUSTOM LOG PREFIX"` |
| `transports`   | Custom logger transport implementations            | No           | `List<Dictionary<string, object>>`     | See example below     |

Each transport in the `transports` list is a `Dictionary<string, object>` that supports these keys:

| **Key**             | **Description**                                                              | **Required** | **Type**        |
| ------------------- | ---------------------------------------------------------------------------- | ------------ | --------------- |
| `level`             | Per-transport log level filter (overrides global level)                      | No           | `string`        |
| `log`               | A `LogTransport` implementation that receives **raw** (unformatted) messages | No           | `LogTransport`  |
| `defaultTransport`  | A `LogTransport` implementation that receives **formatted** messages         | No           | `LogTransport`  |

Each transport dictionary should contain either `log` or `defaultTransport` (not both). Use `log` when you want full control over formatting, or `defaultTransport` to receive pre-formatted messages with timestamp, level, and prefix.

#### Example 1: Set log level to control verbosity of logs

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
var vwoInitOptions1 = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Dictionary<string, object> { { "level", "DEBUG" } }
};
var vwoClient1 = VWO.Init(vwoInitOptions1);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
var wingifyInitOptions1 = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Dictionary<string, object> { { "level", "DEBUG" } }
};
var wingifyClient1 = Wingify.Init(wingifyInitOptions1);
```

</details>

#### Example 2: Add custom prefix to log messages for easier identification

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
var vwoInitOptions2 = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Dictionary<string, object>
    {
        { "level", "DEBUG" },
        { "prefix", "CUSTOM LOG PREFIX" }
    }
};
var vwoClient2 = VWO.Init(vwoInitOptions2);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
var wingifyInitOptions2 = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Dictionary<string, object>
    {
        { "level", "DEBUG" },
        { "prefix", "CUSTOM LOG PREFIX" }
    }
};
var wingifyClient2 = Wingify.Init(wingifyInitOptions2);
```

</details>

#### Example 3: Implement custom transports to handle logs your way

The `transports` key allows you to implement custom logging behavior by providing your own `LogTransport` implementations. Each transport can have its own log level filter, so you can route different severity levels to different destinations.

For example, you could:

- Send logs to a third-party logging service
- Write logs to a file
- Format log messages differently
- Filter or transform log messages
- Route different log levels to different destinations

First, create classes that implement the `LogTransport` interface:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
using VWOFmeSdk.Interfaces.Logger;
using WingifyFmeSdk.Packages.Logger.Enums;

public class ErrorLogTransport : LogTransport
{
    public void Log(LogLevelEnum level, string message)
    {
        Console.Error.WriteLine("Error: " + message);
    }
}

public class InfoLogTransport : LogTransport
{
    public void Log(LogLevelEnum level, string message)
    {
        Console.WriteLine("Info: " + message);
    }
}

public class DebugLogTransport : LogTransport
{
    public void Log(LogLevelEnum level, string message)
    {
        Console.WriteLine("Debug: " + message);
    }
}
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
using WingifyFmeSdk.Interfaces.Logger;
using WingifyFmeSdk.Packages.Logger.Enums;

public class ErrorLogTransport : LogTransport
{
    public void Log(LogLevelEnum level, string message)
    {
        Console.Error.WriteLine("Error: " + message);
    }
}

public class InfoLogTransport : LogTransport
{
    public void Log(LogLevelEnum level, string message)
    {
        Console.WriteLine("Info: " + message);
    }
}

public class DebugLogTransport : LogTransport
{
    public void Log(LogLevelEnum level, string message)
    {
        Console.WriteLine("Debug: " + message);
    }
}
```

</details>

Then pass them as transports with per-level filtering:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
var vwoInitOptions3 = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Dictionary<string, object>
    {
        { "level", "DEBUG" },
        { "transports", new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "level", "ERROR" },
                    { "log", new ErrorLogTransport() }
                },
                new Dictionary<string, object>
                {
                    { "level", "INFO" },
                    { "log", new InfoLogTransport() }
                },
                new Dictionary<string, object>
                {
                    { "level", "DEBUG" },
                    { "log", new DebugLogTransport() }
                }
            }
        }
    }
};
var vwoClient3 = VWO.Init(vwoInitOptions3);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
var wingifyInitOptions3 = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Dictionary<string, object>
    {
        { "level", "DEBUG" },
        { "transports", new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "level", "ERROR" },
                    { "log", new ErrorLogTransport() }
                },
                new Dictionary<string, object>
                {
                    { "level", "INFO" },
                    { "log", new InfoLogTransport() }
                },
                new Dictionary<string, object>
                {
                    { "level", "DEBUG" },
                    { "log", new DebugLogTransport() }
                }
            }
        }
    }
};
var wingifyClient3 = Wingify.Init(wingifyInitOptions3);
```

</details>
---

### Network concurrency and queue configuration

You can control how many concurrent network threads are used and how many requests are buffered in the internal queue using `MaxConcurrentThreads` and `MaxRequestQueueCapacity`:

If SDK version less than 1.50.0, use the VWO snippet. If SDK version 1.50.0 or later, we recommend switching to the Wingify snippet

<details>
<summary>VWO (SDK version &lt; 1.50.0)</summary>

```csharp
using VWOFmeSdk;
using VWOFmeSdk.Models.User;

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,

    // Controls how many worker tasks can process queued requests concurrently
    MaxConcurrentThreads = 10, // defaults to Environment.ProcessorCount - 1

    // Controls how many requests can be buffered in-memory before dropping oldest
    MaxRequestQueueCapacity = 20000 // defaults to 10,000
};

var vwoInstance = VWO.Init(vwoInitOptions);
```

</details>

<details>
<summary>Wingify (SDK version &gt;= 1.50.0) — recommended</summary>

```csharp
using WingifyFmeSdk;
using WingifyFmeSdk.Models.User;

var wingifyInitOptions = new WingifyInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,

    // Controls how many worker tasks can process queued requests concurrently
    MaxConcurrentThreads = 10, // defaults to Environment.ProcessorCount - 1

    // Controls how many requests can be buffered in-memory before dropping oldest
    MaxRequestQueueCapacity = 20000 // defaults to 10,000
};

var wingifyInstance = Wingify.Init(wingifyInitOptions);
```

</details>

---

### Version History

The version history tracks changes, improvements, and bug fixes in each version. For a full history, see the [CHANGELOG.md](https://github.com/wingify/vwo-fme-dotnet-sdk/blob/master/CHANGELOG.md).

## Development and Testing

### Install Dependencies and Bootstrap Git Hooks

```bash
dotnet restore
```

### Compile Solution

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

## Contributing

We welcome contributions! Please read our [contributing guidelines](https://github.com/wingify/vwo-fme-dotnet-sdk/CONTRIBUTING.md) before submitting a PR.

---

## License

[Apache License, Version 2.0](https://github.com/wingify/vwo-fme-dotnet-sdk/blob/master/LICENSE)

Copyright 2024-2026 Wingify Software Pvt. Ltd.
