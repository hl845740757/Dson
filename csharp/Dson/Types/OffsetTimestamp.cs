/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using System.Text;
using Dson.Text;

namespace Dson;

/// <summary>
/// 带时区偏移的时间戳
/// </summary>
public struct OffsetTimestamp : IEquatable<OffsetTimestamp>
{
    public const int MASK_DATE = 1;
    public const int MASK_TIME = 1 << 1;
    public const int MASK_OFFSET = 1 << 2;
    public const int MASK_NANOS = 1 << 3;

    public const int MASK_DATETIME = MASK_DATE | MASK_TIME;
    public const int MASK_OFFSET_DATETIME = MASK_DATE | MASK_TIME | MASK_OFFSET;

    public readonly long Seconds;
    public readonly int Nanos;
    public readonly int Offset;
    public readonly int Enables;

    public OffsetTimestamp(long seconds)
        : this(seconds, 0, 0, MASK_DATETIME) {
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

    public bool hasDate() {
        return DsonInternals.isEnabled(Enables, MASK_DATE);
    }

    public bool hasTime() {
        return DsonInternals.isEnabled(Enables, MASK_TIME);
    }

    public bool hasOffset() {
        return DsonInternals.isEnabled(Enables, MASK_OFFSET);
    }

    public bool canConvertNanosToMillis() {
        return (Nanos % 1000_000) == 0;
    }

    public int convertNanosToMillis() {
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

    public static DateTime parseDateTime(string datetimeString) {
        return DateTime.ParseExact(datetimeString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static DateOnly parseDate(string dateString) {
        return DateOnly.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static TimeOnly parseTime(string timeString) {
        return TimeOnly.ParseExact(timeString, "HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static int parseOffset(string offsetString) {
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
        long seconds = (parseTime(offsetString.Substring(1)).Ticks / DsonInternals.TicksPerSecond);
        if (offsetString[0] == '+') {
            return (int)seconds;
        }
        return (int)(-1 * seconds);
    }

    public static string formatDateTime(long seconds) {
        return DateTime.UnixEpoch.AddSeconds(seconds).ToString("s");
    }

    public static string formatDate(long epochSeconds) {
        string fullDate = DateTime.UnixEpoch.AddSeconds(epochSeconds).ToString("s");
        return fullDate.Substring(0, fullDate.IndexOf('T'));
    }

    public static string formatTime(long epochSeconds) {
        string fullDate = DateTime.UnixEpoch.AddSeconds(epochSeconds).ToString("s");
        return fullDate.Substring(fullDate.IndexOf('T') + 1);
    }

    public static string formatOffset(int offsetSeconds) {
        if (offsetSeconds == 0) {
            return "Z";
        }
        string sign = offsetSeconds < 0 ? "-" : "+";
        if (offsetSeconds % 60 == 0) { // 没有秒部分
            return sign + formatTime(Math.Abs(offsetSeconds)).Substring(0, 5);
        }
        else {
            return sign + formatTime(Math.Abs(offsetSeconds));
        }
    }

    #endregion


    #region 常量

    public const string NAMES_DATE = "date";
    public const string NAMES_TIME = "time";
    public const string NAMES_MILLIS = "millis";

    public const string NAMES_SECONDS = "seconds";
    public const string NAMES_NANOS = "nanos";
    public const string NAMES_OFFSET = "offset";
    public const string NAMES_ENABLES = "enables";

    public static readonly int NUMBERS_SECONDS = Dsons.MakeFullNumberZeroIdep(0);
    public static readonly int NUMBERS_NANOS = Dsons.MakeFullNumberZeroIdep(1);
    public static readonly int NUMBERS_OFFSET = Dsons.MakeFullNumberZeroIdep(2);
    public static readonly int NUMBERS_ENABLES = Dsons.MakeFullNumberZeroIdep(3);

    #endregion
}