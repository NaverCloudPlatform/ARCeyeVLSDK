# ChangeLog

## [1.8.0] - 2025-01-21
* 초기 위치 추정 시 여러 개의 VL pose 정보를 사용하는 기능 추가.
* `OnVLPoseRequested(VLRequestEventData)` 이벤트 추가.
* `OnVLPoseResponded(VLResponseEventData)` 이벤트 추가.
* `VLResponseEventData`를 통해 VL 성공, 실패 상태 및 메시지 전달.
* `OnInitialPoseReceived(int)` 이벤트 삭제.
* `VLSDKManager`에 `trackerState` 프로퍼티 추가.

## [1.7.1-preview.2] - 2024-12-30
* inlier, withGlobal 값을 SDK 레벨에서 설정할 수 있도록 구현
* TrackerConfig를 class에서 struct로 변경

## [1.7.1-preview.1] - 2024-12-19
### Changed
* define symbol 수동 해결 버튼 추가
* NativeLogger 사용 시, 별도의 GameObject를 사용하지 않고 VLSDKManager 활성화 시 자동으로 생성되는 방식으로 변경
* iOS 시뮬레이터 더비 빌드 지원