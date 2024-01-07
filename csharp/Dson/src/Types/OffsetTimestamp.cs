#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Globalization;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.Text;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Types;

/// <summary>
/// 带时区偏移的时间戳
/// </summary>
public readonly struct OffsetTimestamp : IEquatable<OffsetTimestamp>
{
    public const int MaskDate = 1;
    public const int MaskTime = 1 << 1;
    public const int MaskOffset = 1 << 2;
    public const int MaskNanos = 1 << 3;

    public const int MaskDatetime = MaskDate | MaskTime;
    public const int MaskInstant = MaskDate | MaskTime | MaskNanos;
    public const int MaskOffsetDatetime = MaskDate | MaskTime | MaskOffset;

    public readonly long Seconds;
    public readonly int Nanos;
    public readonly int Offset;
    public readonly int Enables;

    /// <summary>
    /// 该接口慎用，通常我们需要精确到毫秒
    /// </summary>
    /// <param name="seconds">纪元时间秒时间戳</param>
    public OffsetTimestamp(long seconds)
        : this(seconds, 0, 0, MaskDatetime) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="seconds">纪元时间秒时间戳</param>
    /// <param name="nanos">时间戳的纳秒部分</param>
    /// <param name="offset">时区偏移量--秒</param>
    /// <param name="enables">启用的字段信息；只有有效的字段才会被保存(序列化)</param>
    public OffsetTimestamp(long seconds, int nanos, int offset, int enables) {
        if (seconds != 0 && DsonInternals.IsAllDisabled(enables, MaskDatetime)) {
            throw new ArgumentException("date and time are disabled");
        }
        if (offset != 0 && DsonInternals.IsAnyDisabled(enables, MaskOffset)) {
            throw new ArgumentException("offset is disabled, but the value is not 0");
        }
        if (nanos != 0 && DsonInternals.IsAnyDisabled(enables, MaskNanos)) {
            throw new ArgumentException("nanos is disabled, but the value is not 0");
        }
        if (nanos > 999_999_999 || nanos < 0) {
            throw new ArgumentException("nanos > 999999999 or < 0");
        }
        this.Seconds = seconds;
        this.Nanos = nanos;
        this.Offset = offset;
        this.Enables = enables;
    }

    /** 考虑到跨平台问题，默认精确到毫秒部分 */
    public static OffsetTimestamp OfDateTime(in DateTime dateTime) {
        long epochMillis = dateTime.ToEpochMillis();
        long seconds = epochMillis / 1000;
        int nanos = (int)(epochMillis % 1000 * DsonInternals.NanosPerMilli);
        if (nanos == 0) {
            return new OffsetTimestamp(seconds, 0, 0, MaskDatetime);
        }
        return new OffsetTimestamp(seconds, nanos, 0, MaskInstant);
    }

    #region props

    public bool HasDate => DsonInternals.IsEnabled(Enables, MaskDate);

    public bool HasTime => DsonInternals.IsEnabled(Enables, MaskTime);

    public bool HasOffset => DsonInternals.IsEnabled(Enables, MaskOffset);

    /** 纳秒部分是否可转为毫秒 -- 纳秒恰为整毫秒时返回true */
    public bool CanConvertNanosToMillis() {
        return (Nanos % 1000_000) == 0;
    }

    /** 将纳秒部分换行为毫秒 */
    public int ConvertNanosToMillis() {
        return Nanos / 1000_000;
    }

    # endregion

    #region equals

    public bool Equals(OffsetTimestamp other) {
        return Seconds == other.Seconds && Nanos == other.Nanos && Offset == other.Offset && Enables == other.Enables;
    }

    public override bool Equals(object? obj) {
        return obj is OffsetTimestamp other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Seconds, Nanos, Offset, Enables);
    }

    public static bool operator ==(OffsetTimestamp left, OffsetTimestamp right) {
        return left.Equals(right);
    }

    public static bool operator !=(OffsetTimestamp left, OffsetTimestamp right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(Seconds)}: {Seconds}, {nameof(Nanos)}: {Nanos}, {nameof(Offset)}: {Offset}, {nameof(Enables)}: {Enables}";
    }

    #region 解析

    public static DateTime ParseDateTime(string datetimeString) {
        return DateTime.ParseExact(datetimeString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static DateOnly ParseDate(string dateString) {
        return DateOnly.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static TimeOnly ParseTime(string timeString) {
        return TimeOnly.ParseExact(timeString, "HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 格式化日期时间为ISO-8601格式
    /// 固定为:<code>yyyy-MM-ddTHH:mm:ss</code>
    /// </summary>
    public static string FormatDateTime(long seconds) {
        return DateTime.UnixEpoch.AddSeconds(seconds).ToString("s");
    }

    /// <summary>
    /// 格式化日期为ISO-8601格式
    /// 固定为:<code>"yyyy-MM-dd"</code>格式
    /// </summary>
    public static string FormatDate(long epochSeconds) {
        DateTime dateTime = DateTime.UnixEpoch.AddSeconds(epochSeconds);
        DateOnly dateOnly = new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        return dateOnly.ToString("O");
    }

    /// <summary>
    /// 格式化时间为ISO-8601格式
    /// 固定为:<code>HH:mm:ss</code>格式
    /// </summary>
    public static string FormatTime(long epochSeconds) {
        DateTime dateTime = DateTime.UnixEpoch.AddSeconds(epochSeconds);
        TimeOnly timeOnly = new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second);
        return timeOnly.ToString("HH:mm:ss");
    }

    public static int ParseOffset(string offsetString) {
        if (offsetString == "Z" || offsetString == "z") {
            return 0;
        }
        if (offsetString[0] != '+' && offsetString[0] != '-') {
            throw new DsonParseException("Invalid offsetString, plus/minus not found when expected: " + offsetString);
        }
        // 不想写得太复杂，补全后解析
        switch (offsetString.Length) {
            case 2: { // ±H
                offsetString = new StringBuilder(offsetString, 9)
                    .Insert(1, '0')
                    .Append(":00:00")
                    .ToString();
                break;
            }
            case 3: { // ±HH
                offsetString = offsetString + ":00:00";
                break;
            }
            case 5: { // ±H:mm
                offsetString = new StringBuilder(offsetString, 9)
                    .Insert(1, '0')
                    .Append(":00")
                    .ToString();
                break;
            }
            case 6: { // ±HH:mm
                offsetString = offsetString + ":00";
                break;
            }
            case 9: { // ±HH:mm:ss
                break;
            }
        }
        int seconds = DsonInternals.ToSecondOfDay(ParseTime(offsetString.Substring(1)));
        if (offsetString[0] == '+') {
            return seconds;
        }
        return -1 * seconds;
    }

    public static string FormatOffset(int offsetSeconds) {
        if (offsetSeconds == 0) {
            return "Z";
        }
        string sign = offsetSeconds < 0 ? "-" : "+";
        string timeString = new TimeOnly(offsetSeconds * DatetimeUtil.TicksPerSecond)
            .ToString("HH:mm:ss");
        if (offsetSeconds % 60 == 0) { // 没有秒部分
            return sign + timeString.Substring(0, 5);
        } else {
            return sign + timeString;
        }
    }

    #endregion

    #region 常量

    public const string NamesDate = "date";
    public const string NamesTime = "time";
    public const string NamesMillis = "millis";

    public const string NamesSeconds = "seconds";
    public const string NamesNanos = "nanos";
    public const string NamesOffset = "offset";
    public const string NamesEnables = "enables";

    public static readonly FieldNumber NumbersSeconds = FieldNumber.OfLnumber(0);
    public static readonly FieldNumber NumbersNanos = FieldNumber.OfLnumber(1);
    public static readonly FieldNumber NumbersOffset = FieldNumber.OfLnumber(2);
    public static readonly FieldNumber NumbersEnables = FieldNumber.OfLnumber(3);

    #endregion
}