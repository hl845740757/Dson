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

import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public class DsonObjectReader extends AbstractDsonReader {

    private String nextName;
    private DsonValue nextValue;

    public DsonObjectReader(int recursionLimit, List<DsonValue> valueList) {
        super(recursionLimit);
        Context context = new Context(null, DsonContextType.TOP_LEVEL);
        context.arrayIterator = valueList.iterator();
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

    private void pushNextValue(DsonValue nextValue) {
        this.nextValue = Objects.requireNonNull(nextValue);
    }

    private DsonValue popNextValue() {
        DsonValue r = this.nextValue;
        this.nextValue = null;
        return r;
    }

    private void pushNextName(String nextName) {
        this.nextName = Objects.requireNonNull(nextName);
    }

    private String popNextName() {
        String r = this.nextName;
        this.nextName = null;
        return r;
    }

    @Override
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        popNextName();
        popNextValue();

        DsonType dsonType;
        if (context.header != null) { // 需要先读取header
            dsonType = DsonType.HEADER;
            pushNextValue(context.header);
            context.header = null;
        } else if (context.contextType.isLikeArray()) {
            DsonValue nextValue = context.nextValue();
            if (nextValue == null) {
                dsonType = DsonType.END_OF_OBJECT;
            } else {
                pushNextValue(nextValue);
                dsonType = nextValue.getDsonType();
            }
        } else {
            Map.Entry<String, DsonValue> nextElement = context.nextElement();
            if (nextElement == null) {
                dsonType = DsonType.END_OF_OBJECT;
            } else {
                pushNextName(nextElement.getKey());
                pushNextValue(nextElement.getValue());
                dsonType = nextElement.getValue().getDsonType();
            }
        }

        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VARINT;
        this.currentName = INVALID_NAME;

        onReadDsonType(context, dsonType);
        return dsonType;
    }

    @Override
    protected void doReadName() {
        currentName = popNextName();
    }

    // endregion

    // region 简单值

    @Override
    protected int doReadInt32() {
        return popNextValue().asInt32().getValue(); // as顺带null检查
    }

    @Override
    protected long doReadInt64() {
        return popNextValue().asInt64().getValue();
    }

    @Override
    protected float doReadFloat() {
        return popNextValue().asFloat().getValue();
    }

    @Override
    protected double doReadDouble() {
        return popNextValue().asDouble().getValue();
    }

    @Override
    protected boolean doReadBool() {
        return popNextValue().asBoolean().getValue();
    }

    @Override
    protected String doReadString() {
        return popNextValue().asString().getValue();
    }

    @Override
    protected void doReadNull() {
        popNextValue();
    }

    @Override
    protected DsonBinary doReadBinary() {
        return popNextValue().asBinary().copy(); // 需要拷贝
    }

    @Override
    protected DsonExtString doReadExtString() {
        return popNextValue().asExtString();
    }

    @Override
    protected DsonExtInt32 doReadExtInt32() {
        return popNextValue().asExtInt32();
    }

    @Override
    protected DsonExtInt64 doReadExtInt64() {
        return popNextValue().asExtInt64();
    }

    @Override
    protected ObjectRef doReadRef() {
        return popNextValue().asReference().getValue();
    }

    @Override
    protected OffsetTimestamp doReadTimestamp() {
        return popNextValue().asTimestamp().getValue();
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType) {
        Context newContext = newContext(getContext(), contextType);
        DsonValue dsonValue = popNextValue();
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            MutableDsonObject<String> dsonObject = dsonValue.asObject();
            newContext.header = dsonObject.getHeader();
            newContext.objectIterator = dsonObject.valueMap.entrySet().iterator();
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            MutableDsonArray<String> dsonArray = dsonValue.asArray();
            newContext.header = dsonArray.getHeader();
            newContext.arrayIterator = dsonArray.iterator();
        } else {
            // type == header
            newContext.objectIterator = dsonValue.asHeader().valueMap.entrySet().iterator();
        }
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
    }

    @Override
    protected void doReadEndContainer() {
        Context context = getContext();

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
        popNextName();
    }

    @Override
    protected void doSkipValue() {
        popNextValue();
    }

    @Override
    protected void doSkipToEndOfObject() {
        Context context = getContext();
        if (context.arrayIterator != null) {
            context.arrayIterator.forEachRemaining(dsonValue -> {
            });
        } else {
            context.objectIterator.forEachRemaining(e -> {
            });
        }
    }

    @Override
    protected <T> T doReadMessage(int binaryType, Parser<T> parser) {
        DsonBinary dsonBinary = (DsonBinary) popNextValue();
        Objects.requireNonNull(dsonBinary);
        try {
            return parser.parseFrom(dsonBinary.getData());
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        throw new UnsupportedOperationException();
    }

    // endregion

    // region context

    private Context newContext(Context parent, DsonContextType contextType) {
        Context context = getPooledContext();
        if (context != null) {
            setPooledContext(null);
        } else {
            context = new Context();
        }
        context.init(parent, contextType);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        setPooledContext(context);
    }

    private static class Context extends AbstractDsonReader.Context {

        /** 如果不为null，则表示需要先读取header */
        private DsonHeader<String> header;
        private Iterator<Map.Entry<String, DsonValue>> objectIterator;
        private Iterator<DsonValue> arrayIterator;

        public Context() {
        }

        public Context(Context parent, DsonContextType contextType) {
            super(parent, contextType);
        }

        public void reset() {
            super.reset();
            header = null;
            objectIterator = null;
            arrayIterator = null;
        }

        public DsonValue nextValue() {
            return arrayIterator.hasNext()
                    ? arrayIterator.next() : null;
        }

        public Map.Entry<String, DsonValue> nextElement() {
            return objectIterator.hasNext()
                    ? objectIterator.next() : null;
        }

    }

    // endregion

}