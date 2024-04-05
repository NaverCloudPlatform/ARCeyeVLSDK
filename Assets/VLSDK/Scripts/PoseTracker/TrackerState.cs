namespace ARCeye {
[System.Serializable]
public enum TrackerState {
    /// <summary>
    /// 모든 세션이 초기화 된 상태. VL 초기화가 긴 시간동안 계속 실패한 경우.
    /// </summary>
    INITIAL, 

    /// <summary>
    /// VL 성공 응답을 수신한 상태. 아직 localizedPose가 계산되지 않음.
    /// </summary>
    VL_RECEIVED, 

    /// <summary>
    /// VL 초기화가 한 번이라도 성공한 경우.
    /// </summary>
    VL_PASS, 

    /// <summary>
    /// 40번 연속 VL 요청에 실패하는 경우.
    /// </summary>
    VL_FAIL, 

    /// <summary>
    /// 서비스 범위 밖일 경우. 
    /// </summary>
    VL_OUT_OF_SERVICE
}
}