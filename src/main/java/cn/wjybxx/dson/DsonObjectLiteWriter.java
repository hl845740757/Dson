package cn.wjybxx.dson;

import cn.wjybxx.dson.io.Chunk;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.MessageLite;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public class DsonObjectLiteWriter extends AbstractDsonLiteWriter {

    public DsonObjectLiteWriter(int recursionLimit, DsonArray<FieldNumber> outList) {
        super(recursionLimit);
        // 顶层输出是一个数组
        Context context = new Context();
        context.init(null, DsonContextType.TOP_LEVEL, null);
        context.container = Objects.requireNonNull(outList);
        setContext(context);
    }

    @Override
    public Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    @Override
    public void flush() {

    }
    //

    @Override
    protected void doWriteInt32(int value, WireType wireType) {
        getContext().add(new DsonInt32(value));
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType) {
        getContext().add(new DsonInt64(value));
    }

    @Override
    protected void doWriteFloat(float value) {
        getContext().add(new DsonFloat(value));
    }

    @Override
    protected void doWriteDouble(double value) {
        getContext().add(new DsonDouble(value));
    }

    @Override
    protected void doWriteBool(boolean value) {
        getContext().add(DsonBool.valueOf(value));
    }

    @Override
    protected void doWriteString(String value) {
        getContext().add(new DsonString(value));
    }

    @Override
    protected void doWriteNull() {
        getContext().add(DsonNull.INSTANCE);
    }

    @Override
    protected void doWriteBinary(DsonBinary binary) {
        getContext().add(binary.copy()); // 需要拷贝
    }

    @Override
    protected void doWriteBinary(int type, Chunk chunk) {
        getContext().add(new DsonBinary(type, chunk.payload()));
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 value, WireType wireType) {
        getContext().add(value); // 不可变对象
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 value, WireType wireType) {
        getContext().add(value);
    }

    @Override
    protected void doWriteExtString(DsonExtString value) {
        getContext().add(value);
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        getContext().add(new DsonObjectRef(objectRef));
    }

    @Override
    protected void doWriteTimestamp(OffsetTimestamp timestamp) {
        getContext().add(new DsonTimestamp(timestamp));
    }

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context parent = getContext();
        Context newContext = newContext(parent, contextType, dsonType);
        newContext.container = switch (contextType) {
            case HEADER -> parent.getHeader();
            case ARRAY -> new DsonArray<>();
            case OBJECT -> new DsonObject<>();
            default -> throw new AssertionError();
        };

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        Context context = getContext();
        if (context.contextType != DsonContextType.HEADER) {
            context.getParent().add(context.container);
        }

        this.recursionDepth--;
        setContext(context.parent);
        poolContext(context);
    }

    // region 特殊接口

    @Override
    protected void doWriteMessage(int binaryType, MessageLite messageLite) {
        doWriteBinary(new DsonBinary(binaryType, messageLite.toByteArray()));
    }

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        throw new UnsupportedOperationException();
    }

    // endregion

    // region context
    private Context newContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = getPooledContext();
        if (context != null) {
            setPooledContext(null);
        } else {
            context = new Context();
        }
        context.init(parent, contextType, dsonType);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        setPooledContext(context);
    }

    protected static class Context extends AbstractDsonLiteWriter.Context {

        DsonValue container;

        public Context() {
        }

        public DsonHeader<FieldNumber> getHeader() {
            if (container.getDsonType() == DsonType.OBJECT) {
                return container.asObjectLite().getHeader();
            } else {
                return container.asArrayLite().getHeader();
            }
        }

        @SuppressWarnings("unchecked")
        public void add(DsonValue value) {
            if (container.getDsonType() == DsonType.OBJECT) {
                ((DsonObject<FieldNumber>) container).put(FieldNumber.ofFullNumber(curName), value);
            } else if (container.getDsonType() == DsonType.ARRAY) {
                ((DsonArray<FieldNumber>) container).add(value);
            } else {
                ((DsonHeader<FieldNumber>) container).put(FieldNumber.ofFullNumber(curName), value);
            }
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }
    }
    // endregion
}