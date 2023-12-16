package cn.wjybxx.dson;

import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public class DsonObjectLiteWriter extends AbstractDsonLiteWriter {

    public DsonObjectLiteWriter(DsonWriterSettings settings, DsonArray<FieldNumber> outList) {
        super(settings);
        // 顶层输出是一个数组
        Context context = new Context();
        context.init(null, DsonContextType.TOP_LEVEL, null);
        context.container = Objects.requireNonNull(outList);
        setContext(context);
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    protected Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    @Override
    public void flush() {

    }

    public DsonArray<FieldNumber> getOutList() {
        Context context = getContext();
        while (context.contextType != DsonContextType.TOP_LEVEL) {
            context = context.getParent();
        }
        return context.container.asArrayLite();
    }
    // region 简单值

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
        getContext().add(DsonNull.NULL);
    }

    @Override
    protected void doWriteBinary(DsonBinary binary) {
        getContext().add(binary.copy()); // 需要拷贝
    }

    @Override
    protected void doWriteBinary(int type, DsonChunk chunk) {
        getContext().add(new DsonBinary(type, chunk));
    }

    @Override
    protected void doWriteExtInt32(DsonExtInt32 extInt32, WireType wireType) {
        getContext().add(extInt32); // 不可变对象
    }

    @Override
    protected void doWriteExtInt64(DsonExtInt64 extInt64, WireType wireType) {
        getContext().add(extInt64);
    }

    @Override
    protected void doWriteExtDouble(DsonExtDouble extDouble) {
        getContext().add(extDouble);
    }

    @Override
    protected void doWriteExtString(DsonExtString extString) {
        getContext().add(extString);
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        getContext().add(new DsonReference(objectRef));
    }

    @Override
    protected void doWriteTimestamp(OffsetTimestamp timestamp) {
        getContext().add(new DsonTimestamp(timestamp));
    }

    //endregion

    //region 容器

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

    // endregion

    // region 特殊接口

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