# 프로젝트 설정

프로젝트 설정 단계에서 필요한 절차입니다.

## AndroidManifest.xml 설정

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:tools="http://schemas.android.com/tools">
...
<uses-permission  android:name="android.permission.CAMERA"/>
<uses-feature android:name="android.hardware.camera.ar" android:required="true"/>
<uses-feature android:name="android.hardware.camera.autofocus" android:required="false"/>
<uses-feature android:glEsVersion="0x00030000" android:required="true" />

<application>
...
<activity
    android:name=".MainActivity"
    android:screenOrientation="portrait">
    ...
</activity>
<meta-data android:name="com.google.ar.core" android:value="required" />
</application>
```
* AR 카메라 및 GLES3 관련 요소 추가
* AR 렌더링 관련 액티비티 방향 `portrait`로 고정
    * `portrait` 방향으로만 AR 렌더링 지원
