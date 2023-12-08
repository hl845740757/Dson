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

import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.MessageLite;

import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/28
 */
public abstract class AbstractDsonWriter implements DsonWriter {

    protected final DsonWriterSettings settings;
    private Context context;
    private Context pooledContext; // 一个额外的缓存，用于写集合等减少上下文创建

    protected int recursionDepth;

    public AbstractDsonWriter(DsonWriterSettings settings) {
        this.settings = settings;
    }

    public DsonWriterSettings getSettings() {
        return settings;
    }

    protected Context getContext() {
        return context;
    }

    protected void setContext(Context context) {
        this.context = context;
    }

    protected Context getPooledContext() {
        return pooledContext;
    }

    protected void setPooledContext(Context pooledContext) {
        this.pooledContext = pooledContext;
    }

    @Override
    public void close() {
        context = null;
        pooledContext = null;
    }

    // region state

    @Override
    public DsonContextType getContextType() {
        return context.contextType;
    }

    @Override
    public boolean isAtName() {
        return context.state == DsonWriterState.NAME;
    }

    @Override
    public void writeName(String name) {
        Objects.requireNonNull(name, "name");
        Context context = this.context;
        if (context.state != DsonWriterState.NAME) {
            throw invalidState(List.of(DsonWriterState.NAME), context.state);
        }
        context.curName = name;
        context.state = DsonWriterState.VALUE;
        doWriteName(name);
    }

    /** 执行{@link #writeName(String)}时调用 */
    protected void doWriteName(String name) {

    }

    protected void advanceToValueState(String name) {
        Context context = this.context;
        if (context.state == DsonWriterState.NAME) {
            writeName(name);
        }
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    protected void ensureValueState(Context context) {
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    protected void setNextState() {
        switch (context.contextType) {
            case OBJECT, HEADER -> context.setState(DsonWriterState.NAME);
            case TOP_LEVEL, ARRAY -> context.setState(DsonWriterState.VALUE);
        }
    }

    private DsonIOException invalidState(List<DsonWriterState> expected, DsonWriterState state) {
        return DsonIOException.invalidState(context.contextType, expected, state);
    }
    // endregion

    // region 简单值
    @Override
    public void writeInt32(String name, int value, WireType wireType, INumberStyle style) {
        advanceToValueState(name);
        doWriteInt32(value, wireType, style);
        setNextState();
    }

    @Override
    public void writeInt64(String name, long value, WireType wireType, INumberStyle style) {
        advanceToValueState(name);
        doWriteInt64(value, wireType, style);
        setNextState();
    }

    @Override
    public void writeFloat(String name, float value, INumberStyle style) {
        advanceToValueState(name);
        doWriteFloat(value, style);
        setNextState();
    }

    @Override
    public void writeDouble(String name, double value, INumberStyle style) {
        advanceToValueState(name);
        doWriteDouble(value, style);
        setNextState();
    }

    @Override
    public void writeBoolean(String name, boolean value) {
        advanceToValueState(name);
        doWriteBool(value);
        setNextState();
    }

    @Override
    public void writeString(String name, String value, StringStyle style) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteString(value, style);
        setNextState();
    }

    @Override
    public void writeNull(String name) {
        advanceToValueState(name);
        doWriteNull();
        setNextState();
    }

    @Override
    public void writeBinary(String name, DsonBinary dsonBinary) {
        Objects.requireNonNull(dsonBinary);
        advanceToValueState(name);
        doWriteBinary(dsonBinary);
        setNextState();
    }

    @Override
    public void writeBinary(String name, int type, DsonChunk chunk) {
        Objects.requireNonNull(chunk);
        Dsons.checkSubType(type);
        Dsons.checkBinaryLength(chunk.getLength());
        advanceToValueState(name);
        doWriteBinary(type, chunk);
        setNextState();
    }

    @Override
    public void writeExtInt32(String name, DsonExtInt32 value, WireType wireType, INumberStyle style) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteExtInt32(value, wireType, style);
        setNextState();
    }

