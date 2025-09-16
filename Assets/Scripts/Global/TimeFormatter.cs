using System;
using UnityEngine; // Debug.Log 용 (원하면 제거 가능)

public static class TimeFormatter
{
    /// <summary>
    /// created_at 같은 ISO8601 문자열을 받아서 "몇 분 전/몇 시간 전" 형태로 반환
    /// </summary>
    public static string ToRelativeTime(string isoTimeString)
    {
        if (string.IsNullOrEmpty(isoTimeString))
            return "";

        DateTime time;
        if (!DateTime.TryParse(isoTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out time))
        {
            Debug.LogWarning($"[TimeFormatter] 문자열 파싱 실패: {isoTimeString}");
            return isoTimeString; // 원래 문자열 그대로 리턴
        }

        TimeSpan diff = DateTime.UtcNow - time.ToUniversalTime();

        if (diff.TotalSeconds < 60)
            return $"{(int)diff.TotalSeconds}초 전";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}분 전";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}시간 전";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}일 전";

        // 일주일 이상이면 날짜로 출력
        return time.ToLocalTime().ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// ISO8601 문자열을 받아서 "yyyy-MM-dd HH:mm" 포맷으로 출력
    /// </summary>
    public static string ToFullDate(string isoTimeString)
    {
        if (string.IsNullOrEmpty(isoTimeString))
            return "";

        DateTime time;
        if (!DateTime.TryParse(isoTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind, out time))
        {
            Debug.LogWarning($"[TimeFormatter] 문자열 파싱 실패: {isoTimeString}");
            return isoTimeString;
        }

        return time.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}