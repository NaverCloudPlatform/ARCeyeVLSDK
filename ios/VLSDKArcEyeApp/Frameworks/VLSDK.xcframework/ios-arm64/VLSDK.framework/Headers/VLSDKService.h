#import <Foundation/Foundation.h>
#import <VLSDK/VLSDKReferencePoint.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKService : NSObject

- (instancetype)init NS_UNAVAILABLE;
- (instancetype)initWithLocation: (NSString*) location
                       invokeUrl: (NSString*) invokeUrl
                       secretKey: (NSString*) secretKey;

@property (nonatomic, copy) NSString* location;
@property (nonatomic, copy) NSString* invokeUrl;
@property (nonatomic, copy) NSString* secretKey;

@end

NS_ASSUME_NONNULL_END
