#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

typedef NS_ENUM(NSInteger, VLSDKStatus) {
    VLSDKStatusInitial = 0,
    VLSDKStatusNotRecognized = 1,
    VLSDKStatusVLPass = 2,
    VLSDKStatusVLFail = 3
};

NS_ASSUME_NONNULL_END
