﻿/*
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
/// 行首类型（Line Head Type）
/// </summary>
public enum LineHead
{
    /** 注释 */
    COMMENT,
    /** 添加新行 */
    APPEND_LINE,
    /** 与上一行合并 */
    APPEND,
    /** 切换模式 */
    SWITCH_MODE,
    /** 文本输入结束 */
    END_OF_TEXT
}