﻿#region LICENSE

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

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson数据类型枚举
/// </summary>
public enum DsonType
{
    /** 对象的结束标识 */
    EndOfObject = 0,

    Int32 = 1,
    Int64 = 2,
    Float = 3,
    Double = 4,
    Boolean = 5,
    String = 6,
    Null = 7,

    /// <summary>
    /// 二进制字节数组
    /// 我们为二进制字节数组也提供了一个扩展子类型，实现一定程度上的自解释
    /// </summary>
    Binary = 8,

    /// <summary>
    /// Int32的扩展类型
    /// 基本类型的int无法直接表达其使用目的，需要额外的类型支持；
    /// 通过扩展类型，可避免破坏业务代码，可避免用户自行封装
    /// </summary>
    ExtInt32 = 9,
    /// <summary>
    /// Int64的扩展类型
    /// </summary>
    ExtInt64 = 10,
    /// <summary>
    /// Double的扩展类型
    /// </summary>
    ExtDouble = 11,
    /// <summary>
    /// String的扩展类型
    /// </summary>
    ExtString = 12,

    /// <summary>
    /// 对象引用
    /// </summary>
    Reference = 13,
    /// <summary>
    /// 时间戳
    /// </summary>
    Timestamp = 14,

    /// <summary>
    /// 对象头信息，与Object类型编码格式类似
    /// 但header不可以再直接嵌入header
    /// </summary>
    Header = 29,
    /// <summary>
    /// 数组(v,v,v...)
    /// </summary>
    Array = 30,
    /// <summary>
    /// 普通对象(k,v,k,v...)
    /// </summary>
    Object = 31,
}

/// <summary>
/// DsonType的工具类
/// </summary>
public static class DsonTypes
{
    private static readonly DsonType[] LookUp;

    /** 用于表示无效DsonType的共享值 */
    public static readonly DsonType Invalid = (DsonType)(-1);

    static DsonTypes() {
        LookUp = new DsonType[(int)DsonType.Object + 1];
        foreach (var dsonType in Enum.GetValues<DsonType>()) {
            LookUp[(int)dsonType] = dsonType;
        }
    }

    /** DsonType是否表示Dson的Number */
    public static bool IsNumber(this DsonType dsonType) {
        return dsonType switch
        {
            DsonType.Int32 => true,
            DsonType.Int64 => true,
            DsonType.Float => true,
            DsonType.Double => true,
            _ => false
        };
    }

    /** DsonType关联的值在二进制编码时是否包含WireType */
    public static bool HasWireType(this DsonType dsonType) {
        return dsonType switch
        {
            DsonType.Int32 => true,
            DsonType.Int64 => true,
            DsonType.ExtInt32 => true,
            DsonType.ExtInt64 => true,
            _ => false
        };
    }

    /** DsonType是否表示容器类型；header不属于普通意义上的容器 */
    public static bool IsContainer(this DsonType dsonType) {
        return dsonType == DsonType.Object || dsonType == DsonType.Array;
    }

    /** DsonType是否是容器类型或Header */
    public static bool IsContainerOrHeader(this DsonType dsonType) {
        return dsonType == DsonType.Object || dsonType == DsonType.Array || dsonType == DsonType.Header;
    }

    /** Dson是否是KV结构 */
    public static bool IsObjectLike(this DsonType dsonType) {
        return dsonType == DsonType.Object || dsonType == DsonType.Header;
    }

    /** 通过Number获取对应的枚举 */
    public static DsonType ForNumber(int number) {
        return LookUp[number];
    }
}