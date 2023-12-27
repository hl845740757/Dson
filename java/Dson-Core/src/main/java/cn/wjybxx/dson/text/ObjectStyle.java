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
 * @author wjybxx
 * date - 2023/6/6
 */
public enum ObjectStyle implements IStyle {

    /**
     * 缩进模式
     * 注意：当父节点是Flow模式时，当前节点也将转换为Flow模式
     */
    INDENT,

    /** 流模式 - 线性模式 */
    FLOW,

}