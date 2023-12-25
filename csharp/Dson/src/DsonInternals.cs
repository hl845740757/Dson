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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson;

/// <summary>
/// Dson内部工具类
/// </summary>
internal static class DsonInternals
{
    //c#居然没有一开始就支持逻辑右移...C#11提供了逻辑右移，但目前.NET6是主流
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LogicalShiftRight(int val, int offset) {
        if (offset < 0) throw new ArgumentException("invalid offset " + offset);
        if (offset == 0) return val;
        uint uval = (uint)val;
        return (int)(uval >> offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LogicalShiftRight(long val, int offset) {
        if (offset < 0) throw new ArgumentException("invalid offset " + offset);
        if (offset == 0) return val;
        ulong uval = (ulong)val;
        return (long)(uval >> offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnabled(int value, int mask) {
        return (value & mask) == mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDisabled(int value, int mask) {
        return (value & mask) != mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStringKey<TName>() {
        if (typeof(TName) == typeof(string)) {
            return true;
        }
        if (typeof(TName) == typeof(FieldNumber)) {
            return false;
        }
        throw new InvalidCastException("Cant cast TName to string or FieldNumber, type: " + typeof(TName));
    }

    #region datetime

    internal const long TicksPerMillisecond = 10000;
    internal const long TicksPerSecond = TicksPerMillisecond * 1000;
    internal static readonly DateOnly UtcEpochDate = new DateOnly(1970, 1, 1);

    /// <summary>
    /// 转unix秒时间戳
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static long ToEpochSeconds(this DateTime dateTime) {
        return (long)dateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// 转Unix毫秒时间戳
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static long ToEpochMillis(this DateTime dateTime) {
        return (long)dateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
    }

    #endregion

    #region 集合Util

    public static List<T> NewList<T>(T first) {
        return new List<T>(1) { first };
    }

    public static List<T> NewList<T>(T first, T second) {
        return new List<T>(2) { first, second };
    }

    public static List<T> NewList<T>(T first, T second, T third) {
        return new List<T>(3) { first, second, third };
    }

    public static IDictionary<TK, DsonValue> NewLinkedDictionary<TK>(int capacity = 0) {
        return new Dictionary<TK, DsonValue>(capacity);
    }

    public static IDictionary<TK, DsonValue> NewLinkedDictionary<TK>(IDictionary<TK, DsonValue> src) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        var dictionary = new LinkedDictionary<TK, DsonValue>();
        dictionary.PutAll(src);
        return dictionary;
    }

    #endregion
}