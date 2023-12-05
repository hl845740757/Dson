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

public static class DsonInternals
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

    public static bool isEnabled(int value, int mask) {
        return (value & mask) == mask;
    }

    public static bool isDisabled(int value, int mask) {
        return (value & mask) != mask;
    }

    public static bool IsStringKey<TName>() {
        if (typeof(TName) == typeof(string)) {
            return true;
        }
        if (typeof(TName) == typeof(int)) {
            return false;
        }
        throw new InvalidCastException("Cant cast TName to string or int, type: " + typeof(TName));
    }

    #region 集合Util

    public static IDictionary<TK, DsonValue> NewLinkedDictionary<TK>(int capacity = 0) {
        return new Dictionary<TK, DsonValue>(capacity);
    }

    public static IDictionary<TK, DsonValue> NewLinkedDictionary<TK>(IDictionary<TK, DsonValue> src) {
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

    #endregion
}