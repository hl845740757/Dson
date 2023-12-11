﻿#region LICENSE

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

namespace Wjybxx.Dson;

/// <summary>
/// Dson上下文类型
/// </summary>
public enum DsonContextType
{
    /** 当前在最顶层，尚未开始读写（topLevel相当于一个数组） */
    TopLevel,
    /** 当前是一个普通对象结构 */
    Object,
    /** 当前是一个数组结构 */
    Array,
    /** 当前是一个Header结构 - 类似Object */
    Header,
}

public static class DsonContextTypes
{
    /// <summary>
    /// 上下文的开始符号
    /// </summary>
    public static string? GetStartSymbol(this DsonContextType contextType) {
        return contextType switch {
            DsonContextType.TopLevel => null,
            DsonContextType.Object => "{",
            DsonContextType.Array => "[",
            DsonContextType.Header => "@{",
            _ => throw new ArgumentException(nameof(contextType))
        };
    }

    /// <summary>
    /// 上下文的结束符号
    /// </summary>
    public static string? GetEndSymbol(this DsonContextType contextType) {
        return contextType switch {
            DsonContextType.TopLevel => null,
            DsonContextType.Object => "}",
            DsonContextType.Array => "]",
            DsonContextType.Header => "}",
            _ => throw new ArgumentException(nameof(contextType))
        };
    }

    public static bool IsContainer(this DsonContextType contextType) {
        return contextType == DsonContextType.Object || contextType == DsonContextType.Array;
    }

    public static bool IsLikeArray(this DsonContextType contextType) {
        return contextType == DsonContextType.Array || contextType == DsonContextType.TopLevel;
    }

    public static bool IsLikeObject(this DsonContextType contextType) {
        return contextType == DsonContextType.Object || contextType == DsonContextType.Header;
    }
}