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
        context.init(null, DsonContextType.TOP_LEVEL, DsonTypeExt.INVALID);
        context.container = outList;
        SetContext(context);
    }

    /// <summary>
    /// 获取传入的OutList
    /// </summary>
    public DsonArray<TName> OutList {
        get {
            Context context = GetContext();
            while (context.contextType != DsonContextType.TOP_LEVEL) {
                context = context.Parent;
            }
            return context.container.AsArray<TName>();
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
        GetContext().add(new DsonInt32(value));
    }

    protected override void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        GetContext().add(new DsonInt64(value));
    }

    protected override void doWriteFloat(float value, INumberStyle style) {
        GetContext().add(new DsonFloat(value));
    }

    protected override void doWriteDouble(double value, INumberStyle style) {
        GetContext().add(new DsonDouble(value));
    }

    protected override void doWriteBool(bool value) {
        GetContext().add(DsonBool.ValueOf(value));
    }

    protected override void doWriteString(String value, StringStyle style) {
        GetContext().add(new DsonString(value));
    }

    protected override void doWriteNull() {
        GetContext().add(DsonNull.Null);
    }

    protected override void doWriteBinary(DsonBinary binary) {
        GetContext().add(binary.Copy()); // 需要拷贝
    }

    protected override void doWriteBinary(int type, DsonChunk chunk) {
        GetContext().add(new DsonBinary(type, chunk));
    }

    protected override void doWriteExtInt32(DsonExtInt32 value, WireType wireType, INumberStyle style) {
        GetContext().add(value); // 不可变对象
    }

    protected override void doWriteExtInt64(DsonExtInt64 value, WireType wireType, INumberStyle style) {
        GetContext().add(value);
    }

    protected override void doWriteExtDouble(DsonExtDouble value, INumberStyle style) {
        GetContext().add(value);
    }

    protected override void doWriteExtString(DsonExtString value, StringStyle style) {
        GetContext().add(value);
    }

    protected override void doWriteRef(ObjectRef objectRef) {
        GetContext().add(new DsonReference(objectRef));
    }

    protected override void doWriteTimestamp(OffsetTimestamp timestamp) {
        GetContext().add(new DsonTimestamp(timestamp));
    }

    #endregion

    #region 容器

    protected override void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        Context parent = GetContext();
        Context newContext = NewContext(parent, contextType, dsonType);
        switch (contextType) {
            case DsonContextType.HEADER: {
                newContext.container = parent.getHeader();
                break;
            }
            case DsonContextType.ARRAY: {
                newContext.container = new DsonArray<TName>();
                break;
            }
            case DsonContextType.OBJECT: {
                newContext.container = new DsonObject<TName>();
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
            context.Parent.add(context.container);
        }

        this.RecursionDepth--;
        SetContext(context.Parent);
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
        protected internal DsonValue container;

        public Context() {
        }

        public DsonHeader<TName> getHeader() {
            if (container.DsonType == DsonType.OBJECT) {
                return container.AsObject<TName>().Header;
            }
            else {
                return container.AsArray<TName>().Header;
            }
        }

        public void add(DsonValue value) {
            if (container.DsonType == DsonType.OBJECT) {
                container.AsObject<TName>().Append(curName, value);
            }
            else if (container.DsonType == DsonType.ARRAY) {
                container.AsArray<TName>().Add(value);
            }
            else {
                container.AsHeader<TName>().Append(curName, value);
            }
        }

        public new Context Parent => (Context)_parent;
    }

    #endregion
}