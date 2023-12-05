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

namespace Dson.Text;

/// <summary>
/// C#这里用值类型是非常方便的
/// </summary>
public struct StyleOut
{
    public readonly string Value;
    public readonly bool Typed;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value">文本</param>
    /// <param name="typed">输出是否需要加类型</param>
    public StyleOut(string value, bool typed) {
        Value = value;
        Typed = typed;
    }
}