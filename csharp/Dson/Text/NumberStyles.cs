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

using System.Globalization;

namespace Dson.Text;

/// <summary>
/// 这里提供默认数字格式化方式
/// </summary>
public class NumberStyles
{
    /** 普通打印 -- 超过表示范围时会添加类型标签 */
    public static readonly INumberStyle Simple = new SimpleStyle();
    /** 总是打印类型 */
    public static readonly INumberStyle Typed = new TypedStyle();

    /** 16进制，打印正负号 -- 不支持浮点数 */
    public static readonly INumberStyle SignedHex = new SignedHexStyle();
    /** 无符号16进制，按位打印 -- 不支持浮点数 */
    public static readonly INumberStyle UnsignedHex = new UnsignedHexStyle();

    /** 2进制，打印正负号 -- 不支持浮点数；需要 .Net8+ */
    public static readonly INumberStyle SignedBinary = new SignedBinaryStyle();
    /** 无符号2进制，按位打印 -- 不支持浮点数；需要 .Net8+  */
    public static readonly INumberStyle UnsignedBinary = new UnsignedBinaryStyle();

    /** double能精确表示的最大整数 */
    private const long DOUBLE_MAX_LONG = (1L << 53) - 1;

    #region simple

    private class SimpleStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut(value.ToString(), false);
        }

        public StyleOut ToString(long value) {
            return new StyleOut(value.ToString(), value >= DOUBLE_MAX_LONG);
        }

        public StyleOut ToString(float value) {
            if (float.IsInfinity(value) || float.IsNaN(value)) {
                return new StyleOut(value.ToString(CultureInfo.InvariantCulture), true);
            }
            int iv = (int)value;
            if (iv == value) {
                return new StyleOut(iv.ToString(), false);
            }
            else {
                string str = value.ToString(CultureInfo.InvariantCulture);
                return new StyleOut(str, str.IndexOf('E') >= 0);
            }
        }

        public StyleOut ToString(double value) {
            if (double.IsInfinity(value) || double.IsNaN(value)) {
                return new StyleOut(value.ToString(CultureInfo.InvariantCulture), true);
            }
            long lv = (long)value;
            if (lv == value) {
                return new StyleOut(lv.ToString(), false);
            }
            else {
                string str = value.ToString(CultureInfo.InvariantCulture);
                return new StyleOut(str, str.IndexOf('E') >= 0);
            }
        }
    }

    private class TypedStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut(Simple.ToString(value).Value, true);
        }

        public StyleOut ToString(long value) {
            return new StyleOut(Simple.ToString(value).Value, true);
        }

        public StyleOut ToString(float value) {
            return new StyleOut(Simple.ToString(value).Value, true);
        }

        public StyleOut ToString(double value) {
            return new StyleOut(Simple.ToString(value).Value, true);
        }
    }

    #endregion

    #region 16进制

    private class SignedHexStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            if (value < 0 && value != int.MinValue) {
                return new StyleOut("-0x" + (-1 * value).ToString("X"), true);
            }
            else {
                return new StyleOut("0x" + value.ToString("X"), true);
            }
        }

        public StyleOut ToString(long value) {
            if (value < 0 && value != long.MinValue) {
                return new StyleOut("-0x" + (-1 * value).ToString("X"), true);
            }
            else {
                return new StyleOut("0x" + value.ToString("X"), true);
            }
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    private class UnsignedHexStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut("0x" + value.ToString("X"), true);
        }

        public StyleOut ToString(long value) {
            return new StyleOut("0x" + value.ToString("X"), true);
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region 二进制

    private class SignedBinaryStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            if (value < 0 && value != int.MinValue) {
                return new StyleOut("-0b" + (-1 * value).ToString("B"), true);
            }
            else {
                return new StyleOut("0b" + value.ToString("B"), true);
            }
        }

        public StyleOut ToString(long value) {
            if (value < 0 && value != long.MinValue) {
                return new StyleOut("-0b" + (-1 * value).ToString("B"), true);
            }
            else {
                return new StyleOut("0b" + value.ToString("B"), true);
            }
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    private class UnsignedBinaryStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut("0b" + value.ToString("B"), true);
        }

        public StyleOut ToString(long value) {
            return new StyleOut("0b" + value.ToString("B"), true);
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    #endregion
}