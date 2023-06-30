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
 * date - 2023/6/2
 */
public enum LheadType {

    /** 注释 */
    COMMENT(DsonTexts.LHEAD_COMMENT),
    /** 添加新行 */
    APPEND_LINE(DsonTexts.LHEAD_APPEND_LINE),
    /** 与上一行合并 */
    APPEND(DsonTexts.LHEAD_APPEND),
    /** 切换模式 */
    SWITCH_MODE(DsonTexts.LHEAD_SWITCH_MODE),
    /** 文本输入结束 */
    END_OF_TEXT(DsonTexts.LHEAD_END_OF_TEXT);

    public final String label;

    LheadType(String label) {
        this.label = label;
    }

    public static LheadType forLabel(String label) {
        return switch (label) {
            case DsonTexts.LHEAD_COMMENT -> COMMENT;
            case DsonTexts.LHEAD_APPEND_LINE -> APPEND_LINE;
            case DsonTexts.LHEAD_APPEND -> APPEND;
            case DsonTexts.LHEAD_SWITCH_MODE -> SWITCH_MODE;
            case DsonTexts.LHEAD_END_OF_TEXT -> END_OF_TEXT;
            default -> null;
        };
    }

}