﻿#region LICENSE

//  Copyright 2023 wjybxx
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

namespace Dson.Text;

/// <summary>
/// 对象和数组的缩进模式
/// </summary>
public enum ObjectStyle
{
    /** 缩进模式 */
    Indent,

    /** 流模式 - 线性模式 */
    Flow,
}