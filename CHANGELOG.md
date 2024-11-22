# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


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
