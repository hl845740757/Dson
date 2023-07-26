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

package cn.wjybxx.dson.internal;

import org.apache.commons.lang3.BooleanUtils;
import org.apache.commons.lang3.math.NumberUtils;

import java.util.Objects;
import java.util.Properties;

/**
 * 存放一些基础的工具方法，不想定义过多的小类，减少维护量
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class InternalUtils {

    /**
     * 如果给定参数为null，则返回给定的默认值，否则返回值本身
     * {@link Objects#requireNonNullElse(Object, Object)}不允许def为null
     */
    public static <V> V nullToDef(V obj, V def) {
        return obj == null ? def : obj;
    }

    public static void recoveryInterrupt(Throwable ex) {
        if (ex instanceof InterruptedException) {
            Thread.currentThread().interrupt();
        }
    }

    // region properties

    public static int getInt(Properties properties, String key, int def) {
        String v = properties.getProperty(key);
        return NumberUtils.toInt(v, def);
    }

    public static long getLong(Properties properties, String key, long def) {
        String v = properties.getProperty(key);
        return NumberUtils.toLong(v, def);
    }

    public static boolean getBool(Properties properties, String key, boolean def) {
        String v = properties.getProperty(key);
        return v != null ? BooleanUtils.toBoolean(v) : def;
    }

    public static String getString(Properties properties, String key, String def) {
        return properties.getProperty(key, def);
    }

    // endregion

    // region bits

    public static boolean isEnabled(int value, int mask) {
        return (value & mask) == mask;
    }

    public static boolean isDisabled(int value, int mask) {
        return (value & mask) != mask;
    }

    // endregion
}