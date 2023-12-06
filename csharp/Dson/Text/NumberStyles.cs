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
                value.
            }
            
            return new StyleOut(value.ToString(), )
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }
}