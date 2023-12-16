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

import cn.wjybxx.dson.internal.MarkableIterator;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;

import java.util.*;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public class DsonObjectReader extends AbstractDsonReader {

    private String nextName;
    private DsonValue nextValue;

    public DsonObjectReader(DsonReaderSettings settings, DsonArray<String> dsonArray) {
        super(settings);
        Context context = new Context();
        context.init(null, DsonContextType.TOP_LEVEL, null);
        context.header = dsonArray.getHeader().size() > 0 ? dsonArray.getHeader() : null;
        context.arrayIterator = new MarkableIterator<>(dsonArray.iterator());
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

    /**
     * 设置key的迭代顺序
     *
     * @param defValue key不存在时的返回值；可选择{@link DsonNull#UNDEFINE}
     */
    public void setKeyItr(Iterator<String> keyItr, DsonValue defValue) {
        Objects.requireNonNull(keyItr);
        Objects.requireNonNull(defValue);
        Context context = getContext();
        if (context.dsonObject == null) {
            throw DsonIOException.contextError(List.of(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        context.setKeyItr(keyItr, defValue);
    }

    public Set<String> getkeySet() {
        Context context = getContext();
        if (context.dsonObject == null) {
            throw DsonIOException.contextError(List.of(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        return context.dsonObject.keySet();
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
    public DsonType peekDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        if (context.header != null) {
            return DsonType.HEADER;
        }
        if (!context.hasNext()) {
            return DsonType.END_OF_OBJECT;
        }
        if (context.contextType.isLikeArray()) {
            context.markItr();
            DsonValue nextValue = context.nextValue();
            context.resetItr();
            return nextValue.getDsonType();
        } else {
            context.markItr();
            Map.Entry<String, DsonValue> nextElement = context.nextElement();
            context.resetItr();
            return nextElement.getValue().getDsonType();
        }
    }

    @Override
    protected void doReadName() {
        currentName = popNextName();
    }

    // endregion

    // region 简单值

    @Override
    protected int doReadInt32() {
        return popNextValue().asInt32(); // as顺带null检查
    }

    @Override
    protected long doReadInt64() {
        return popNextValue().asInt64();
    }

    @Override
    protected float doReadFloat() {
        return popNextValue().asFloat();
    }

    @Override
    protected double doReadDouble() {
        return popNextValue().asDouble();
    }

    @Override
    protected boolean doReadBool() {
        return popNextValue().asBool();
    }

    @Override
    protected String doReadString() {
        return popNextValue().asString();
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
    protected DsonExtInt32 doReadExtInt32() {
        return popNextValue().asExtInt32();
    }

    @Override
    protected DsonExtInt64 doReadExtInt64() {
        return popNextValue().asExtInt64();
    }

    @Override
    protected DsonExtDouble doReadExtDouble() {
        return popNextValue().asExtDouble();
    }

    @Override
    protected DsonExtString doReadExtString() {
        return popNextValue().asExtString();
    }

    @Override
    protected ObjectRef doReadRef() {
        return popNextValue().asReference();
    }

    @Override
    protected OffsetTimestamp doReadTimestamp() {
        return popNextValue().asTimestamp();
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = newContext(getContext(), contextType, dsonType);
        DsonValue dsonValue = popNextValue();
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            DsonObject<String> dsonObject = dsonValue.asObject();
            newContext.header = dsonObject.getHeader().size() > 0 ? dsonObject.getHeader() : null;
            newContext.dsonObject = dsonObject;
            newContext.objectIterator = new MarkableIterator<>(dsonObject.entrySet().iterator());
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            DsonArray<String> dsonArray = dsonValue.asArray();
            newContext.header = dsonArray.getHeader().size() > 0 ? dsonArray.getHeader() : null;
            newContext.arrayIterator = new MarkableIterator<>(dsonArray.iterator());
        } else {
            // 其它内置结构体
            newContext.dsonObject = dsonValue.asHeader();
            newContext.objectIterator = new MarkableIterator<>(dsonValue.asHeader().entrySet().iterator());
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
        context.header = null;
        if (context.arrayIterator != null) {
            context.arrayIterator.forEachRemaining(dsonValue -> {});
        } else {
            context.objectIterator.forEachRemaining(e -> {});
        }
    }

    @Override
    protected byte[] doReadValueAsBytes() {
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

    protected static class Context extends AbstractDsonReader.Context {

        /** 如果不为null，则表示需要先读取header */
        private DsonHeader<String> header;
        private AbstractDsonObject<String> dsonObject;
        private MarkableIterator<Map.Entry<String, DsonValue>> objectIterator;
        private MarkableIterator<DsonValue> arrayIterator;

        public Context() {
        }

        public void reset() {
            super.reset();
            header = null;
            dsonObject = null;
            objectIterator = null;
            arrayIterator = null;
        }

        public void setKeyItr(Iterator<String> keyItr, DsonValue defValue) {
            if (dsonObject == null) throw new IllegalStateException();
            if (objectIterator.isMarking()) throw new IllegalStateException("reader is in marking state");
            objectIterator = new MarkableIterator<>(new KeyIterator(dsonObject, keyItr, defValue));
        }

        public boolean hasNext() {
            if (objectIterator != null) {
                return objectIterator.hasNext();
            } else {
                return arrayIterator.hasNext();
            }
        }

        public void markItr() {
            if (objectIterator != null) {
                objectIterator.mark();
            } else {
                arrayIterator.mark();
            }
        }

        public void resetItr() {
            if (objectIterator != null) {
                objectIterator.reset();
            } else {
                arrayIterator.reset();
            }
        }

        public DsonValue nextValue() {
            return arrayIterator.hasNext() ? arrayIterator.next() : null;
        }

        public Map.Entry<String, DsonValue> nextElement() {
            return objectIterator.hasNext() ? objectIterator.next() : null;
        }

    }

    private static class KeyIterator implements Iterator<Map.Entry<String, DsonValue>> {

        final AbstractDsonObject<String> dsonObject;
        final Iterator<String> keyItr;
        final DsonValue defValue;

        public KeyIterator(AbstractDsonObject<String> dsonObject, Iterator<String> keyItr, DsonValue defValue) {
            this.dsonObject = dsonObject;
            this.keyItr = keyItr;
            this.defValue = defValue;
        }

        @Override
        public boolean hasNext() {
            return keyItr.hasNext();
        }

        @Override
        public Map.Entry<String, DsonValue> next() {
            String key = keyItr.next();
            DsonValue dsonValue = dsonObject.get(key);
            if (dsonValue == null) {
                return Map.entry(key, defValue);
            } else {
                return Map.entry(key, dsonValue);
            }
        }
    }

    // endregion

}