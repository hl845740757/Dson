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

public enum DsonType
{
    /** 对象的结束标识 */
    END_OF_OBJECT = 0,

    INT32 = 1,
    INT64 = 2,
    FLOAT = 3,
    DOUBLE = 4,
    BOOLEAN = 5,
    STRING = 6,
    NULL = 7,

    /// <summary>
    /// 二进制字节数组
    /// 我们为二进制字节数组也提供了一个扩展子类型，实现一定程度上的自解释
    /// </summary>
    BINARY = 8,

    /// <summary>
    /// Int32的扩展类型
    /// 基本类型的int无法直接表达其使用目的，需要额外的类型支持；
    /// 通过扩展类型，可避免破坏业务代码，可避免用户自行封装
    /// </summary>
    EXT_INT32 = 9,
    EXT_INT64 = 10,
    EXT_DOUBLE = 11,
    EXT_STRING = 12,

    /// <summary>
    /// 对象引用
    /// </summary>
    REFERENCE = 13,
    /// <summary>
    /// 时间戳
    /// </summary>
    TIMESTAMP = 14,

    /// <summary>
    /// 对象头信息，与Object类型编码格式类似
    /// 但header不可以再直接嵌入header
    /// </summary>
    HEADER = 29,
    /// <summary>
    /// 数组(v,v,v...)
    /// </summary>
    ARRAY = 30,
    /// <summary>
    /// 普通对象(k,v,k,v...)
    /// </summary>
    OBJECT = 31,
}

public static class DsonTypeExt
{
    private static readonly IList<DsonType> LOOK_UP;
    public static readonly DsonType INVALID = (DsonType)(-1);

    static DsonTypeExt() {
        LOOK_UP = new List<DsonType>((int)DsonType.OBJECT + 1);
        foreach (var dsonType in Enum.GetValues<DsonType>()) {
            LOOK_UP[(int)dsonType] = dsonType;
        }
    }

    public static bool IsNumber(this DsonType dsonType) {
        return dsonType switch
        {
            DsonType.INT32 => true,
            DsonType.INT64 => true,
            DsonType.FLOAT => true,
            DsonType.DOUBLE => true,
            _ => false
        };
    }

    /** {@link WireType} */
    public static bool HasWireType(this DsonType dsonType) {
        return dsonType switch
        {
            DsonType.INT32 => true,
            DsonType.INT64 => true,
            DsonType.EXT_INT32 => true,
            DsonType.EXT_INT64 => true,
            _ => false
        };
    }

    /** header不属于普通意义上的容器 */
    public static bool IsContainer(this DsonType dsonType) {
        return dsonType == DsonType.OBJECT || dsonType == DsonType.ARRAY;
    }

    public static bool IsContainerOrHeader(this DsonType dsonType) {
        return dsonType == DsonType.OBJECT || dsonType == DsonType.ARRAY || dsonType == DsonType.HEADER;
    }

    public static DsonType ForNumber(int number) {
        return LOOK_UP[number];
    }
}