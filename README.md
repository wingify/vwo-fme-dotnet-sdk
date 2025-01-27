# VWO Feature Management and Experimentation SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/VWO.FME.Sdk.svg?style=plastic)](https://www.nuget.org/packages/VWO.FME.Sdk/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](http://www.apache.org/licenses/LICENSE-2.0)

## Overview

The **VWO Feature Management and Experimentation SDK** (VWO FME Dotnet SDK) enables dotnet developers to integrate feature flagging and experimentation into their applications. This SDK provides full control over feature rollout, A/B testing, and event tracking, allowing teams to manage features dynamically and gain insights into user behavior.

---

## Requirements

- .NET Standard 2.0 and higher
- Compatible with .NET Core, .NET Framework, and .NET 5+

---

## Installation

Install the SDK using the .NET CLI or NuGet Package Manager:

### Using .NET CLI
```bash
> dotnet add package VWO.FME.Sdk
```

### Using Package Manager
```bash
PM> Install-Package VWO.FME.Sdk
```

---

## Basic Usage Example

The following example demonstrates initializing the SDK, creating a user context, checking if a feature flag is enabled, and tracking a custom event:

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

---

## Advanced Configuration Options

To customize the SDK further, additional parameters can be passed to the `init()` API. Here's a table describing each option:

