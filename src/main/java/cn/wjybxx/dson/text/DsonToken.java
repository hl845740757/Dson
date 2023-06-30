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

import java.util.Objects;

/**
 * token可能记录位置更有助于排查问题
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonToken {

    private final TokenType type;
    private final Object value;
    private final int pos;

    /**
     * @param pos token所在的位置，-1表示动态生成的token
     */
    public DsonToken(TokenType type, Object value, int pos) {
        this.type = Objects.requireNonNull(type);
        this.value = value;
        this.pos = pos;
    }

    public TokenType getType() {
        return type;
    }

    public Object getValue() {
        return value;
    }

    public int getPos() {
        return pos;
    }

    //

    public String castAsString() {
        return (String) value;
    }

    public char firstChar() {
        String value = (String) this.value;
        return value.charAt(0);
    }

    public char lastChar() {
        String value = (String) this.value;
        return value.charAt(value.length() - 1);
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonToken token = (DsonToken) o;

        if (pos != token.pos) return false;
        if (type != token.type) return false;
        return Objects.equals(value, token.value);
    }

    @Override
    public int hashCode() {
        int result = type.hashCode();
        result = 31 * result + (value != null ? value.hashCode() : 0);
        result = 31 * result + pos;
        return result;
    }

    @Override
    public String toString() {
        return "DsonToken{" +
                "type=" + type +
                ", value=" + value +
                ", pos=" + pos +
                '}';
    }
}