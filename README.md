# VWO Feature Management and Experimentation SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/VWO.FME.Sdk.svg?style=plastic)](https://www.nuget.org/packages/VWO.FME.Sdk/)

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](http://www.apache.org/licenses/LICENSE-2.0)


## Requirements

- Works with NetStandard: 2.0 onwards.

## Installation

```bash
PM> Install-Package VWO.FME.Sdk
```

## Basic usage

**Using and Instantiation**

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
    var variableValue = getFlag.GetVariable("stringVar", "default-value")
    
    // Track the event for the given event name and context
    var trackResponse = vwoInstance.TrackEvent("event-name", context);
    
    // send attributes data
    vwoInstance.SetAttribute("key", "value" , context);
```

## Setting Up development environment

```bash
chmod +x start-dev.sh;
bash start-dev.sh;
```

It will install the git-hooks necessary for commiting and pushing the code.

## Running Unit Tests

```bash
dotnet test
```

## Authors

- Main Contributor - [Saksham Gupta](https://github.com/sakshamg1304)

## Changelog

Refer [CHANGELOG.md](https://github.com/wingify/vwo-fme-dotnet-sdk/blob/master/CHANGELOG.md)

## Contributing

Please go through our [contributing guidelines](https://github.com/wingify/vwo-fme-dotnet-sdk/CONTRIBUTING.md)

## Code of Conduct

[Code of Conduct](https://github.com/wingify/vwo-fme-dotnet-sdk/blob/master/CODE_OF_CONDUCT.md)

## License

[Apache License, Version 2.0](https://github.com/wingify/vwo-fme-dotnet-sdk/blob/master/LICENSE)

Copyright 2024 Wingify Software Pvt. Ltd.
