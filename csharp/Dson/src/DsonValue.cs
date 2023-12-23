#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// Dson所有值类型的抽象
/// </summary>
public abstract class DsonValue
{
    public abstract DsonType DsonType { get; }

    #region 拆箱类型

    public int AsInt32() => ((DsonInt32)this).IntValue;

    public long AsInt64() => ((DsonInt64)this).LongValue;

    public float AsFloat() => ((DsonFloat)this).FloatValue;

    public double AsDouble() => ((DsonDouble)this).DoubleValue;

    public bool AsBool() => ((DsonBool)this).Value;

    public string AsString() => ((DsonString)this).Value;

    public ObjectRef AsReference() => ((DsonReference)this).Value;

    public OffsetTimestamp AsTimestamp() => ((DsonTimestamp)this).Value;

    public DsonNumber AsNumber() => ((DsonNumber)this);

    #endregion

    #region 装箱类型

    public DsonInt32 AsDsonInt32() => (DsonInt32)this;

    public DsonInt64 AsDsonInt64() => (DsonInt64)this;

    public DsonFloat AsDsonFloat() => (DsonFloat)this;

    public DsonDouble AsDsonDouble() => (DsonDouble)this;

    public DsonBool AsDsonBool() => (DsonBool)this;

    public DsonString AsDsonString() => (DsonString)this;

    public DsonReference AsDsonReference() => (DsonReference)this;

    public DsonTimestamp AsDsonTimestamp() => (DsonTimestamp)this;

    #endregion

    #region Dson特定类型

    public DsonNull AsNull() => (DsonNull)this;

    public DsonBinary AsBinary() => (DsonBinary)this;

    public DsonExtInt32 AsExtInt32() => (DsonExtInt32)this;

    public DsonExtInt64 AsExtInt64() => (DsonExtInt64)this;

    public DsonExtDouble AsExtDouble() => (DsonExtDouble)this;

    public DsonExtString AsExtString() => (DsonExtString)this;

    public DsonHeader<T> AsHeader<T>() => (DsonHeader<T>)this;

    public DsonArray<T> AsArray<T>() => (DsonArray<T>)this;

    public DsonObject<T> AsObject<T>() => (DsonObject<T>)this;

    public DsonHeader<string> AsHeader() => (DsonHeader<string>)this;

    public DsonArray<string> AsArray() => (DsonArray<string>)this;

    public DsonObject<string> AsObject() => (DsonObject<string>)this;

    public DsonHeader<FieldNumber> AsHeaderLite() => (DsonHeader<FieldNumber>)this;

    public DsonArray<FieldNumber> AsArrayLite() => (DsonArray<FieldNumber>)this;

    public DsonObject<FieldNumber> AsObjectLite() => (DsonObject<FieldNumber>)this;

    #endregion

    #region util

    public bool IsNumber => DsonType.IsNumber();

    #endregion
}