    @Override
    public void writeExtInt64(String name, DsonExtInt64 value, WireType wireType, INumberStyle style) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteExtInt64(value, wireType, style);
        setNextState();
    }

    @Override
    public void writeExtDouble(String name, DsonExtDouble value, INumberStyle style) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteExtDouble(value, style);
        setNextState();
    }

    @Override
    public void writeExtString(String name, DsonExtString value, StringStyle style) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteExtString(value, style);
        setNextState();
    }

    @Override
    public void writeRef(String name, ObjectRef objectRef) {
        Objects.requireNonNull(objectRef);
        advanceToValueState(name);
        doWriteRef(objectRef);
        setNextState();
    }

    @Override
    public void writeTimestamp(String name, OffsetTimestamp timestamp) {
        Objects.requireNonNull(timestamp);
        advanceToValueState(name);
        doWriteTimestamp(timestamp);
        setNextState();
    }

    protected abstract void doWriteInt32(int value, WireType wireType, INumberStyle style);

    protected abstract void doWriteInt64(long value, WireType wireType, INumberStyle style);

    protected abstract void doWriteFloat(float value, INumberStyle style);

    protected abstract void doWriteDouble(double value, INumberStyle style);

    protected abstract void doWriteBool(boolean value);

    protected abstract void doWriteString(String value, StringStyle style);

    protected abstract void doWriteNull();

    protected abstract void doWriteBinary(DsonBinary binary);

    protected abstract void doWriteBinary(int type, DsonChunk chunk);

    protected abstract void doWriteExtInt32(DsonExtInt32 extInt32, WireType wireType, INumberStyle style);

    protected abstract void doWriteExtInt64(DsonExtInt64 extInt64, WireType wireType, INumberStyle style);

    protected abstract void doWriteExtDouble(DsonExtDouble extDouble, INumberStyle style);

    protected abstract void doWriteExtString(DsonExtString extString, StringStyle style);

    protected abstract void doWriteRef(ObjectRef objectRef);

    protected abstract void doWriteTimestamp(OffsetTimestamp timestamp);

    // endregion

    // region 容器
    @Override
    public void writeStartArray(ObjectStyle style) {
        writeStartContainer(DsonContextType.ARRAY, DsonType.ARRAY, style);
    }

    @Override
    public void writeEndArray() {
        writeEndContainer(DsonContextType.ARRAY, DsonWriterState.VALUE);
    }

    @Override
    public void writeStartObject(ObjectStyle style) {
        writeStartContainer(DsonContextType.OBJECT, DsonType.OBJECT, style);
    }

    @Override
    public void writeEndObject() {
        writeEndContainer(DsonContextType.OBJECT, DsonWriterState.NAME);
    }

    @Override
    public void writeStartHeader(ObjectStyle style) {
        // object下默认是name状态
        Context context = this.context;
        if (context.contextType == DsonContextType.OBJECT && context.state == DsonWriterState.NAME) {
            context.setState(DsonWriterState.VALUE);
        }
        writeStartContainer(DsonContextType.HEADER, DsonType.HEADER, style);
    }

    @Override
    public void writeEndHeader() {
        writeEndContainer(DsonContextType.HEADER, DsonWriterState.NAME);
    }

    private void writeStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        Objects.requireNonNull(style);
        if (recursionDepth >= settings.recursionLimit) {
            throw DsonIOException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(contextType, dsonType, style);
        setNextState(); // 设置新上下文状态
    }

    private void writeEndContainer(DsonContextType contextType, DsonWriterState expectedState) {
        Context context = this.context;
        checkEndContext(context, contextType, expectedState);
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    protected void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && context.state == DsonWriterState.INITIAL) {
            context.setState(DsonWriterState.VALUE);
        }
    }

    protected void checkEndContext(Context context, DsonContextType contextType, DsonWriterState state) {
        if (context.contextType != contextType) {
            throw DsonIOException.contextError(contextType, context.contextType);
        }
        if (context.state != state) {
            throw invalidState(List.of(state), context.state);
        }
    }

    /** 写入类型信息，创建新上下文，压入上下文 */
    protected abstract void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style);

    /** 弹出上下文 */
    protected abstract void doWriteEndContainer();

    // endregion
    // region sp

    @Override
    public void writeMessage(String name, int binaryType, MessageLite messageLite) {
        Dsons.checkSubType(binaryType);
        advanceToValueState(name);
        doWriteMessage(binaryType, messageLite);
        setNextState();
    }

    @Override
    public void writeValueBytes(String name, DsonType type, byte[] data) {
        DsonReaderUtils.checkWriteValueAsBytes(type);
        advanceToValueState(name);
        doWriteValueBytes(type, data);
        setNextState();
    }

    @Override
    public Object attach(Object userData) {
        return context.attach(userData);
    }

    @Override
    public Object attachment() {
        return context.userData;
    }

    protected abstract void doWriteMessage(int binaryType, MessageLite messageLite);

    protected abstract void doWriteValueBytes(DsonType type, byte[] data);

    // endregion

    // region context

    protected static class Context {

        public Context parent;
        public DsonContextType contextType;
        public DsonType dsonType; // 用于在Object/Array模式下写入内置数据结构
        public DsonWriterState state = DsonWriterState.INITIAL;
        public String curName;
        public Object userData;

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
            state = DsonWriterState.INITIAL;
            curName = null;
            userData = null;
        }

        public Object attach(Object userData) {
            Object r = this.userData;
            this.userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void setState(DsonWriterState state) {
            this.state = state;
        }

        public Context getParent() {
            return parent;
        }

    }

}