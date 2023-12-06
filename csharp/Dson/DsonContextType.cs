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
/// Dson上下文类型
/// </summary>
public enum DsonContextType
{
    /** 当前在最顶层，尚未开始读写（topLevel相当于一个数组） */
    TOP_LEVEL,
    /** 当前是一个普通对象结构 */
    OBJECT,
    /** 当前是一个数组结构 */
    ARRAY,
    /** 当前是一个Header结构 - 类似Object */
    HEADER,
}

public static class DsonContextTypes
{
    /// <summary>
    /// 上下文的开始符号
    /// </summary>
    public static string? startSymbol(this DsonContextType contextType) {
        return contextType switch {
            DsonContextType.TOP_LEVEL => null,
            DsonContextType.OBJECT => "{",
            DsonContextType.ARRAY => "[",
            DsonContextType.HEADER => "@{",
            _ => throw new ArgumentException(nameof(contextType))
        };
    }

    /// <summary>
    /// 上下文的结束符号
    /// </summary>
    public static string? endSymbol(this DsonContextType contextType) {
        return contextType switch {
            DsonContextType.TOP_LEVEL => null,
            DsonContextType.OBJECT => "}",
            DsonContextType.ARRAY => "]",
            DsonContextType.HEADER => "}",
            _ => throw new ArgumentException(nameof(contextType))
        };
    }

    public static bool isContainer(this DsonContextType contextType) {
        return contextType == DsonContextType.OBJECT || contextType == DsonContextType.ARRAY;
    }

    public static bool isLikeArray(this DsonContextType contextType) {
        return contextType == DsonContextType.ARRAY || contextType == DsonContextType.TOP_LEVEL;
    }

    public static bool isLikeObject(this DsonContextType contextType) {
        return contextType == DsonContextType.OBJECT || contextType == DsonContextType.HEADER;
    }
}