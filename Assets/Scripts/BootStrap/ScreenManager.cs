using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    void Awake()
    {
        // 앱이 실행되는 동안 화면이 절대 꺼지지 않도록 설정합니다.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}