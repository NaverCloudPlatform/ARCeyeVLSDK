#import <Foundation/Foundation.h>
#import <VLSDK/VLSDKBuilder.h>
#import <VLSDK/VLSDKConfig.h>
#import <VLSDK/VLSDKDecoder.h>
#import <VLSDK/VLSDKFrame.h>
#import <VLSDK/VLSDKFps.h>
#import <VLSDK/VLSDKStatus.h>
#import <VLSDK/VLSDKLogLevel.h>
#import <VLSDK/VLSDKService.h>
#import <VLSDK/VLSDKHit.h>

NS_ASSUME_NONNULL_BEGIN

/*
 +------------------------------------------------------------+
 | VLSDKSession
 +------------------------------------------------------------+
 */

@interface VLSDKSession : NSObject

/**
 @brief Retrieves the shared (singleton) instance of VLSDKSession.
 
 @discussion Since only one session should be active at a time,
 VLSDKSession is provided as a singleton. Use this method whenever
 you need to access the session.
 
 @return The singleton instance of VLSDKSession.
 */
+ (instancetype _Nonnull) shared;
- (instancetype) init NS_UNAVAILABLE;
+ (instancetype) new NS_UNAVAILABLE;

@property (nonatomic, strong, nullable) VLSDKDecoder *decoder;

/**
 @brief Configures the session using the provided VLSDKConfig.
 
 @discussion Use a VLSDKBuilder to create a @c VLSDKConfig object with the
 desired settings and pass it here before starting the session.
 
 @param config A @c VLSDKConfig instance containing all the session settings.
 */
- (void) setupWithConfig:(VLSDKConfig*) config;

/**
 @brief Resumes the session if it has been paused.
 
 @discussion If the session is in a paused state, calling this method
 will reactivate necessary subsystems, such as sensors or network operations,
 so the session can continue functioning.
 */
- (void) resume;

/**
 @brief Resumes the session if it has been paused.
 
 @discussion If the session is in a paused state, calling this method
 will reactivate necessary subsystems, such as sensors or network operations,
 so the session can continue functioning.
 */
- (void) pause;

/**
 @brief Resets the session to its initial state.
 
 @discussion This method clears the internal tracking data and session state,
 effectively starting the session from scratch. Use this if you want to
 manually reinitialize or restart the session flow.
 */
- (void) reset;

/**
 @brief Sets the radius for localized visual localization search.
 
 @discussion When visual localization enters the VLPass state, the session can
 restrict or expand VL search region near the device’s current pose (local search).
 Defines the size, in meters, of a spherical search region centered at the device’s current pose.
 A smaller radius speeds up localization but may fail if the current pose is inaccurate.
 A larger radius increases robustness at the cost of additional computation.
 
 The default search radius is 10 meters.
 
 @param radius Search radius in meters. If not explicitly set, the default value of 10 meters is used.
 */
- (void) setLocalVLSearchRange:(int) radius;

/**
 @brief Sets prior dataset information to filter visual localization responses.

 @discussion When a prior is set, the session restricts VL requests so that
 only responses matching the specified dataset information are returned.
 Underscores in the value are converted to commas before sending the request
 (e.g. @c "1784_1f" becomes @c "1784,1f" ).

 @param prior A @c VLSDKDatasetInfo instance used to filter VL responses.
 */
- (void) setDatasetInfoPrior:(VLSDKDatasetInfo*) prior;

/**
 @brief Enables or disables the automatic reset when the device is tilted downward (pitch).
 
 @discussion If set to @c YES, the session will automatically call @c reset
 whenever the mobile device is tilted below a certain pitch threshold.
 If set to @c NO, the automatic reset feature is disabled, and you must
 manually call @c reset if needed.

 @param active A boolean indicating whether the automatic reset-on-drop feature is active.
 */
- (void) setDropReset:(BOOL) active;

/**
 @brief Updates the viewport size used by the session.

 @discussion
 Call this method whenever the viewport size changes externally so the session
 can reconfigure its internal states, specifically the projMatrix and
 textureTransform within @c VLSDKFrame, according to the new dimensions.
 Note that the device is assumed to be in a portrait orientation.
 
 @param viewport The new size for the rendering viewport.
 */
- (void) setViewport:(CGSize) viewport;

/**
 @brief Performs a raycast from the given normalized screen location.

 @discussion
 Casts a ray from the camera through the specified 2D screen coordinate
 and returns hit information if a tracked object or surface is intersected.

 @param location A normalized point in screen coordinates (0.0 to 1.0 range).
                 Convert from UIKit coordinates: CGPoint(x / viewWidth, y / viewHeight).
 @return A VLSDKHit object if an intersection is found; otherwise nil.
 */
- (nullable VLSDKHit*) raycast:(CGPoint) location;

@end

NS_ASSUME_NONNULL_END
