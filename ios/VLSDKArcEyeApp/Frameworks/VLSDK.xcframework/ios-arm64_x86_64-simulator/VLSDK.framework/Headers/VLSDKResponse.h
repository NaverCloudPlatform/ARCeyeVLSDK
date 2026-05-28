#import <Foundation/Foundation.h>
#import <simd/simd.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKResponse : NSObject

@property(nonatomic, assign, readwrite) bool success;
@property(nonatomic, assign, readwrite) int code;
@property(nonatomic, copy) NSString* message;

@property(nonatomic, assign, readwrite) long timestamp;
@property(nonatomic, copy) NSString* datasetInfo;
@property(nonatomic, assign, readwrite) simd_quatd quaternion;
@property(nonatomic, assign, readwrite) simd_double3 translation;
@property (nonatomic, strong, nullable) NSNumber* latitude;
@property (nonatomic, strong, nullable) NSNumber* longitude;
@property (nonatomic, strong, nullable) NSNumber* bearing;
@property(nonatomic, assign, readwrite) float confidence;

@property(nonatomic, strong) NSString* hint;

@end

NS_ASSUME_NONNULL_END
