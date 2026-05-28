#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

/**
 @brief Represents a dataset info identifier composed of hierarchical components.

 @discussion Components are joined with underscores internally, and converted to
 comma-separated format when sent as a VL request parameter.

 @code
 // From separate components
 VLSDKDatasetInfo *info = [[VLSDKDatasetInfo alloc] initWithComponents:@[@"1784", @"1f"]];
 VLSDKDatasetInfo *info = [[VLSDKDatasetInfo alloc] initWithComponents:@[@"1784", @"1f", @"device"]];

 // From a comma-separated string (factory)
 VLSDKDatasetInfo *info = [VLSDKDatasetInfo fromString:@"1784,1f,device"];
 @endcode
 */
@interface VLSDKDatasetInfo : NSObject

- (instancetype)init NS_UNAVAILABLE;

/// Initialize with separate hierarchical components (e.g. @c @[@"1784", @"1f", @"device"] ).
/// Components must not contain commas or spaces.
- (instancetype)init:(NSArray<NSString*>*)components;

/// Factory method from a comma-separated string.
+ (instancetype)fromString:(NSString*)string;

@property (nonatomic, copy, readonly) NSString *value;

@end

NS_ASSUME_NONNULL_END
