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

    public OffsetTimestamp(long seconds) : this(seconds, 0, 0, MASK_DATETIME) {
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