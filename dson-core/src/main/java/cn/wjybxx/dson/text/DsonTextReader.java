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

package cn.wjybxx.dson.text;

import cn.wjybxx.dson.*;
import cn.wjybxx.dson.internal.CollectionUtils;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.commons.lang3.math.NumberUtils;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneOffset;
import java.util.ArrayDeque;
import java.util.List;
import java.util.Objects;

/**
 * 在二进制下，写入的顺序是： type-name-value
 * 但在文本格式下，写入的顺序是：name-type-value
 * 但我们要为用户提供一致的api，即对上层表现为二进制相同的读写顺序，因此我们需要将name缓存下来，直到用户调用readName。
 * 另外，我们只有先读取了value的token之后，才可以返回数据的类型{@link DsonType}，
 * 因此 name-type-value 通常是在一次readType中完成。
 * <p>
 * 另外，分隔符也需要压栈，以验证用户输入的正确性。
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonTextReader extends AbstractDsonReader {

    private static final List<TokenType> VALUE_SEPARATOR_TOKENS = List.of(TokenType.COMMA, TokenType.END_OBJECT, TokenType.END_ARRAY);
    private static final DsonToken TOKEN_START_HEADER = new DsonToken(TokenType.HEADER, "@{", -1);
    private static final DsonToken TOKEN_END_OBJECT = new DsonToken(TokenType.END_OBJECT, "}", -1);
    private static final DsonToken TOKEN_COLON = new DsonToken(TokenType.COLON, ":", -1);
    private static final DsonToken TOKEN_CLASSNAME = new DsonToken(TokenType.UNQUOTE_STRING, DsonHeader.NAMES_CLASS_NAME, -1);

    private DsonScanner scanner;
    private final ArrayDeque<DsonToken> pushedTokenQueue = new ArrayDeque<>(6);
    private String nextName;
    /** 未声明为DsonValue，避免再拆装箱 */
    private Object nextValue;

    public DsonTextReader(int recursionLimit, String dson) {
        this(recursionLimit, new DsonScanner(new DsonStringBuffer(dson)));
    }

    public DsonTextReader(int recursionLimit, List<String> dsonLines) {
        this(recursionLimit, new DsonScanner(new DsonLinesBuffer(dsonLines)));
    }

    public DsonTextReader(int recursionLimit, DsonScanner scanner) {
        super(recursionLimit);
        this.scanner = scanner;
        setContext(new Context().init(null, DsonContextType.TOP_LEVEL, null));
    }

    @Override
    public void close() {
        if (scanner != null) {
            scanner.close();
            scanner = null;
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

    private DsonToken popToken() {
        if (pushedTokenQueue.isEmpty()) {
            return scanner.nextToken();
        } else {
            return pushedTokenQueue.pop();
        }
    }

    private void pushToken(DsonToken token) {
        Objects.requireNonNull(token);
        pushedTokenQueue.push(token);
    }

    private void pushNextValue(Object nextValue) {
        this.nextValue = Objects.requireNonNull(nextValue);
    }

    private Object popNextValue() {
        Object r = this.nextValue;
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

    // region state

    @Override
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        DsonType dsonType = readDsonTypeOfToken();
        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VARINT;
        this.currentName = INVALID_NAME;

        onReadDsonType(context, dsonType);
        if (dsonType == DsonType.HEADER) {
            context.headerCount++;
        } else {
            context.count++;
        }
        return dsonType;
    }

    /**
     * 两个职责：
     * 1.校验token在上下文中的正确性 -- 上层会校验DsonType的合法性
     * 2.将合法的token转换为dson的键值对（或值）
     * <p>
     * 在读取valueToken时遇见 { 或 [ 时要判断是否是内置结构体，如果是内置结构体，要预读为值，而不是返回beginXXX；
     * 如果不是内置结构体，如果是 '@className' 形式声明的类型，要伪装成 {clsName: $className} 的token流，使得上层可按照相同的方式解析。
     * '@clsName' 本质是简化书写的语法糖。
     */
    private DsonType readDsonTypeOfToken() {
        // 丢弃旧值
        popNextName();
        popNextValue();

        Context context = getContext();
        if (context.count == 0) {
            // object和array可以有一个修饰自己的header，其合法性在读取到{和[时验证
            if (context.headerCount == 0 && context.contextType.isContainer() && context.beginToken.lastChar() == '@') {
                DsonToken headerToken = popToken();
                verifyTokenType(context, headerToken, TokenType.HEADER);
                pushNextValue(headerToken);
                return DsonType.HEADER;
            }
        } else {
            // 统一处理 逗号 分隔符，顶层对象之间可不写分隔符
            DsonToken nextToken = popToken();
            if (context.contextType != DsonContextType.TOP_LEVEL) {
                verifyTokenType(context, nextToken, VALUE_SEPARATOR_TOKENS);
            }
            if (nextToken.getType() != TokenType.COMMA) {
                pushToken(nextToken);
            }
        }

        // object/header 需要先读取 name 和 冒号
        if (context.contextType == DsonContextType.OBJECT || context.contextType == DsonContextType.HEADER) {
            DsonToken nameToken = popToken();
            switch (nameToken.getType()) {
                case STRING, UNQUOTE_STRING -> {
                    pushNextName(nameToken.castAsString());
                }
                case END_OBJECT -> {
                    return DsonType.END_OF_OBJECT;
                }
                default -> throw DsonIOException.invalidTokenType(context.contextType, nameToken,
                        List.of(TokenType.STRING, TokenType.UNQUOTE_STRING, TokenType.END_OBJECT));
            }
            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, TokenType.COLON);
        }

        // 走到这里，表示 top/object/header/array 读值
        DsonToken valueToken = popToken();
        return switch (valueToken.getType()) {
            case INT32 -> {
                pushNextValue(valueToken.getValue());
                yield DsonType.INT32;
            }
            case INT64 -> {
                pushNextValue(valueToken.getValue());
                yield DsonType.INT64;
            }
            case FLOAT -> {
                pushNextValue(valueToken.getValue());
                yield DsonType.FLOAT;
            }
            case DOUBLE -> {
                pushNextValue(valueToken.getValue());
                yield DsonType.DOUBLE;
            }
            case BOOL -> {
                pushNextValue(valueToken.getValue());
                yield DsonType.BOOLEAN;
            }
            case STRING -> {
                pushNextValue(valueToken.castAsString());
                yield DsonType.STRING;
            }
            case NULL -> {
                pushNextValue(DsonNull.INSTANCE);
                yield DsonType.NULL;
            }
            case BEGIN_ARRAY -> parseBeginArrayToken(context, valueToken);
            case BEGIN_OBJECT -> parseBeginObjectToken(context, valueToken);
            case UNQUOTE_STRING -> parseUnquoteStringToken(context, valueToken);
            case HEADER -> parseHeaderToken(context, valueToken);
            case END_ARRAY -> {
                // Array必须在读取下一个值的时候结束；而Object必须在下次读取name的时候结束
                if (context.contextType == DsonContextType.ARRAY) {
                    yield DsonType.END_OF_OBJECT;
                }
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
            case EOF -> {
                // eof 只能在顶层上下文出现
                if (context.contextType == DsonContextType.TOP_LEVEL) {
                    yield DsonType.END_OF_OBJECT;
                }
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
            default -> {
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
        };
    }

    /** 字符串默认解析规则 */
    private DsonType parseUnquoteStringToken(Context context, DsonToken valueToken) {
        String unquotedString = valueToken.castAsString();
        if (context.compClsNameToken != null) {
            switch (context.compClsNameToken.castAsString()) {
                case DsonTexts.LABEL_INT32 -> {
                    pushNextValue(DsonTexts.parseInt(unquotedString));
                    return DsonType.INT32;
                }
                case DsonTexts.LABEL_INT64 -> {
                    pushNextValue(DsonTexts.parseLong(unquotedString));
                    return DsonType.INT64;
                }
                case DsonTexts.LABEL_FLOAT -> {
                    pushNextValue(DsonTexts.parseFloat(unquotedString));
                    return DsonType.FLOAT;
                }
                case DsonTexts.LABEL_DOUBLE -> {
                    pushNextValue(DsonTexts.parseDouble(unquotedString));
                    return DsonType.DOUBLE;
                }
                case DsonTexts.LABEL_BOOL -> {
                    pushNextValue(DsonTexts.parseBool(unquotedString));
                    return DsonType.BOOLEAN;
                }
            }
        }
        if (context.contextType == DsonContextType.HEADER) {
            // 处理header的特殊属性依赖
            switch (nextName) {
                case DsonHeader.NAMES_CLASS_NAME,
                        DsonHeader.NAMES_COMP_CLASS_NAME,
                        DsonHeader.NAMES_LOCAL_ID -> {
                    pushNextValue(unquotedString);
                    return DsonType.STRING;
                }
            }
        }

        if ("true".equals(unquotedString) || "false".equals(unquotedString)) {
            pushNextValue(Boolean.valueOf(unquotedString));
            return DsonType.BOOLEAN;
        }
        if ("null".equals(unquotedString)) {
            pushNextValue(DsonNull.INSTANCE);
            return DsonType.NULL;
        }
        // 简单的数
        if (DsonTexts.isParsable(unquotedString)) {
            pushNextValue(DsonTexts.parseDouble(unquotedString));
            return DsonType.DOUBLE;
        }
        pushNextValue(unquotedString);
        return DsonType.STRING;
    }

    /** 处理内置结构体的语法糖 */
    private DsonType parseHeaderToken(Context context, final DsonToken valueToken) {
        String clsName = valueToken.castAsString();
        if (DsonTexts.LABEL_REFERENCE.equals(clsName)) {// @ref localId
            DsonToken nextToken = popToken();
            ensureStringsToken(context, nextToken);
            pushNextValue(new ObjectRef(null, nextToken.castAsString()));
            return DsonType.REFERENCE;
        }
        // 对象的header只在beginObject和beginArray可能产生，其它时候都不能出现
        if (context.contextType != DsonContextType.TOP_LEVEL) {
            throw DsonIOException.invalidTokenType(context.contextType, valueToken);
        }
        // 允许顶层出现header方便未来扩展
        escapeHeaderAndPush(valueToken);
        pushNextValue(popToken());
        return DsonType.HEADER;
    }

    /** 处理内置结构体的语法糖 */
    private DsonType parseBeginObjectToken(Context context, final DsonToken valueToken) {
        DsonToken headerToken;
        if (valueToken.lastChar() == '@') {
            headerToken = popToken();
            verifyTokenType(getContext(), headerToken, TokenType.HEADER);
        } else {
            headerToken = context.compClsNameToken;
        }
        if (headerToken == null) {
            pushNextValue(valueToken);
            return DsonType.OBJECT;
        }

        String clsName = headerToken.castAsString();
        if (DsonTexts.LABEL_REFERENCE.equals(clsName)) {
            pushNextValue(scanRef(context));
            return DsonType.REFERENCE;
        }
        if (DsonTexts.LABEL_DATETIME.equals(clsName)) {
            pushNextValue(scanTimestamp(context));
            return DsonType.TIMESTAMP;
        }

        escapeHeaderAndPush(headerToken);
        pushNextValue(valueToken); // push以供context保存
        return DsonType.OBJECT;
    }

    /** 需要处理内置二元组 */
    private DsonType parseBeginArrayToken(Context context, final DsonToken valueToken) {
        DsonToken headerToken;
        if (valueToken.lastChar() == '@') {
            headerToken = popToken();
            verifyTokenType(getContext(), headerToken, TokenType.HEADER);
        } else {
            headerToken = context.compClsNameToken;
        }
        if (headerToken == null) {
            pushNextValue(valueToken);
            return DsonType.ARRAY;
        }

        return switch (headerToken.castAsString()) {
            case DsonTexts.LABEL_BINARY -> {
                Tuple2 tuple2 = scanTuple2(context);
                byte[] data = DsonTexts.decodeHex(tuple2.value.toCharArray());
                pushNextValue(new DsonBinary(tuple2.type, data));
                yield DsonType.BINARY;
            }
            case DsonTexts.LABEL_EXTINT32 -> {
                Tuple2 tuple2 = scanTuple2(context);
                int value = DsonTexts.parseInt(tuple2.value);
                pushNextValue(new DsonExtInt32(tuple2.type, value));
                yield DsonType.EXT_INT32;
            }
            case DsonTexts.LABEL_EXTINT64 -> {
                Tuple2 tuple2 = scanTuple2(context);
                long value = DsonTexts.parseLong(tuple2.value);
                pushNextValue(new DsonExtInt64(tuple2.type, value));
                yield DsonType.EXT_INT64;
            }
            case DsonTexts.LABEL_EXTSTRING -> {
                Tuple2 tuple2 = scanTuple2(context);
                pushNextValue(new DsonExtString(tuple2.type, tuple2.value));
                yield DsonType.EXT_STRING;
            }
            default -> {
                escapeHeaderAndPush(headerToken);
                pushNextValue(valueToken); // push以供context保存
                yield DsonType.ARRAY;
            }
        };
    }

    private ObjectRef scanRef(Context context) {
        String namespace = null;
        String localId = null;
        int type = 0;
        int policy = 0;
        DsonToken keyToken;
        while ((keyToken = popToken()).getType() != TokenType.END_OBJECT) {
            // key必须是字符串
            ensureStringsToken(context, keyToken);

            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, TokenType.COLON);

            // 根据name校验
            DsonToken valueToken = popToken();
            switch (keyToken.castAsString()) {
                case ObjectRef.NAMES_NAMESPACE -> {
                    ensureStringsToken(context, valueToken);
                    namespace = valueToken.castAsString();
                }
                case ObjectRef.NAMES_LOCAL_ID -> {
                    ensureStringsToken(context, valueToken);
                    localId = valueToken.castAsString();
                }
                case ObjectRef.NAMES_TYPE -> {
                    verifyTokenType(context, valueToken, TokenType.UNQUOTE_STRING);
                    type = DsonTexts.parseInt(valueToken.castAsString());
                }
                case ObjectRef.NAMES_POLICY -> {
                    verifyTokenType(context, valueToken, TokenType.UNQUOTE_STRING);
                    policy = DsonTexts.parseInt(valueToken.castAsString());
                }
                default -> {
                    throw new DsonIOException("invalid ref fieldName: " + keyToken.castAsString());
                }
            }
            checkSeparator(context);
        }
        return new ObjectRef(namespace, localId, type, policy);
    }

    private OffsetTimestamp scanTimestamp(Context context) {
        LocalDate date = LocalDate.EPOCH;
        LocalTime time = LocalTime.MIN;
        int nanos = 0;
        int offset = 0;
        int enables = 0;
        DsonToken keyToken;
        while ((keyToken = popToken()).getType() != TokenType.END_OBJECT) {
            // key必须是字符串
            ensureStringsToken(context, keyToken);

            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, TokenType.COLON);

            // 根据name校验
            switch (keyToken.castAsString()) {
                case OffsetTimestamp.NAMES_DATE -> {
                    String dateString = scanStringUtilComma();
                    date = OffsetTimestamp.parseDate(dateString);
                    enables |= OffsetTimestamp.MASK_DATE;
                }
                case OffsetTimestamp.NAMES_TIME -> {
                    String timeString = scanStringUtilComma();
                    time = OffsetTimestamp.parseTime(timeString);
                    enables |= OffsetTimestamp.MASK_TIME;
                }
                case OffsetTimestamp.NAMES_OFFSET -> {
                    String offsetString = scanStringUtilComma();
                    offset = OffsetTimestamp.parseOffset(offsetString);
                    enables |= OffsetTimestamp.MASK_OFFSET;
                }
                case OffsetTimestamp.NAMES_NANOS -> {
                    DsonToken valueToken = popToken();
                    ensureStringsToken(context, valueToken);
                    nanos = DsonTexts.parseInt(valueToken.castAsString());
                    if (nanos < 0) {
                        throw new IllegalArgumentException("invalid nanos " + valueToken);
                    }
                }
                case OffsetTimestamp.NAMES_MILLIS -> {
                    DsonToken valueToken = popToken();
                    ensureStringsToken(context, valueToken);
                    int millis = DsonTexts.parseInt(valueToken.castAsString());
                    if (millis < 0 || millis > 999) {
                        throw new IllegalArgumentException("invalid millis " + valueToken);
                    }
                    nanos = millis * 1000000;
                }
                default -> {
                    throw new DsonIOException("invalid datetime fieldName: " + keyToken.castAsString());
                }
            }
            checkSeparator(context);
        }
        long seconds = LocalDateTime.of(date, time).toEpochSecond(ZoneOffset.UTC);
        return new OffsetTimestamp(seconds, nanos, offset, enables);
    }

    /** 扫描string，直到遇见逗号或结束符 */
    private String scanStringUtilComma() {
        StringBuilder sb = new StringBuilder(12);
        while (true) {
            DsonToken valueToken = popToken();
            switch (valueToken.getType()) {
                case COMMA, END_OBJECT, END_ARRAY -> {
                    pushToken(valueToken);
                    return sb.toString();
                }
                default -> {
                    sb.append(valueToken.castAsString());
                }
            }
        }
    }

    private Tuple2 scanTuple2(Context context) {
        // beginArray已读取
        DsonToken nextToken = popToken();
        verifyTokenType(context, nextToken, TokenType.UNQUOTE_STRING);
        int type = DsonTexts.parseInt(nextToken.castAsString());

        nextToken = popToken();
        verifyTokenType(context, nextToken, TokenType.COMMA);

        nextToken = popToken();
        ensureStringsToken(context, nextToken);
        String value = nextToken.castAsString();

        nextToken = popToken();
        verifyTokenType(context, nextToken, TokenType.END_ARRAY);
        return new Tuple2(type, value);
    }

    private static class Tuple2 {

        int type;
        String value;

        public Tuple2(int type, String value) {
            this.type = type;
            this.value = value;
        }
    }

    private void checkSeparator(Context context) {
        // 每读取一个值，判断下分隔符，尾部最多只允许一个逗号 -- 这里在尾部更容易处理
        DsonToken keyToken;
        if ((keyToken = popToken()).getType() == TokenType.COMMA
            && (keyToken = popToken()).getType() == TokenType.COMMA) {
            throw DsonIOException.invalidTokenType(context.contextType, keyToken);
        } else {
            pushToken(keyToken);
        }
    }

    private void escapeHeaderAndPush(DsonToken headerToken) {
        // 如果header不是结构体，则封装为结构体，注意...要反序压栈
        if (headerToken.firstChar() != '{') {
            pushToken(TOKEN_END_OBJECT);
            pushToken(new DsonToken(TokenType.STRING, headerToken.castAsString(), -1));
            pushToken(TOKEN_COLON);
            pushToken(TOKEN_CLASSNAME);
            pushToken(TOKEN_START_HEADER);
        } else {
            pushToken(headerToken);
        }
    }

    private static void ensureStringsToken(Context context, DsonToken token) {
        if (token.getType() != TokenType.STRING && token.getType() != TokenType.UNQUOTE_STRING) {
            throw DsonIOException.invalidTokenType(context.contextType, token, List.of(TokenType.STRING, TokenType.UNQUOTE_STRING));
        }
    }

    private static void verifyTokenType(Context context, DsonToken token, TokenType expected) {
        if (token.getType() != expected) {
            throw DsonIOException.invalidTokenType(context.contextType, token, List.of(expected));
        }
    }

    private static void verifyTokenType(Context context, DsonToken token, List<TokenType> expected) {
        if (!CollectionUtils.containsRef(expected, token.getType())) {
            throw DsonIOException.invalidTokenType(context.contextType, token, expected);
        }
    }

    @Override
    protected void doReadName() {
        currentName = Objects.requireNonNull(popNextName());
        // 将header中的特殊属性记录下来
        Context context = getContext();
        if (context.contextType == DsonContextType.HEADER) {
            if (context.compClsNameToken == null && DsonHeader.NAMES_COMP_CLASS_NAME.equals(currentName)) {
                context.compClsNameToken = new DsonToken(TokenType.HEADER, nextValue, -1);
            }
            // 其它属性
        }
    }

    // endregion

    // region 简单值

    @Override
    protected int doReadInt32() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.intValue();
    }

    @Override
    protected long doReadInt64() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.longValue();
    }

    @Override
    protected float doReadFloat() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.floatValue();
    }

    @Override
    protected double doReadDouble() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.doubleValue();
    }

    @Override
    protected boolean doReadBool() {
        Boolean value = (Boolean) popNextValue();
        Objects.requireNonNull(value);
        return value;
    }

    @Override
    protected String doReadString() {
        String value = (String) popNextValue();
        Objects.requireNonNull(value);
        return value;
    }

    @Override
    protected void doReadNull() {
        Object value = popNextValue();
        assert value == DsonNull.INSTANCE;
    }

    @Override
    protected DsonBinary doReadBinary() {
        return (DsonBinary) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected DsonExtString doReadExtString() {
        return (DsonExtString) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected DsonExtInt32 doReadExtInt32() {
        return (DsonExtInt32) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected DsonExtInt64 doReadExtInt64() {
        return (DsonExtInt64) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected ObjectRef doReadRef() {
        return (ObjectRef) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected OffsetTimestamp doReadTimestamp() {
        return (OffsetTimestamp) Objects.requireNonNull(popNextValue());
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = newContext(getContext(), contextType, dsonType);
        newContext.beginToken = (DsonToken) Objects.requireNonNull(popNextValue());
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
    }

    @Override
    protected void doReadEndContainer() {
        Context context = getContext();
        // header中的信息是修饰外层对象的
        if (context.contextType == DsonContextType.HEADER) {
            context.getParent().compClsNameToken = context.compClsNameToken;
        }

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
        // 名字早已读取
        popNextName();
    }

    @Override
    protected void doSkipValue() {
        popNextValue();
        int stack = 0;
        switch (currentDsonType) {
            case HEADER, OBJECT, ARRAY -> stack = 1;
        }
        skipStack(stack);
    }

    @Override
    protected void doSkipToEndOfObject() {
        int stack = 1;
        skipStack(stack);
        // 避免计数导致readDsonType异常
        getContext().count++;
        getContext().headerCount++;
    }

    private void skipStack(int stack) {
        while (stack > 0) {
            DsonToken token = popToken();
            switch (token.getType()) {
                case BEGIN_ARRAY, BEGIN_OBJECT -> stack++;
                case HEADER -> {
                    if (token.lastChar() == '{') { // @{
                        stack++;
                    }
                }
                case END_ARRAY, END_OBJECT -> {
                    stack--;
                    if (stack == 0) {
                        pushToken(token);
                        return;
                    }
                }
                case EOF -> {
                    throw DsonIOException.invalidTokenType(getContextType(), token);
                }
            }
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
        // Text的Reader和Writer实现最好相同，要么都不支持，要么都支持
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

    private static class Context extends AbstractDsonReader.Context {

        DsonToken beginToken;
        /** header只可触发一次流程 */
        int headerCount = 0;
        /** 元素计数，判断冒号 */
        int count;
        /** 数组/Object成员的类型 - token类型可直接复用 */
        DsonToken compClsNameToken;

        public Context() {
        }

        public void reset() {
            super.reset();
            beginToken = null;
            headerCount = 0;
            count = 0;
            compClsNameToken = null;
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }

    }

    // endregion

}