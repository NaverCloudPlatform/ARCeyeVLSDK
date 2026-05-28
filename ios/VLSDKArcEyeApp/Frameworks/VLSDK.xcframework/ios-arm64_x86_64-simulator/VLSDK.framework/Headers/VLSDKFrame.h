#import <Foundation/Foundation.h>
#import <CoreVideo/CoreVideo.h>
#import <simd/simd.h>
#import <UIKit/UIKit.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKFrame : NSObject

@property (nonatomic, assign) long timestamp;
@property (nonatomic, assign) simd_double4x4 viewMatrix;
@property (nonatomic, assign) simd_float4x4 projMatrix;
@property (nonatomic, assign) simd_float3x3 textureTransform;
@property CVPixelBufferRef capturedImage;

@property (nonatomic, strong, nullable) NSNumber* bearing;
@property (nonatomic, strong, nullable) NSNumber* latitude;
@property (nonatomic, strong, nullable) NSNumber* longitude;

@property (nonatomic, assign) double relativeAltitude;

@end

NS_ASSUME_NONNULL_END
