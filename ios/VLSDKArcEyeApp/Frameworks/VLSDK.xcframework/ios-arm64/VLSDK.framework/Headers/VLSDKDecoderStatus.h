#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

typedef NS_ENUM(NSUInteger, VLSDKDecoderStatus) {
    VLSDKDecoderStatusUndefined = 0,
    VLSDKDecoderStatusSuccess = 1,
    VLSDKDecoderStatusFileOpenError = 2
};
NS_ASSUME_NONNULL_END
