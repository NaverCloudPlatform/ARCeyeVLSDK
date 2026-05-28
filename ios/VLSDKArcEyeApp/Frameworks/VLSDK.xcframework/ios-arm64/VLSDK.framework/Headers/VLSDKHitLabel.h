#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

typedef NS_ENUM(NSInteger, VLSDKHitLabel) {
    VLSDKHitLabelNone = 0,
    VLSDKHitLabelWall,
    VLSDKHitLabelFloor,
    VLSDKHitLabelCeiling,
    VLSDKHitLabelTable,
    VLSDKHitLabelSeat,
    VLSDKHitLabelWindow,
    VLSDKHitLabelDoor
};

NS_ASSUME_NONNULL_END

