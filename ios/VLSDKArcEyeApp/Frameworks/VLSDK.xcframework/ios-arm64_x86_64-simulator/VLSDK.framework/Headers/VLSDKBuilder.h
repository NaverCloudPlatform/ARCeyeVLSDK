#import <Foundation/Foundation.h>
#import <VLSDK/VLSDKRequest.h>
#import <VLSDK/VLSDKResponse.h>
#import <VLSDK/VLSDKStatus.h>
#import <VLSDK/VLSDKConfig.h>
#import <VLSDK/VLSDKLogLevel.h>
#import <VLSDK/VLSDKFrame.h>
#import <VLSDK/VLSDKFps.h>
#import <VLSDK/VLSDKAutoResetReason.h>
#import <VLSDK/VLSDKRequestPauseReason.h>
#import <VLSDK/VLSDKService.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKBuilder : NSObject

- (instancetype)init NS_UNAVAILABLE;
- (instancetype)initWithServices: (NSArray<VLSDKService*>*) services;

- (VLSDKBuilder*)logLevel:(VLSDKLogLevel)value;

- (VLSDKBuilder*)viewport:(CGSize)viewport;
- (VLSDKBuilder*)useDecoder:(bool)value;
- (VLSDKBuilder*)targetFps:(VLSDKFps)value;
- (VLSDKBuilder*)useRaycast:(bool)value;

- (VLSDKBuilder*)requestIntervalBeforeLocalization:(int)value;
- (VLSDKBuilder*)requestIntervalAfterLocalization:(int)value;
- (VLSDKBuilder *)dropResetActive:(bool)value;
- (VLSDKBuilder *)datasetInfoPrior:(VLSDKDatasetInfo*)value;

- (VLSDKBuilder*)onUpdateFrame:(void(^)(VLSDKFrame* frame))block;
- (VLSDKBuilder*)onUpdateStatus:(void(^)(VLSDKStatus status))block;
- (VLSDKBuilder*)onUpdateDatasetInfo:(void(^)(NSString* datasetInfo))block;
- (VLSDKBuilder*)onResumeRequest:(void (^)())block;
- (VLSDKBuilder*)onPauseRequest:(void (^)(VLSDKRequestPauseReason reason))block;
- (VLSDKBuilder*)onInvokeAutoReset:(void(^)(VLSDKAutoResetReason reason))block;
- (VLSDKBuilder*)onUpdateTargetFps:(void(^)(int fps))block;

- (VLSDKConfig*)build;

@end

NS_ASSUME_NONNULL_END
