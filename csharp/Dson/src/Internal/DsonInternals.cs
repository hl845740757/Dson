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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Internal;

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

    /** 是否启用了所有标记 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnabled(int value, int mask) {
        return (value & mask) == mask;
    }

    /** 是否禁用了任意标记位 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAnyDisabled(int value, int mask) {
        return (value & mask) != mask;
    }

    /** 是否启用了任意标记 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAnyEnabled(int value, int mask) {
        return (value & mask) != 0;
    }

    /** 是否所有标记位都禁用了 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAllDisabled(int value, int mask) {
        return (value & mask) == 0;
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

    public static readonly DateOnly UtcEpochDate = new DateOnly(1970, 1, 1);

    #endregion

    #region 集合Util

    public static IGenericDictionary<TK, DsonValue> NewLinkedDictionary<TK>(int capacity = 0) {
        return new LinkedDictionary<TK, DsonValue>(capacity);
    }

    public static IGenericDictionary<TK, DsonValue> NewLinkedDictionary<TK>(IDictionary<TK, DsonValue> src) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        var dictionary = new LinkedDictionary<TK, DsonValue>();
        dictionary.PutAll(src);
        return dictionary;
    }

    public static string ToString<T>(ICollection<T> collection) {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        StringBuilder sb = new StringBuilder(64);
        sb.Append('[');
        bool first = true;
        foreach (T value in collection) {
            if (first) {
                first = false;
            } else {
                sb.Append(',');
            }
            if (value == null) {
                sb.Append("null");
            } else {
                sb.Append(value.ToString());
            }
        }
        sb.Append(']');
        return sb.ToString();
    }

    #endregion
}