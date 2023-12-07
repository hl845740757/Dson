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


namespace Dson;

/// <summary>
/// 字段编号
/// 在使用int表达类字段名字的时候，编号并不是随意的，而是具有编码的；字段的编号由 继承深度+本地编号 构成，
/// 
/// 1.Dson最初是为序列化设计的，是支持继承的。
/// 2.完整编号不可直接比较，需要调用这里提供的静态比较方法
/// </summary>
public struct FieldNumber : IEquatable<FieldNumber>, IComparable<FieldNumber>, IComparable
{
    public static readonly FieldNumber Zero = new FieldNumber(0, 0);

    /// <summary>
    /// 继承深度
    /// </summary>
    public readonly byte Idep;
    /// <summary>
    /// 字段本地编码
    /// </summary>
    public readonly int Lnumber;

    public FieldNumber(byte idep, int lnumber) {
        if (idep > Dsons.IDEP_MAX_VALUE) {
            throw InvalidArgs(idep, lnumber);
        }
        if (lnumber < 0) {
            throw InvalidArgs(idep, lnumber);
        }
        this.Idep = idep;
        this.Lnumber = lnumber;
    }

    private static Exception InvalidArgs(int idep, int lnumber) {
        throw new ArgumentException($"idep: {idep}, lnumber: {lnumber}");
    }

    /// <summary>
    /// 字段的完整编号
    /// 注意：完整编号不可直接比较，需要调用这里提供的静态比较方法
    /// </summary>
    public int FullNumber => Dsons.MakeFullNumber(Idep, Lnumber);


    public static FieldNumber Of(int idep, int lnumber) {
        if (idep < 0 || idep > byte.MaxValue) {
            throw InvalidArgs(idep, lnumber);
        }
        return new FieldNumber((byte)idep, lnumber);
    }

    /// <summary>
    /// 通过字段本地编号创建结构，默认继承深度0
    /// </summary>
    /// <param name="lnumber"></param>
    /// <returns></returns>
    public static FieldNumber OfLnumber(int lnumber) {
        return new FieldNumber(0, lnumber);
    }

    /// <summary>
    /// 通过字段的完整编号创建结构
    /// </summary>
    /// <param name="fullNumber"></param>
    /// <returns></returns>
    public static FieldNumber OfFullNumber(int fullNumber) {
        return new FieldNumber(Dsons.IdepOfFullNumber(fullNumber), Dsons.LnumberOfFullNumber(fullNumber));
    }

    /// <summary>
    /// 比较两个字段的大小
    /// </summary>
    /// <param name="fullNumber1">字段完整编号</param>
    /// <param name="fullNumber2">字段完整编号</param>
    /// <returns></returns>
    public static int Compare(int fullNumber1, int fullNumber2) {
        if (fullNumber1 == fullNumber2) {
            return 0;
        }
        // 先比较继承深度 -- 父类字段靠前
        var idepComparison = Dsons.IdepOfFullNumber(fullNumber1)
            .CompareTo(Dsons.IdepOfFullNumber(fullNumber2));
        if (idepComparison != 0) {
            return idepComparison;
        }
        // 再比较类
        return Dsons.LnumberOfFullNumber(fullNumber1)
            .CompareTo(Dsons.LnumberOfFullNumber(fullNumber2));
    }

    #region equals

    public bool Equals(FieldNumber other) {
        return Idep == other.Idep && Lnumber == other.Lnumber;
    }

    public override bool Equals(object? obj) {
        return obj is FieldNumber other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Idep, Lnumber);
    }

    public static bool operator ==(FieldNumber left, FieldNumber right) {
        return left.Equals(right);
    }

    public static bool operator !=(FieldNumber left, FieldNumber right) {
        return !left.Equals(right);
    }

    public int CompareTo(FieldNumber other) {
        var idepComparison = Idep.CompareTo(other.Idep);
        if (idepComparison != 0) return idepComparison;
        return Lnumber.CompareTo(other.Lnumber);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is FieldNumber other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(FieldNumber)}");
    }

    public static bool operator <(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}