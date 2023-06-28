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

package cn.wjybxx.dson;

import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;
import java.util.Objects;

/**
 * 抽象类主要负责状态管理，子类负责具体的读取实现
 * PS：模板方法用多了也是很丑的
 *
 * @author wjybxx
 * date - 2023/4/28
 */
public abstract class AbstractDsonLiteReader implements DsonLiteReader {

    protected static final int INVALID_NAME = -1;

    protected final int recursionLimit;
    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    // 这些值放外面，不需要上下文隔离，但需要能恢复
    protected int recursionDepth;
    protected DsonType currentDsonType;
    protected WireType currentWireType;
    protected int currentName = INVALID_NAME;

    public AbstractDsonLiteReader(int recursionLimit) {
        this.recursionLimit = recursionLimit;
    }

    public Context getContext() {
        return context;
    }

    public void setContext(Context context) {
        this.context = context;
    }

    public Context getPooledContext() {
        return pooledContext;
    }

    public void setPooledContext(Context pooledContext) {
        this.pooledContext = pooledContext;
    }

    @Override
    public void close() {
        context = null;
        pooledContext = null;
    }

    // region state
    @Nonnull
    @Override
    public DsonType getCurrentDsonType() {
        if (currentDsonType == null) {
            assert context.contextType == DsonContextType.TOP_LEVEL;
            throw invalidState(List.of(DsonReaderState.NAME, DsonReaderState.VALUE));
        }
        return currentDsonType;
    }

    @Override
    public int getCurrentName() {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        return currentName;
    }

    @Override
    public DsonContextType getContextType() {
        return context.contextType;
    }

    @Override
    public boolean isAtType() {
        if (context.state == DsonReaderState.TYPE) {
            return true;
        }
        return context.contextType == DsonContextType.TOP_LEVEL
                && context.state != DsonReaderState.VALUE; // INIT or DONE
    }

    @Override
    public boolean isAtName() {
        return context.state == DsonReaderState.NAME;
    }

    @Override
    public int readName() {
        if (context.state != DsonReaderState.NAME) {
            throw invalidState(List.of(DsonReaderState.NAME));
        }
        doReadName();
        context.setState(DsonReaderState.VALUE);
        return currentName;
    }

    @Override
    public void readName(int expected) {
        int name = readName();
        if (name != expected) {
            throw DsonIOException.unexpectedName(expected, name);
        }
    }

    protected abstract void doReadName();

