using Dson.IO;
using Dson.Text;
using Dson.Types;

namespace Dson;

public class DsonBinaryWriter<TName> : AbstractDsonWriter<TName>
{
    private IDsonOutput output;

    public DsonBinaryWriter(DsonWriterSettings settings, IDsonOutput output) : base(settings) {
        this.output = output;
    }

    protected override Context getContext() {
        return (Context)base.getContext();
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Flush() {
        output?.Flush();
    }

    public override void Dispose() {
        if (settings.autoClose) {
            output?.Dispose();
            output = null!;
        }
        base.Dispose();
    }

    #region state

    private void writeFullTypeAndCurrentName(IDsonOutput output, DsonType dsonType, int wireType) {
        output.WriteRawByte((byte)Dsons.MakeFullType((int)dsonType, wireType));
        if (dsonType != DsonType.HEADER) { // header是匿名属性
            Context context = getContext();
            if (context.contextType == DsonContextType.OBJECT ||
                context.contextType == DsonContextType.HEADER) {
                if (isStringKey) {
                    output.WriteString(context.StringName);
                }
                else {
                    output.WriteUint32(context.IntName);
                }
            }
        }
    }

    #endregion

    #region 简单值

    protected override void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT32, (int)wireType);
        DsonReaderUtils.writeInt32(output, value, wireType);
    }

    protected override void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT64, (int)wireType);
        DsonReaderUtils.writeInt64(output, value, wireType);
    }

    protected override void doWriteFloat(float value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfFloat(value);
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.FLOAT, wireType);
        DsonReaderUtils.writeFloat(output, value, wireType);
    }

    protected override void doWriteDouble(double value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfDouble(value);
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.DOUBLE, wireType);
        DsonReaderUtils.writeDouble(output, value, wireType);
    }

    protected override void doWriteBool(bool value) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BOOLEAN, value ? 1 : 0);
    }

    protected override void doWriteString(String value, StringStyle style) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.STRING, 0);
        output.WriteString(value);
    }

    protected override void doWriteNull() {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.NULL, 0);
    }

    protected override void doWriteBinary(DsonBinary binary) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, binary);
    }

    protected override void doWriteBinary(int type, DsonChunk chunk) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, type, chunk);
    }

    protected override void doWriteExtInt32(DsonExtInt32 value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT32, (int)wireType);
        DsonReaderUtils.writeExtInt32(output, value, wireType);
    }

    protected override void doWriteExtInt64(DsonExtInt64 value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT64, (int)wireType);
        DsonReaderUtils.writeExtInt64(output, value, wireType);
    }

    protected override void doWriteExtDouble(DsonExtDouble value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfDouble(value.Value);
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_DOUBLE, wireType);
        DsonReaderUtils.writeExtDouble(output, value, wireType);
    }

    protected override void doWriteExtString(DsonExtString value, StringStyle style) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_STRING, DsonReaderUtils.wireTypeOfExtString(value));
        DsonReaderUtils.writeExtString(output, value);
    }

    protected override void doWriteRef(ObjectRef objectRef) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.REFERENCE, DsonReaderUtils.wireTypeOfRef(objectRef));
        DsonReaderUtils.writeRef(output, objectRef);
    }

    protected override void doWriteTimestamp(OffsetTimestamp timestamp) {
        IDsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.TIMESTAMP, 0);
        DsonReaderUtils.writeTimestamp(output, timestamp);
    }

    #endregion


    #region context

    private Context newContext(Context parent, DsonContextType contextType, DsonType dsonType) {
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
        int preWritten = 0;

        public Context() {
        }

        public void reset() {
            base.reset();
            preWritten = 0;
        }
    }

    #endregion
}