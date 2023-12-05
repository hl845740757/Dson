using Dson.IO;
using Dson.Types;
using Google.Protobuf;

namespace Dson;

public class DsonBinaryReader<TName> : AbstractDsonReader<TName>
{
    private IDsonInput input;

    public DsonBinaryReader(DsonReaderSettings settings, IDsonInput input) : base(settings) {
        this.input = input;
    }

    protected override Context GetContext() {
        return (Context)base.GetContext();
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Dispose() {
        if (Settings.autoClose) {
            input?.Dispose();
            input = null!;
        }
        base.Dispose();
    }

    #region state

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        int fullType = input.IsAtEnd() ? 0 : BinaryUtils.ToUint(input.ReadRawByte());
        int wreTypeBits = Dsons.WireTypeOfFullType(fullType);
        DsonType dsonType = DsonTypeExt.ForNumber(Dsons.DsonTypeOfFullType(fullType));
        WireType wireType = dsonType.HasWireType() ? WireTypeExt.ForNumber(wreTypeBits) : WireType.VarInt;
        this.currentDsonType = dsonType;
        this.currentWireType = wireType;
        this.currentWireTypeBits = wreTypeBits;
        this.currentName = default;

        onReadDsonType(context, dsonType);
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        int fullType = input.IsAtEnd() ? 0 : BinaryUtils.ToUint(input.GetByte(input.Position));
        return DsonTypeExt.ForNumber(Dsons.DsonTypeOfFullType(fullType));
    }

    protected override void doReadName() {
        if (IsStringKey) {
            string filedName = input.ReadString();
            if (Settings.enableFieldIntern) {
                filedName = Dsons.InternField(filedName);
            }
            DsonBinaryReader<string> textReader = (DsonBinaryReader<string>)(object)this;
            textReader.currentName = filedName;
        }
        else {
            int fieldName = input.ReadUint32();
            DsonBinaryReader<int> binReader = (DsonBinaryReader<int>)(object)this;
            binReader.currentName = fieldName;
        }
    }

    #endregion

    #region 简单值

    protected override int doReadInt32() {
        return DsonReaderUtils.readInt32(input, currentWireType);
    }

    protected override long doReadInt64() {
        return DsonReaderUtils.readInt64(input, currentWireType);
    }

    protected override float doReadFloat() {
        return DsonReaderUtils.readFloat(input, currentWireTypeBits);
    }

    protected override double doReadDouble() {
        return DsonReaderUtils.readDouble(input, currentWireTypeBits);
    }

    protected override bool doReadBool() {
        return DsonReaderUtils.readBool(input, currentWireTypeBits);
    }

    protected override String doReadString() {
        return input.ReadString();
    }

    protected override void doReadNull() {
    }

    protected override DsonBinary doReadBinary() {
        return DsonReaderUtils.readDsonBinary(input);
    }

    protected override DsonExtInt32 doReadExtInt32() {
        return DsonReaderUtils.readDsonExtInt32(input, currentWireType);
    }

    protected override DsonExtInt64 doReadExtInt64() {
        return DsonReaderUtils.readDsonExtInt64(input, currentWireType);
    }

    protected override DsonExtDouble doReadExtDouble() {
        return DsonReaderUtils.readDsonExtDouble(input, currentWireTypeBits);
    }

    protected override DsonExtString doReadExtString() {
        return DsonReaderUtils.readDsonExtString(input, currentWireTypeBits);
    }

    protected override ObjectRef doReadRef() {
        return DsonReaderUtils.readRef(input, currentWireTypeBits);
    }

    protected override OffsetTimestamp doReadTimestamp() {
        return DsonReaderUtils.readTimestamp(input);
    }

    #endregion

    #region 容器

    protected override void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        int length = input.ReadFixed32();
        newContext.oldLimit = input.PushLimit(length);
        newContext.name = currentName;

        this.recursionDepth++;
        SetContext(newContext);
    }

    protected override void doReadEndContainer() {
        if (!input.IsAtEnd()) {
            throw DsonIOException.bytesRemain(input.GetBytesUntilLimit());
        }
        Context context = GetContext();
        input.PopLimit(context.oldLimit);

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        SetContext(context.parent!);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void doSkipName() {
        if (IsStringKey) {
            // 避免构建字符串
            int size = input.ReadUint32();
            if (size > 0) {
                input.SkipRawBytes(size);
            }
        }
        else {
            input.ReadUint32();
        }
    }

    protected override void doSkipValue() {
        DsonReaderUtils.skipValue(input, ContextType, currentDsonType, currentWireType, currentWireTypeBits);
    }

    protected override void doSkipToEndOfObject() {
        DsonReaderUtils.skipToEndOfObject(input);
    }

    protected override T doReadMessage<T>(int binaryType, MessageParser<T> parser) {
        return DsonReaderUtils.readMessage(input, binaryType, parser);
    }

    protected override byte[] doReadValueAsBytes() {
        return DsonReaderUtils.readValueAsBytes(input, currentDsonType);
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

    private void PoolContext(Context context) {
        context.reset();
        SetPooledContext(context);
    }

    protected class Context : AbstractDsonReader<TName>.Context
    {
        protected internal int oldLimit = -1;

        public Context() {
        }

        public void reset() {
            base.reset();
            oldLimit = -1;
        }
    }

    #endregion
}