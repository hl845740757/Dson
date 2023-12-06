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

using System.Globalization;

namespace Dson.Text;

/// <summary>
/// 这里提供默认数字格式化方式
/// </summary>
public class NumberStyles
{
    public static readonly INumberStyle Simple = new SimpleStyle();

    /** double能精确表示的最大整数 */
    private const long DOUBLE_MAX_LONG = (1L << 53) - 1;

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
}