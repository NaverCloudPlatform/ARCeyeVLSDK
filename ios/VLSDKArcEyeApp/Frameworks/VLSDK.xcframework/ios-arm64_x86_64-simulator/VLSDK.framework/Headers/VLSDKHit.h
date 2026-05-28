#import <Foundation/Foundation.h>
#import <CoreVideo/CoreVideo.h>
#import <simd/simd.h>
#import <VLSDK/VLSDKHitLabel.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKHit : NSObject

@property (nonatomic, assign) simd_float3 position;
@property(nonatomic, assign, readwrite) VLSDKHitLabel label;

@end

NS_ASSUME_NONNULL_END
