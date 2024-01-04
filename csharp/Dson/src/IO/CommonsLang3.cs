#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;

namespace Wjybxx.Dson.IO;

/// <summary>
/// 这里的算法修改自Java的commons-lang3包
/// </summary>
internal class CommonsLang3
{
    public static bool IsParsable(string str) {
        if (string.IsNullOrEmpty(str)) {
            return false;
        }
        if (str[str.Length - 1] == '.') {
            return false;
        }
        if (str[0] == '-') {
            if (str.Length == 1) {
                return false;
            }
            return WithDecimalsParsing(str, 1);
        }
        return WithDecimalsParsing(str, 0);
    }

    private static bool WithDecimalsParsing(string str, int beginIdx) {
        int decimalPoints = 0;
        for (int i = beginIdx; i < str.Length; i++) {
            bool isDecimalPoint = str[i] == '.';
            if (isDecimalPoint) {
                decimalPoints++;
            }
            if (decimalPoints > 1) {
                return false;
            }
            if (!isDecimalPoint && !char.IsDigit(str[i])) {
                return false;
            }
        }
        return true;
    }

    /** 字节数组转16进制 */
    private static readonly char[] DigitsUpper = new[] {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };

    /** 编码长度固定位 dataLen * 2 */
    public static void EncodeHex(byte[] data, int dataOffset, int dataLen,
                                 Span<char> outBuffer) {
        char[] toDigits = DigitsUpper;
        for (int i = dataOffset, j = 0; i < dataOffset + dataLen; i++) {
            outBuffer[j++] = toDigits[(0xF0 & data[i]) >> 4]; // 高4位
            outBuffer[j++] = toDigits[0x0F & data[i]]; // 低4位
        }
    }
}