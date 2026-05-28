#import <Foundation/Foundation.h>
#import <CoreVideo/CoreVideo.h>
#import <simd/simd.h>

#import <VLSDK/VLSDKDecoderStatus.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKDecoderFrame : NSObject

@property (nonatomic, assign) NSTimeInterval timestamp;
@property (nonatomic, assign) simd_float4x4 viewMatrix;
@property (nonatomic, assign) simd_float4x4 projMatrix;
@property (nonatomic, assign) simd_float3x3 textureTransform;
@property CVPixelBufferRef capturedImage;
@property (nonatomic, assign) simd_float3x3 intrinsics;
@property (nonatomic, assign) double gpsLatitude;
@property (nonatomic, assign) double gpsLongitude;
@property (nonatomic, assign) double gpsAltitude;
@property (nonatomic, assign) double relAltitude;

@end

@interface VLSDKDecoder : NSObject

+ (instancetype)new NS_UNAVAILABLE;
- (instancetype)init NS_UNAVAILABLE;

- (void) importDataset: (NSURL*) url completion:(void(^)(VLSDKDecoderStatus)) callback;
- (NSString*) getVersion;

- (void) seek:(float)position;
- (bool) playing;

- (void) setOnProgress: (void(^)(float)) callback;

@end

NS_ASSUME_NONNULL_END
