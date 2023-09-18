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

package cn.wjybxx.dson.text;

/**
 * 文本模式
 *
 * @author wjybxx
 * date - 2023/9/18
 */
public enum DsonMode {

    /**
     * 标准的Dson模式
     * 每一行由 行首 + 内容构成。
     */
    STANDARD,

    /**
     * 宽松模式
     * 没有行首的文本，支持读取Json
     */
    RELAXED,

}