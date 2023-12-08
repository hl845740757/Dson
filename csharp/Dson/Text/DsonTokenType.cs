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
/// 文本token类型
/// </summary>
public enum DsonTokenType
{
    /** 到达文件尾部 */
    EOF,

    /** 对象开始符， '{' */
    BEGIN_OBJECT,
    /** 对象结束符， '}' */
    END_OBJECT,

    /** 数组开始符，'[' */
    BEGIN_ARRAY,
    /** 数组结束符，']' */
    END_ARRAY,

    /** 对象头开始符 '@{' */
    BEGIN_HEADER,
    /** Header的简写形式 '@clsName' -- clsName不是基础类型 */
    CLASS_NAME,

    /** KV分隔符，冒号 ':' */
    COLON,
    /** 元素分隔符，英文逗号 ',' */
    COMMA,

    /** 显式声明 '@i' */
    INT32,
    /** 显式声明 '@L' */
    INT64,
    /** 显式声明 '@f' */
    FLOAT,
    /** 显式声明 '@d' */
    DOUBLE,
    /** 显式声明 '@b' */
    BOOL,
    /** 显式声明 双引号 或 '@s' 或 '@ss' */
    STRING,
    /** 显式声明 '@N' */
    NULL,

    /** 无引号字符串，scan的时候不解析，使得返回后可以根据上下文推断其类型 */
    UNQUOTE_STRING,
}