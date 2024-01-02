/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.*;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonBinaryWriter extends AbstractDsonWriter {

    private DsonOutput output;

    public DsonBinaryWriter(DsonWriterSettings settings, DsonOutput output) {
        super(settings);
        this.output = Objects.requireNonNull(output);
        setContext(new Context().init(null, DsonContextType.TOP_LEVEL, null));
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    protected Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    //
    @Override
    public void flush() {
        output.flush();
    }

    @Override
    public void close() {
        if (settings.autoClose && output != null) {
            output.close();
            output = null;
        }
        super.close();
    }

    // region state

    private void writeFullTypeAndCurrentName(DsonOutput output, DsonType dsonType, int wireType) {
        output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), wireType));
        if (dsonType != DsonType.HEADER) { // header是匿名属性
            Context context = getContext();
            if (context.contextType == DsonContextType.OBJECT ||
                    context.contextType == DsonContextType.HEADER) {
                output.writeString(context.curName);
            }
        }
    }

    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT32, wireType.getNumber());
        wireType.writeInt32(output, value);
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT64, wireType.getNumber());
        wireType.writeInt64(output, value);
    }

    @Override
    protected void doWriteFloat(float value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfFloat(value);
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.FLOAT, wireType);
        DsonReaderUtils.writeFloat(output, value, wireType);
    }

    @Override
    protected void doWriteDouble(double value, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfDouble(value);
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.DOUBLE, wireType);
        DsonReaderUtils.writeDouble(output, value, wireType);
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BOOLEAN, value ? 1 : 0);
    }

    @Override
    protected void doWriteString(String value, StringStyle style) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.STRING, 0);
        output.writeString(value);
    }

    @Override
    protected void doWriteNull() {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.NULL, 0);
    }

    @Override
    protected void doWriteBinary(Binary binary) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, binary);
    }

    @Override
    protected void doWriteBinary(int type, DsonChunk chunk) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, type, chunk);
    }

    @Override
    protected void doWriteExtInt32(ExtInt32 extInt32, WireType wireType, INumberStyle style) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT32, wireType.getNumber());
        DsonReaderUtils.writeExtInt32(output, extInt32, wireType);
    }

    @Override
    protected void doWriteExtInt64(ExtInt64 extInt64, WireType wireType, INumberStyle style) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_INT64, wireType.getNumber());
        DsonReaderUtils.writeExtInt64(output, extInt64, wireType);
    }

    @Override
    protected void doWriteExtDouble(ExtDouble extDouble, INumberStyle style) {
        int wireType = DsonReaderUtils.wireTypeOfDouble(extDouble.getValue());
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_DOUBLE, wireType);
        DsonReaderUtils.writeExtDouble(output, extDouble, wireType);
    }

    @Override
    protected void doWriteExtString(ExtString extString, StringStyle style) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.EXT_STRING, DsonReaderUtils.wireTypeOfExtString(extString));
        DsonReaderUtils.writeExtString(output, extString);
    }

    @Override
    protected void doWriteRef(ObjectRef objectRef) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.REFERENCE, DsonReaderUtils.wireTypeOfRef(objectRef));
        DsonReaderUtils.writeRef(output, objectRef);
    }

    @Override
    protected void doWriteTimestamp(OffsetTimestamp timestamp) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.TIMESTAMP, 0);
        DsonReaderUtils.writeTimestamp(output, timestamp);
    }
    // endregion

    // region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, dsonType, 0);

        Context newContext = newContext(getContext(), contextType, dsonType);
        newContext.preWritten = output.getPosition();
        output.writeFixed32(0);

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        // 记录preWritten在写length之前，最后的size要减4
        Context context = getContext();
        int preWritten = context.preWritten;
        output.setFixedInt32(preWritten, output.getPosition() - preWritten - 4);

        this.recursionDepth--;
        setContext(context.parent);
        poolContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, type, 0);
        DsonReaderUtils.writeValueBytes(output, type, data);
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

    protected static class Context extends AbstractDsonWriter.Context {

        int preWritten = 0;

        public Context() {
        }

        public void reset() {
            super.reset();
            preWritten = 0;
        }
    }

    // endregion

}