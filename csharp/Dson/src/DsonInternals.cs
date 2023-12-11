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

using System.Runtime.CompilerServices;

namespace Wjybxx.Dson;

internal static class DsonInternals
{
    //c#居然没有一开始就支持逻辑右移...C#11提供了逻辑右移，但目前.NET6是主流
    public static int LogicalShiftRight(int val, int offset) {
        if (offset < 0) throw new ArgumentException("invalid offset " + offset);
        if (offset == 0) return val;
        uint uval = (uint)val;
        return (int)(uval >> offset);
    }

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

    public static bool IsDisabled(int value, int mask) {
        return (value & mask) != mask;
    }

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

    public static IDictionary<TK, DsonValue> NewLinkedDictionary<TK>(int capacity = 0) {
        return new Dictionary<TK, DsonValue>(capacity);
    }

    public static IDictionary<TK, DsonValue> NewLinkedDictionary<TK>(IDictionary<TK, DsonValue> src) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        return new Dictionary<TK, DsonValue>(src);
    }

    public static List<T> NewList<T>(T first) {
        var list = new List<T>(1);
        list.Add(first);
        return list;
    }

    public static List<T> NewList<T>(T first, T second) {
        var list = new List<T>(2);
        list.Add(first);
        list.Add(second);
        return list;
    }

    public static List<T> NewList<T>(T first, T second, T third) {
        var list = new List<T>(3);
        list.Add(first);
        list.Add(second);
        list.Add(third);
        return list;
    }

    public static List<T> NewList<T>(params T[] elements) {
        return new List<T>(elements);
    }

    public static bool ContainsRef<TE>(IList<TE> list, TE element) where TE : class {
        for (int i = 0, size = list.Count; i < size; i++) {
            if (ReferenceEquals(list[i], element)) {
                return true;
            }
        }
        return false;
    }

    public static int IndexOfRef<TE>(IList<TE> list, Object element) where TE : class {
        for (int idx = 0, size = list.Count; idx < size; idx++) {
            if (ReferenceEquals(list[idx], element)) {
                return idx;
            }
        }
        return -1;
    }

    public static int LastIndexOfRef<TE>(IList<TE> list, Object element) where TE : class {
        for (int idx = list.Count - 1; idx >= 0; idx--) {
            if (ReferenceEquals(list[idx], element)) {
                return idx;
            }
        }
        return -1;
    }

    public static bool RemoveRef<TE>(IList<TE> list, Object element) where TE : class {
        int index = IndexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        list.RemoveAt(index);
        return true;
    }

    #endregion

    #region 数组

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src">原始四组</param>
    /// <param name="newLen">可大于或小于原始数组长度</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T[] CopyOf<T>(T[] src, int newLen) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        if (newLen < 0) throw new ArgumentException("newLen cant be negative");
        T[] result = new T[newLen];
        Array.Copy(src, 0, result, 0, Math.Min(src.Length, newLen));
        return result;
    }

    public static bool ContainsRef<TE>(TE[] list, TE element) where TE : class {
        for (int i = 0, size = list.Length; i < size; i++) {
            if (ReferenceEquals(list[i], element)) {
                return true;
            }
        }
        return false;
    }

    public static int IndexOfRef<TE>(TE[] list, Object element) where TE : class {
        for (int idx = 0, size = list.Length; idx < size; idx++) {
            if (ReferenceEquals(list[idx], element)) {
                return idx;
            }
        }
        return -1;
    }

    public static int LastIndexOfRef<TE>(TE[] list, Object element) where TE : class {
        for (int idx = list.Length - 1; idx >= 0; idx--) {
            if (ReferenceEquals(list[idx], element)) {
                return idx;
            }
        }
        return -1;
    }

    #endregion
}