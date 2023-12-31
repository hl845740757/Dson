﻿#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
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

using System;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

#pragma warning disable CS1591
namespace Wjybxx.Dson;

/// <summary>
/// 将对象写入<see cref="DsonArray{TName}"/>
/// </summary>
/// <typeparam name="TName"></typeparam>
public class DsonCollectionWriter<TName> : AbstractDsonWriter<TName> where TName : IEquatable<TName>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings">配置</param>
    /// <param name="outList">接收编码结果</param>
    public DsonCollectionWriter(DsonWriterSettings settings, DsonArray<TName> outList)
        : base(settings) {
        // 顶层输出是一个数组
        Context context = new Context();
        context.Init(null, DsonContextType.TopLevel, DsonTypes.Invalid);
        context.container = outList ?? throw new ArgumentNullException(nameof(outList));
        SetContext(context);
    }

    /// <summary>
    /// 获取传入的OutList
    /// </summary>
    public DsonArray<TName> OutList {
        get {
            Context context = GetContext();
            while (context!.contextType != DsonContextType.TopLevel) {
                context = context.Parent;
            }
            return context.container.AsArray<TName>();
        }
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override AbstractDsonWriter<TName>.Context NewContext() {
        return new Context();
    }

    public override void Flush() {
    }

    #region 简单值

    protected override void DoWriteInt32(int value, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonInt32(value));
    }

    protected override void DoWriteInt64(long value, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonInt64(value));
    }

    protected override void DoWriteFloat(float value, INumberStyle style) {
        GetContext().Add(new DsonFloat(value));
    }

    protected override void DoWriteDouble(double value, INumberStyle style) {
        GetContext().Add(new DsonDouble(value));
    }

    protected override void DoWriteBool(bool value) {
        GetContext().Add(DsonBool.ValueOf(value));
    }

    protected override void DoWriteString(string value, StringStyle style) {
        GetContext().Add(new DsonString(value));
    }

    protected override void DoWriteNull() {
        GetContext().Add(DsonNull.Null);
    }

    protected override void DoWriteBinary(Binary binary) {
        GetContext().Add(new DsonBinary(binary.Copy())); // 需要拷贝
    }

    protected override void DoWriteBinary(int type, DsonChunk chunk) {
        GetContext().Add(new DsonBinary(type, chunk));
    }

    protected override void DoWriteExtInt32(ExtInt32 extInt32, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonExtInt32(extInt32));
    }

    protected override void DoWriteExtInt64(ExtInt64 extInt64, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonExtInt64(extInt64));
    }

    protected override void DoWriteExtDouble(ExtDouble extDouble, INumberStyle style) {
        GetContext().Add(new DsonExtDouble(extDouble));
    }

    protected override void DoWriteExtString(ExtString extString, StringStyle style) {
        GetContext().Add(new DsonExtString(extString));
    }

    protected override void DoWriteRef(ObjectRef objectRef) {
        GetContext().Add(new DsonReference(objectRef));
    }

    protected override void DoWriteTimestamp(OffsetTimestamp timestamp) {
        GetContext().Add(new DsonTimestamp(timestamp));
    }

    #endregion

    #region 容器

    protected override void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        Context parent = GetContext();
        Context newContext = NewContext(parent, contextType, dsonType);
        switch (contextType) {
            case DsonContextType.Header: {
                newContext.container = parent.GetHeader();
                break;
            }
            case DsonContextType.Array: {
                newContext.container = new DsonArray<TName>();
                break;
            }
            case DsonContextType.Object: {
                newContext.container = new DsonObject<TName>();
                break;
            }
            default: throw new InvalidOperationException();
        }

        SetContext(newContext);
        this._recursionDepth++;
    }

    protected override void DoWriteEndContainer() {
        Context context = GetContext();
        if (context.contextType != DsonContextType.Header) {
            context.Parent!.Add(context.container);
        }

        this._recursionDepth--;
        SetContext(context.Parent!);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        throw new InvalidOperationException("Unsupported operation");
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = (Context)RentContext();
        context.Init(parent, contextType, dsonType);
        return context;
    }

    protected new class Context : AbstractDsonWriter<TName>.Context
    {
#nullable disable
        protected internal DsonValue container;
#nullable enable

        public Context() {
        }

        public DsonHeader<TName> GetHeader() {
            if (container.DsonType == DsonType.Object) {
                return container.AsObject<TName>().Header;
            } else {
                return container.AsArray<TName>().Header;
            }
        }

        public void Add(DsonValue value) {
            if (container.DsonType == DsonType.Object) {
                container.AsObject<TName>().Append(curName, value);
            } else if (container.DsonType == DsonType.Array) {
                container.AsArray<TName>().Add(value);
            } else {
                container.AsHeader<TName>().Append(curName, value);
            }
        }

        public new Context Parent => (Context)parent;
    }

    #endregion
}