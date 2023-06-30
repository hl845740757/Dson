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

import cn.wjybxx.dson.internal.BinaryUtils;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;

/**
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonBinaryLiteReader extends AbstractDsonLiteReader {

    private DsonInput input;

    public DsonBinaryLiteReader(int recursionLimit, DsonInput input) {
        super(recursionLimit);
        this.input = input;
        setContext(new Context().init(null, DsonContextType.TOP_LEVEL, null));
    }

    @Override
    public void close() {
        if (input != null) {
            input.close();
            input = null;
        }
        super.close();
    }

    @Override
    public Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    // region state

    @Override
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        final int fullType = input.isAtEnd() ? 0 : BinaryUtils.toUint8(input.readRawByte());
        DsonType dsonType = DsonType.forNumber(Dsons.dsonTypeOfFullType(fullType));
        WireType wireType = WireType.forNumber(Dsons.wireTypeOfFullType(fullType));
        this.currentDsonType = dsonType;
        this.currentWireType = wireType;
        this.currentName = INVALID_NAME;

        onReadDsonType(context, dsonType);
        return dsonType;
    }

    @Override
    protected void doReadName() {
        currentName = input.readUint32();
    }

    // endregion

    // region 简单值

    @Override
    protected int doReadInt32() {
        return currentWireType.readInt32(input);
    }

    @Override
    protected long doReadInt64() {
        return currentWireType.readInt64(input);
    }

    @Override
    protected float doReadFloat() {
        return input.readFloat();
    }

    @Override
    protected double doReadDouble() {
        return input.readDouble();
    }

    @Override
    protected boolean doReadBool() {
        return input.readBool();
    }

    @Override
    protected String doReadString() {
        return input.readString();
    }

    @Override
    protected void doReadNull() {

    }

    @Override
    protected DsonBinary doReadBinary() {
        return DsonReaderUtils.readDsonBinary(input);
    }

    @Override
    protected DsonExtString doReadExtString() {
        return DsonReaderUtils.readDsonExtString(input);
    }

    @Override
    protected DsonExtInt32 doReadExtInt32() {
        return DsonReaderUtils.readDsonExtInt32(input, currentWireType);
    }

    @Override
    protected DsonExtInt64 doReadExtInt64() {
        return DsonReaderUtils.readDsonExtInt64(input, currentWireType);
    }

    @Override
    protected ObjectRef doReadRef() {
        return DsonReaderUtils.readRef(input);
    }

    @Override
    protected OffsetTimestamp doReadTimestamp() {
        return DsonReaderUtils.readTimestamp(input);
    }
    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = newContext(getContext(), contextType, dsonType);
        int length = input.readFixed32();
        newContext.oldLimit = input.pushLimit(length);
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
    }

    @Override
    protected void doReadEndContainer() {
        if (!input.isAtEnd()) {
            throw DsonIOException.bytesRemain(input.getBytesUntilLimit());
        }
        Context context = getContext();
        input.popLimit(context.oldLimit);

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        setContext(context.parent);
        poolContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doSkipName() {
        input.readUint32();
    }

    @Override
    protected void doSkipValue() {
        DsonReaderUtils.skipValue(input, getContextType(), currentDsonType, currentWireType);
    }

    @Override
    protected void doSkipToEndOfObject() {
        DsonReaderUtils.skipToEndOfObject(input);
    }

    @Override
    protected <T> T doReadMessage(int binaryType, Parser<T> parser) {
        return DsonReaderUtils.readMessage(input, binaryType, parser);
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        return DsonReaderUtils.readValueAsBytes(input, currentDsonType);
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

    protected static class Context extends AbstractDsonLiteReader.Context {

        int oldLimit = -1;

        public Context() {
        }

        public void reset() {
            super.reset();
            oldLimit = -1;
        }
    }

    // endregion

}