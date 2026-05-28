#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

typedef NS_ENUM(NSInteger, VLSDKRequestPauseReason) {
    VLSDKRequestPauseReasonUnknown = 0,
    VLSDKRequestPauseReasonSingularity = 1,
    VLSDKRequestPauseReasonTiltedDevice = 2
};

NS_ASSUME_NONNULL_END
