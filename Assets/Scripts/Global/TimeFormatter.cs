using System;

public static class TimeFormatter
{
    /// <summary>
    /// 현재 시각 기준으로 몇 분/시간/일 전인지 한국어 문자열 반환
    /// </summary>
    public static string ToRelativeTime(DateTime time)
    {
        TimeSpan diff = DateTime.UtcNow - time.ToUniversalTime();

        if (diff.TotalSeconds < 60)
            return $"{(int)diff.TotalSeconds}초 전";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}분 전";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}시간 전";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}일 전";

        // 일주일 이상이면 그냥 날짜로 보여줌
        return time.ToLocalTime().ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 정확한 년/월/일 시:분 출력
    /// </summary>
    public static string ToFullDate(DateTime time)
    {
        return time.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}