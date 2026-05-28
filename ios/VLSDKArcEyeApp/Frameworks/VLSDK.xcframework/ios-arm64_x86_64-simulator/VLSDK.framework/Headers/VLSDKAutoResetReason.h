#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

typedef NS_ENUM(NSInteger, VLSDKAutoResetReason) {
    VLSDKAutoResetReasonUnknown = -1,
    VLSDKAutoResetReasonTiltedDevice = 0,
    VLSDKAutoResetReasonLocalizationLoss = 2,
    VLSDKAutoResetReasonMonitoringLoss = 3
};

NS_ASSUME_NONNULL_END
