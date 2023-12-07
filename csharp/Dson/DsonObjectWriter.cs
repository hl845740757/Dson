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

using Dson.IO;
using Dson.Text;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonObjectWriter<TName> : AbstractDsonWriter<TName> where TName : IEquatable<TName>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings">配置</param>
    /// <param name="outList">接收编码结果</param>
    public DsonObjectWriter(DsonWriterSettings settings, DsonArray<TName> outList)
        : base(settings) {
        if (outList == null) throw new ArgumentNullException(nameof(outList));
        // 顶层输出是一个数组
        Context context = new Context();
        context.init(null, DsonContextType.TOP_LEVEL, DsonTypes.INVALID);
        context.Container = outList;
        SetContext(context);
    }

    /// <summary>
    /// 获取传入的OutList
    /// </summary>
    public DsonArray<TName> OutList {
        get {
            Context context = GetContext();
            while (context!.contextType != DsonContextType.TOP_LEVEL) {
                context = context.Parent;
            }
            return context.Container.AsArray<TName>();
        }
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Flush() {
    }

    #region 简单值

    protected override void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonInt32(value));
    }

    protected override void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonInt64(value));
    }

    protected override void doWriteFloat(float value, INumberStyle style) {
        GetContext().Add(new DsonFloat(value));
    }

    protected override void doWriteDouble(double value, INumberStyle style) {
        GetContext().Add(new DsonDouble(value));
    }

    protected override void doWriteBool(bool value) {
        GetContext().Add(DsonBool.ValueOf(value));
    }

    protected override void doWriteString(String value, StringStyle style) {
        GetContext().Add(new DsonString(value));
    }

    protected override void doWriteNull() {
        GetContext().Add(DsonNull.Null);
    }

    protected override void doWriteBinary(DsonBinary binary) {
        GetContext().Add(binary.Copy()); // 需要拷贝
    }

    protected override void doWriteBinary(int type, DsonChunk chunk) {
        GetContext().Add(new DsonBinary(type, chunk));
    }

    protected override void doWriteExtInt32(DsonExtInt32 value, WireType wireType, INumberStyle style) {
        GetContext().Add(value); // 不可变对象
    }

    protected override void doWriteExtInt64(DsonExtInt64 value, WireType wireType, INumberStyle style) {
        GetContext().Add(value);
    }

    protected override void doWriteExtDouble(DsonExtDouble value, INumberStyle style) {
        GetContext().Add(value);
    }

    protected override void doWriteExtString(DsonExtString value, StringStyle style) {
        GetContext().Add(value);
    }

    protected override void doWriteRef(ObjectRef objectRef) {
        GetContext().Add(new DsonReference(objectRef));
    }

    protected override void doWriteTimestamp(OffsetTimestamp timestamp) {
        GetContext().Add(new DsonTimestamp(timestamp));
    }

    #endregion

    #region 容器

    protected override void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        Context parent = GetContext();
        Context newContext = NewContext(parent, contextType, dsonType);
        switch (contextType) {
            case DsonContextType.HEADER: {
                newContext.Container = parent.GetHeader();
                break;
            }
            case DsonContextType.ARRAY: {
                newContext.Container = new DsonArray<TName>();
                break;
            }
            case DsonContextType.OBJECT: {
                newContext.Container = new DsonObject<TName>();
                break;
            }
            default: throw new InvalidOperationException();
        }

        SetContext(newContext);
        this.RecursionDepth++;
    }

    protected override void doWriteEndContainer() {
        Context context = GetContext();
        if (context.contextType != DsonContextType.HEADER) {
            context.Parent!.Add(context.Container);
        }

        this.RecursionDepth--;
        SetContext(context.Parent!);
        poolContext(context);
    }

    #endregion

    #region 特殊

    protected override void doWriteMessage(int binaryType, IMessage message) {
        doWriteBinary(new DsonBinary(binaryType, message.ToByteArray()));
    }

    protected override void doWriteValueBytes(DsonType type, byte[] data) {
        throw new InvalidOperationException("Unsupported operation");
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = GetPooledContext();
        if (context != null) {
            SetPooledContext(null);
        }
        else {
            context = new Context();
        }
        context.init(parent, contextType, dsonType);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        SetPooledContext(context);
    }

    protected new class Context : AbstractDsonWriter<TName>.Context
    {
#nullable disable
        protected internal DsonValue Container;
#nullable enable

        public Context() {
        }

        public DsonHeader<TName> GetHeader() {
            if (Container.DsonType == DsonType.OBJECT) {
                return Container.AsObject<TName>().Header;
            }
            else {
                return Container.AsArray<TName>().Header;
            }
        }

        public void Add(DsonValue value) {
            if (Container.DsonType == DsonType.OBJECT) {
                Container.AsObject<TName>().Append(curName, value);
            }
            else if (Container.DsonType == DsonType.ARRAY) {
                Container.AsArray<TName>().Add(value);
            }
            else {
                Container.AsHeader<TName>().Append(curName, value);
            }
        }

        public new Context? Parent => (Context)_parent;
    }

    #endregion
}