#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
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
    public const int MaskOffsetDatetime = MaskDate | MaskTime | MaskOffset;

    public readonly long Seconds;
    public readonly int Nanos;
    public readonly int Offset;
    public readonly int Enables;

    public OffsetTimestamp(long seconds)
        : this(seconds, 0, 0, MaskDatetime) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="seconds">纪元时间秒时间戳</param>
    /// <param name="nanos">时间戳的纳秒部分</param>
    /// <param name="offset">时区偏移量--秒</param>
    /// <param name="enables">启用的字段信息</param>
    public OffsetTimestamp(long seconds, int nanos, int offset, int enables) {
        this.Seconds = seconds;
        this.Nanos = nanos;
        this.Offset = offset;
        this.Enables = enables;
    }

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
                offsetString = offsetString + "0:00:00";
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
        long seconds = (ParseTime(offsetString.Substring(1)).Ticks / DsonInternals.TicksPerSecond);
        if (offsetString[0] == '+') {
            return (int)seconds;
        }
        return (int)(-1 * seconds);
    }

    public static string FormatDateTime(long seconds) {
        return DateTime.UnixEpoch.AddSeconds(seconds).ToString("s");
    }

    public static string FormatDate(long epochSeconds) {
        string fullDate = DateTime.UnixEpoch.AddSeconds(epochSeconds).ToString("s");
        return fullDate.Substring(0, fullDate.IndexOf('T'));
    }

    public static string FormatTime(long epochSeconds) {
        string fullDate = DateTime.UnixEpoch.AddSeconds(epochSeconds).ToString("s");
        return fullDate.Substring(fullDate.IndexOf('T') + 1);
    }

    public static string FormatOffset(int offsetSeconds) {
        if (offsetSeconds == 0) {
            return "Z";
        }
        string sign = offsetSeconds < 0 ? "-" : "+";
        if (offsetSeconds % 60 == 0) { // 没有秒部分
            return sign + FormatTime(Math.Abs(offsetSeconds)).Substring(0, 5);
        }
        else {
            return sign + FormatTime(Math.Abs(offsetSeconds));
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