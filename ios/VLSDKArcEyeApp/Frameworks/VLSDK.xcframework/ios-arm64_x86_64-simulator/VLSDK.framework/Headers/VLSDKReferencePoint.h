#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKReferencePoint : NSObject

- (instancetype)init NS_UNAVAILABLE;
- (instancetype)initWithLatitude: (double) latitude longitude: (double) longitude;

@property (nonatomic, assign) double latitude;
@property (nonatomic, assign) double longitude;

@end

NS_ASSUME_NONNULL_END
