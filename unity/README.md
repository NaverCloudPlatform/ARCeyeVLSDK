# ARC eye VLSDK for Unity

![](https://img.shields.io/badge/Unity-2021.3+-blue.svg?style=flat&logo=unity) ![](https://img.shields.io/badge/Unity-2022.3+-blue.svg?style=flat&logo=unity) ![](https://img.shields.io/badge/Unity-6000.0+-blue.svg?style=flat&logo=unity)

## Overview

ARC eye VLSDK for Unity is a Unity package that estimates the 6-DoF pose of a mobile device from a single camera image, using the ARC eye Visual Localization (VL) API. Use it in AR apps that need accurate device pose where GPS is unreliable, such as indoor navigation.

## Installation

### Installation via UPM (Recommended)

1. Open Package Manager by clicking **Window > Package Manager** in the Unity top menu.
1. Click the + button in the upper left corner of the Package Manager window, then click **Add package from git URL** and enter the following address:

```
https://github.com/NaverCloudPlatform/ARCeyeVLSDK.git?path=unity/Assets/VLSDK
```

### Direct Addition to Project

If you are not using UPM, import the package directly:

1. Download `vl-sdk-x.x.x.unitypackage` from the repository.
1. Click **Assets > Import Packages > Custom Package…** in the Unity top menu, then find and add **vl-sdk-x.x.x.unitypackage** to the project.

## Usage

See the [official documentation](https://ar.naverlabs.com/docs/vlsdk) for detailed usage instructions.

## License

See [LICENSE](./LICENSE) for the full open source license.