| **Parameter**          | **Description**                                                                                                     | **Required** | **Type**        | **Example**                     |
|------------------------|---------------------------------------------------------------------------------------------------------------------|--------------|-----------------|---------------------------------|
| `SdkKey`               | SDK key for authenticating your application with VWO.                                                              | Yes          | `string`        | `"32-alpha-numeric-sdk-key"`   |
| `AccountId`            | VWO Account ID for authentication.                                                                                 | Yes          | `int`           | `123456`                        |
| `PollInterval`         | Time interval (in milliseconds) for fetching updates from VWO servers.                                              | No           | `int`           | `60000`                         |
| `Storage`              | Custom storage mechanism for persisting user decisions and campaign data.                                           | No           | `IStorage`      | See [Storage](#storage) section |
| `Logger`               | Configure log levels and transport for debugging purposes.                                                          | No           | `ILogger`       | See [Logger](#logger) section   |
| `Integrations`         | Callback function for integrating with third-party analytics services.                                              | No           | `Action`        | See [Integrations](#integrations) section |

Refer to the [official VWO documentation](https://developers.vwo.com/v2/docs/fme-dotnet-install) for additional parameter details.

---

## User Context

The `VWOContext` object uniquely identifies users and supports targeting and segmentation. It includes parameters like user ID, custom variables, user agent, and IP address.

### Parameters Table
| **Parameter**         | **Description**                                                              | **Required** | **Type**             |
|-----------------------|------------------------------------------------------------------------------|--------------|----------------------|
| `Id`                  | Unique identifier for the user.                                              | Yes          | `string`             |
| `CustomVariables`     | Custom attributes for targeting.                                             | No           | `Dictionary<string, object>` |
| `UserAgent`           | User agent string for identifying the user's browser and operating system.   | No           | `string`             |
| `IpAddress`           | IP address of the user.                                                      | No           | `string`             |

### Example
```csharp
var context = new VWOContext
{
    Id = "unique_user_id",
    CustomVariables = new Dictionary<string, object> { { "age", 25 }, { "location", "US" } },
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
    IpAddress = "1.1.1.1"
};

```

---

## Basic Feature Flagging

Feature Flags serve as the foundation for all testing, personalization, and rollout rules within FME.
To implement a feature flag, first use the `getFlag` API to retrieve the flag configuration.
The `getFlag` API provides a simple way to check if a feature is enabled for a specific user and access its variables. It returns a feature flag object that contains methods for checking the feature's status and retrieving any associated variables.

| Parameter    | Description                                                      | Required | Type   | Example              |
| ------------ | ---------------------------------------------------------------- | -------- | ------ | -------------------- |
| `featureKey` | Unique identifier of the feature flag                            | Yes      | String | `'new_checkout'`     |
| `context`    | Object containing user identification and contextual information | Yes      | Object | `{ id: 'user_123' }` |


### Example
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

---

## Custom Event Tracking

Feature flags can be enhanced with connected metrics to track key performance indicators (KPIs) for your features. These metrics help measure the effectiveness of your testing rules by comparing control versus variation performance, and evaluate the impact of personalization and rollout campaigns. Use the trackEvent API to track custom events like conversions, user interactions, and other important metrics:

| Parameter         | Description                                                            | Required | Type   | Example                |
| ----------------- | ---------------------------------------------------------------------- | -------- | ------ | ---------------------- |
| `eventName`       | Name of the event you want to track                                    | Yes      | String | `'purchase_completed'` |
| `context`         | Object containing user identification and other contextual information | Yes      | Object | `{ id: 'user_123' }`   |
| `eventProperties` | Additional properties/metadata associated with the event               | No       | Object | `{ amount: 49.99 }`    |


### Example
```csharp
var eventProperties = new Dictionary<string, object> { { "revenue", 100 } };
var trackResponse = vwoInstance.TrackEvent("event_name", context, eventProperties);
Console.WriteLine("Event tracked: " + trackResponse);
```

See [Tracking Conversions](https://developers.vwo.com/v2/docs/fme-dotnet-metrics#usage) documentation for more information.

### Pushing Attributes

User attributes provide rich contextual information about users, enabling powerful personalization. The `setAttribute` method provides a simple way to associate these attributes with users in VWO for advanced segmentation. Here's what you need to know about the method parameters:

| Parameter        | Description                                                            | Required | Type   | Example                 |
| ---------------- | ---------------------------------------------------------------------- | -------- | ------ | ----------------------- |
| `attributeKey`   | The unique identifier/name of the attribute you want to set            | Yes      | String | `'plan_type'`           |
| `attributeValue` | The value to be assigned to the attribute                              | Yes      | Any    | `'premium'`, `25`, etc. |
| `context`        | Object containing user identification and other contextual information | Yes      | Object | `{ id: 'user_123' }`    |

Example usage:
```csharp
vwoInstance.SetAttribute("attribute_key", "attribute_value", context);

```

See [Pushing Attributes](https://developers.vwo.com/v2/docs/fme-dotnet-attributes#usage) documentation for additional information.

---

### Polling Interval Adjustment

The `pollInterval` is an optional parameter that allows the SDK to automatically fetch and update settings from the VWO server at specified intervals. Setting this parameter ensures your application always uses the latest configuration.

```csharp
var vwoClient = VWO.Init(new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    PollInterval = 60000 // Fetch updates every 60 seconds
});
```

### Gateway

The VWO FME Gateway Service is an optional but powerful component that enhances VWO's Feature Management and Experimentation (FME) SDKs. It acts as a critical intermediary for pre-segmentation capabilities based on user location and user agent (UA). By deploying this service within your infrastructure, you benefit from minimal latency and strengthened security for all FME operations.

#### Why Use a Gateway?

The Gateway Service is required in the following scenarios:

- When using pre-segmentation features based on user location or user agent.
- For applications requiring advanced targeting capabilities.
- It's mandatory when using any thin-client SDK (e.g., Go).

#### How to Use the Gateway

The gateway can be customized by passing the `gatewayService` parameter in the `init` configuration.

```csharp

var vwoInitOptions = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = logger,
    GatewayService = new Dictionary<string, object> { { "url", "https://custom.gateway.com" } },
};
```

Refer to the [Gateway Documentation](https://developers.vwo.com/v2/docs/gateway-service) for further details.

### Storage

The SDK operates in a stateless mode by default, meaning each `getFlag` call triggers a fresh evaluation of the flag against the current user context.

To optimize performance and maintain consistency, you can implement a custom storage mechanism by passing a `storage` parameter during initialization. This allows you to persist feature flag decisions in your preferred database system (like Redis, MongoDB, or any other data store).

Key benefits of implementing storage:

- Improved performance by caching decisions
- Consistent user experience across sessions
- Reduced load on your application

The storage mechanism ensures that once a decision is made for a user, it remains consistent even if campaign settings are modified in the VWO Application. This is particularly useful for maintaining a stable user experience during A/B tests and feature rollouts.

### Example

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

---

### Logger

VWO by default logs all `ERROR` level messages to your server console.
To gain more control over VWO's logging behaviour, you can use the `logger` parameter in the `init` configuration.

| **Parameter** | **Description**                        | **Required** | **Type** | **Example**           |
| ------------- | -------------------------------------- | ------------ | -------- | --------------------- |
| `level`       | Log level to control verbosity of logs | Yes          | String   | `DEBUG`               |
| `prefix`      | Custom prefix for log messages         | No           | String   | `'CUSTOM LOG PREFIX'` |
| `transport`   | Custom logger implementation           | No           | Object   | See example below     |


#### Example 1: Set log level to control verbosity of logs

```csharp
var vwoInitOptions1 = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Logger
    {
        Level = "DEBUG"
    }
};
var vwoClient1 = VWO.Init(vwoInitOptions1);
```

#### Example 2: Add custom prefix to log messages for easier identification

```csharp
var vwoInitOptions2 = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Logger
    {
        Level = "DEBUG",
        Prefix = "CUSTOM LOG PREFIX"
    }
};
var vwoClient2 = VWO.Init(vwoInitOptions2);
```

#### Example 3: Implement custom transport to handle logs your way

The `transport` parameter allows you to implement custom logging behavior by providing your own logging functions. You can define handlers for different log levels (`debug`, `info`, `warn`, `error`, `trace`) to process log messages according to your needs.

For example, you could:

- Send logs to a third-party logging service
- Write logs to a file
- Format log messages differently
- Filter or transform log messages
- Route different log levels to different destinations

The transport object should implement handlers for the log levels you want to customize. Each handler receives the log message as a parameter.

```csharp

var vwoInitOptions3 = new VWOInitOptions
{
    SdkKey = "32-alpha-numeric-sdk-key",
    AccountId = 123456,
    Logger = new Logger
    {
        Level = "DEBUG",
        Transports = new List<LogTransport>
        {
            new LogTransport
            {
                Level = "DEBUG",
                LogHandler = (msg, level) => Console.WriteLine($"DEBUG: {msg}")
            },
            new LogTransport
            {
                Level = "INFO",
                LogHandler = (msg, level) => Console.WriteLine($"INFO: {msg}")
            },
            new LogTransport
            {
                Level = "ERROR",
                LogHandler = (msg, level) => Console.WriteLine($"ERROR: {msg}")
            }
        }
    }
};
var vwoClient3 = VWO.Init(vwoInitOptions3);

```
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

Copyright 2024-2025 Wingify Software Pvt. Ltd.
