#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <VLSDK/VLSDKRequest.h>
#import <VLSDK/VLSDKResponse.h>
#import <VLSDK/VLSDKStatus.h>
#import <VLSDK/VLSDKLogLevel.h>
#import <VLSDK/VLSDKFrame.h>
#import <VLSDK/VLSDKFps.h>
#import <VLSDK/VLSDKAutoResetReason.h>
#import <VLSDK/VLSDKRequestPauseReason.h>
#import <VLSDK/VLSDKService.h>
#import <VLSDK/VLSDKDatasetInfo.h>

NS_ASSUME_NONNULL_BEGIN

@interface VLSDKConfig : NSObject

- (instancetype)init NS_UNAVAILABLE;
- (instancetype)initWithServices: (NSArray<VLSDKService*>*) services;

@property (nonatomic, strong, readonly) NSArray<VLSDKService*> *services;

@property(nonatomic, assign, readwrite) VLSDKLogLevel logLevel;
@property(nonatomic, assign, readwrite) CGSize viewport;
@property(nonatomic, assign, readwrite) bool useDecoder;
@property(nonatomic, assign, readwrite) VLSDKFps targetFps;
@property(nonatomic, assign, readwrite) bool useRaycast;
@property(nonatomic, assign, readwrite) long requestIntervalBeforeLocalized;
@property(nonatomic, assign, readwrite) long requestIntervalAfterLocalized;

@property(nonatomic, assign, readwrite) bool dropResetActive;
@property (nonatomic, strong, readwrite) VLSDKDatasetInfo *datasetInfoPrior;

@property (nonatomic, copy, readwrite, nullable) void (^onUpdateFrame)(VLSDKFrame* frame);
@property (nonatomic, copy, readwrite, nullable) void (^onUpdateStatus)(VLSDKStatus status);
@property (nonatomic, copy, readwrite, nullable) void (^onUpdateDatasetInfo)(NSString* datasetInfo);
@property (nonatomic, copy, readwrite, nullable) void (^onResumeRequest)();
@property (nonatomic, copy, readwrite, nullable) void (^onPauseRequest)(VLSDKRequestPauseReason reason);
@property (nonatomic, copy, readwrite, nullable) void (^onInvokeAutoReset)(VLSDKAutoResetReason reason);
@property (nonatomic, copy, readwrite, nullable) void (^onUpdateTargetFps)(int fps);

@end

NS_ASSUME_NONNULL_END
