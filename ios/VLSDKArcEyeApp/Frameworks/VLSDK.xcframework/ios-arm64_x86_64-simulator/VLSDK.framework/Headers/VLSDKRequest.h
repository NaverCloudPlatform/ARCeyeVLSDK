#import <Foundation/Foundation.h>
#import <simd/simd.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKRequest : NSObject

@property(nonatomic, assign, readwrite) long timestamp;
@property (nonatomic, strong) NSMutableData* queryImage;
@property(nonatomic, strong) NSString* hint;

@end

NS_ASSUME_NONNULL_END