    /** 检查是否可以执行{@link #readDsonType()} */
    protected void checkReadDsonTypeState(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            if (context.state != DsonReaderState.INITIAL && context.state != DsonReaderState.TYPE) {
                throw invalidState(List.of(DsonReaderState.INITIAL, DsonReaderState.TYPE));
            }
        } else if (context.state != DsonReaderState.TYPE) {
            throw invalidState(List.of(DsonReaderState.TYPE));
        }
    }

    protected void onReadDsonType(Context context, DsonType dsonType) {
        if (dsonType == DsonType.END_OF_OBJECT) {
            // readEndXXX都是子上下文中执行的，因此正常情况下topLevel不会读取到 endOfObject 标记
            // 顶层读取到 END_OF_OBJECT 表示到达文件尾
            if (context.contextType == DsonContextType.TOP_LEVEL) {
                context.setState(DsonReaderState.END_OF_FILE);
            } else {
                context.setState(DsonReaderState.WAIT_END_OBJECT);
            }
        } else {
            // topLevel只可是容器对象
            if (context.contextType == DsonContextType.TOP_LEVEL && !dsonType.isContainerOrHeader()) {
                throw DsonIOException.invalidDsonType(context.contextType, dsonType);
            }
            if (context.contextType == DsonContextType.OBJECT) {
                // 如果是header则直接进入VALUE状态 - header是匿名属性
                if (dsonType == DsonType.HEADER) {
                    context.setState(DsonReaderState.VALUE);
                } else {
                    context.setState(DsonReaderState.NAME);
                }
            } else if (context.contextType == DsonContextType.HEADER) {
                context.setState(DsonReaderState.NAME);
            } else {
                context.setState(DsonReaderState.VALUE);
            }
        }
    }

    /** 前进到读值状态 */
    protected void advanceToValueState(int name, @Nullable DsonType requiredType) {
        Context context = this.context;
        if (context.state == DsonReaderState.TYPE) {
            readDsonType();
        }
        if (context.state == DsonReaderState.NAME) {
            readName(name);
        }
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        if (requiredType != null && currentDsonType != requiredType) {
            throw DsonIOException.dsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    protected void ensureValueState(Context context, DsonType requiredType) {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        if (currentDsonType != requiredType) {
            throw DsonIOException.dsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    protected void setNextState() {
        context.setState(DsonReaderState.TYPE);
    }

    protected DsonIOException invalidState(List<DsonReaderState> expected) {
        return DsonIOException.invalidState(context.contextType, expected, context.state);
    }
    // endregion

    // region 简单值
    @Override
    public int readInt32(int name) {
        advanceToValueState(name, DsonType.INT32);
        int value = doReadInt32();
        setNextState();
        return value;
    }

    @Override
    public long readInt64(int name) {
        advanceToValueState(name, DsonType.INT64);
        long value = doReadInt64();
        setNextState();
        return value;
    }

    @Override
    public float readFloat(int name) {
        advanceToValueState(name, DsonType.FLOAT);
        float value = doReadFloat();
        setNextState();
        return value;
    }

    @Override
    public double readDouble(int name) {
        advanceToValueState(name, DsonType.DOUBLE);
        double value = doReadDouble();
        setNextState();
        return value;
    }

    @Override
    public boolean readBoolean(int name) {
        advanceToValueState(name, DsonType.BOOLEAN);
        boolean value = doReadBool();
        setNextState();
        return value;
    }

    @Override
    public String readString(int name) {
        advanceToValueState(name, DsonType.STRING);
        String value = doReadString();
        setNextState();
        return value;
    }

    @Override
    public void readNull(int name) {
        advanceToValueState(name, DsonType.NULL);
        doReadNull();
        setNextState();
    }

    @Override
    public DsonBinary readBinary(int name) {
        advanceToValueState(name, DsonType.BINARY);
        DsonBinary value = doReadBinary();
        setNextState();
        return value;
    }

    @Override
    public DsonExtInt32 readExtInt32(int name) {
        advanceToValueState(name, DsonType.EXT_INT32);
        DsonExtInt32 value = doReadExtInt32();
        setNextState();
        return value;
    }

    @Override
    public DsonExtInt64 readExtInt64(int name) {
        advanceToValueState(name, DsonType.EXT_INT64);
        DsonExtInt64 value = doReadExtInt64();
        setNextState();
        return value;
    }

    @Override
    public DsonExtString readExtString(int name) {
        advanceToValueState(name, DsonType.EXT_STRING);
        DsonExtString value = doReadExtString();
        setNextState();
        return value;
    }

    @Override
    public ObjectRef readRef(int name) {
        advanceToValueState(name, DsonType.REFERENCE);
        ObjectRef value = doReadRef();
        setNextState();
        return value;
    }

    @Override
    public OffsetTimestamp readTimestamp(int name) {
        advanceToValueState(name, DsonType.TIMESTAMP);
        OffsetTimestamp value = doReadTimestamp();
        setNextState();
        return value;
    }

    protected abstract int doReadInt32();

    protected abstract long doReadInt64();

    protected abstract float doReadFloat();

    protected abstract double doReadDouble();

    protected abstract boolean doReadBool();

    protected abstract String doReadString();

    protected abstract void doReadNull();

    protected abstract DsonBinary doReadBinary();

    protected abstract DsonExtInt32 doReadExtInt32();

    protected abstract DsonExtInt64 doReadExtInt64();

    protected abstract DsonExtString doReadExtString();

    protected abstract ObjectRef doReadRef();

    protected abstract OffsetTimestamp doReadTimestamp();

    // region 容器

    @Override
    public void readStartArray() {
        readStartContainer(DsonContextType.ARRAY, DsonType.ARRAY);
    }

    @Override
    public void readEndArray() {
        readEndContainer(DsonContextType.ARRAY);
    }

    @Override
    public void readStartObject() {
        readStartContainer(DsonContextType.OBJECT, DsonType.OBJECT);
    }

    @Override
    public void readEndObject() {
        readEndContainer(DsonContextType.OBJECT);
    }

    @Override
    public void readStartHeader() {
        readStartContainer(DsonContextType.HEADER, DsonType.HEADER);
    }

    @Override
    public void readEndHeader() {
        readEndContainer(DsonContextType.HEADER);
    }

    @Override
    public void backToWaitStart() {
        Context context = this.context;
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonIOException.contextErrorTopLevel();
        }
        if (context.state != DsonReaderState.TYPE) {
            throw invalidState(List.of(DsonReaderState.TYPE));
        }
        context.setState(DsonReaderState.WAIT_START_OBJECT);
    }

    private void readStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context context = this.context;
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            setNextState();
            return;
        }
        if (recursionDepth >= recursionLimit) {
            throw DsonIOException.recursionLimitExceeded();
        }
        autoStartTopLevel(context);
        ensureValueState(context, dsonType);
        doReadStartContainer(contextType, dsonType);
        setNextState(); // 设置新上下文状态
    }

    private void readEndContainer(DsonContextType contextType) {
        Context context = this.context;
        checkEndContext(context, contextType);
        doReadEndContainer();
        setNextState(); // parent前进一个状态
    }

    private void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && (context.state == DsonReaderState.INITIAL || context.state == DsonReaderState.TYPE)) {
            readDsonType();
        }
    }

    private void checkEndContext(Context context, DsonContextType contextType) {
        if (context.contextType != contextType) {
            throw DsonIOException.contextError(DsonContextType.OBJECT, context.contextType);
        }
        if (context.state != DsonReaderState.WAIT_END_OBJECT) {
            throw invalidState(List.of(DsonReaderState.WAIT_END_OBJECT));
        }
    }

    protected void recoverDsonType(Context context) {
        this.currentDsonType = Objects.requireNonNull(context.dsonType);
        this.currentWireType = WireType.VARINT;
        this.currentName = context.name;
    }

    /**
     * 创建新的context，保存信息，压入上下文
     */
    protected abstract void doReadStartContainer(DsonContextType contextType, DsonType dsonType);

    /**
     * 恢复到旧的上下文，恢复{@link #currentDsonType}，弹出上下文
     */
    protected abstract void doReadEndContainer();

    // endregion

    // region 特殊接口

    @Override
    public void skipName() {
        Context context = getContext();
        if (context.state == DsonReaderState.VALUE) {
            return;
        }
        if (context.state != DsonReaderState.NAME) {
            throw invalidState(List.of(DsonReaderState.VALUE, DsonReaderState.NAME));
        }
        doSkipName();
        currentName = INVALID_NAME;
        context.setState(DsonReaderState.VALUE);
    }

    @Override
    public void skipValue() {
        if (context.state != DsonReaderState.VALUE) {
            throw invalidState(List.of(DsonReaderState.VALUE));
        }
        doSkipValue();
        setNextState();
    }

    @Override
    public void skipToEndOfObject() {
        Context context = getContext();
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonIOException.contextErrorTopLevel();
        }
        if (context.state == DsonReaderState.WAIT_START_OBJECT) {
            throw invalidState(List.of(DsonReaderState.TYPE, DsonReaderState.NAME, DsonReaderState.VALUE));
        }
        if (currentDsonType == DsonType.END_OF_OBJECT) {
            assert context.state == DsonReaderState.WAIT_END_OBJECT;
            return;
        }
        doSkipToEndOfObject();
        setNextState();
        readDsonType(); // end of object
        assert currentDsonType == DsonType.END_OF_OBJECT;
    }

    @Override
    public <T> T readMessage(int name, int binaryType, @Nonnull Parser<T> parser) {
        Objects.requireNonNull(parser, "parser");
        advanceToValueState(name, DsonType.BINARY);
        T value = doReadMessage(binaryType, parser);
        setNextState();
        return value;
    }

    @Override
    public byte[] readValueAsBytes(int name) {
        advanceToValueState(name, null);
        DsonReaderUtils.checkReadValueAsBytes(currentDsonType);
        byte[] data = doReadValueAsBytes();
        setNextState();
        return data;
    }

    @Override
    public DsonReaderGuide whatShouldIDo() {
        return DsonReaderUtils.whatShouldIDo(context.contextType, context.state);
    }

    protected abstract void doSkipName();

    protected abstract void doSkipValue();

    protected abstract void doSkipToEndOfObject();

    protected abstract <T> T doReadMessage(int binaryType, Parser<T> parser);

    protected abstract byte[] doReadValueAsBytes();

    // endregion

    // region context

    protected static class Context {

        public Context parent;
        public DsonContextType contextType;
        public DsonType dsonType; // 用于在Object/Array模式下写入内置数据结构
        public DsonReaderState state = DsonReaderState.INITIAL;
        public int name = INVALID_NAME;

        public Context() {
        }

        public Context init(Context parent, DsonContextType contextType, DsonType dsonType) {
            this.parent = parent;
            this.contextType = contextType;
            this.dsonType = dsonType;
            return this;
        }

        public void reset() {
            parent = null;
            contextType = null;
            dsonType = null;
            state = DsonReaderState.INITIAL;
            name = INVALID_NAME;
        }

        /** 方便查看赋值的调用 */
        public void setState(DsonReaderState state) {
            this.state = state;
        }

        public Context getParent() {
            return parent;
        }
    }

}