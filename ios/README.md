# ARC eye VLSDK for iOS

## Overview

ARC eye VLSDK for iOS is a native iOS library that estimates the position of mobile devices in space using the ARC eye Visual Localization (VL) API. This enables easy implementation of AR apps that utilize spatial information.

## Quick Start

1. Open `iOS/VLSDKArcEyeApp.xcodeproj` (or the workspace) in Xcode.
2. In the source, locate the `VLSDKService` initialization and replace the placeholders with your issued credentials:
   - `location` — your location name
   - `invokeUrl` — your VL API URL
   - `secretKey` — your VL API key
3. Select your signing team, then build and run on a device.

## Usage

See the [official documentation](./docs/README.md) for detailed usage instructions.

## License

See [LICENSE](./LICENSE) for the full open source license.
