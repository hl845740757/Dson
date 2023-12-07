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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Dson.IO;
using Dson.Types;
using Google.Protobuf;

namespace Dson.Text;

[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public class DsonTextReader : AbstractDsonReader<string>
{
    private static readonly List<DsonTokenType> ValueSeparatorTokens =
        DsonInternals.NewList(DsonTokenType.COMMA, DsonTokenType.END_OBJECT, DsonTokenType.END_ARRAY);
    private static readonly List<DsonTokenType> HeaderTokens = DsonInternals.NewList(DsonTokenType.BEGIN_HEADER, DsonTokenType.CLASS_NAME);

    private static readonly DsonToken TokenBeginHeader = new DsonToken(DsonTokenType.BEGIN_HEADER, "@{", -1);
    private static readonly DsonToken TokenClassname = new DsonToken(DsonTokenType.UNQUOTE_STRING, DsonHeaderFields.NAMES_CLASS_NAME, -1);
    private static readonly DsonToken TokenColon = new DsonToken(DsonTokenType.COLON, ":", -1);
    private static readonly DsonToken TokenEndObject = new DsonToken(DsonTokenType.END_OBJECT, "}", -1);

#nullable disable
    private DsonScanner _scanner;
    private readonly Stack<DsonToken> _pushedTokenQueue = new(6);
    private string _nextName;
    /** 未声明为DsonValue，避免再拆装箱 */
    private object _nextValue;

    private bool _marking;
    /** C#没有现成的Deque，我们拿List实现 */
    private readonly List<DsonToken> _markedTokenQueue = new(6);
#nullable enable

    public DsonTextReader(DsonTextReaderSettings settings, string dson, DsonMode dsonMode = DsonMode.STANDARD)
        : this(settings, new DsonScanner(dson, dsonMode)) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, DsonCharStream charStream)
        : this(settings, new DsonScanner(charStream)) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, DsonScanner scanner)
        : base(settings) {
        this._scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        SetContext(new Context().init(null, DsonContextType.TOP_LEVEL, DsonTypes.INVALID));
    }

    /**
     * 用于动态指定成员数据类型
     * 1.这对于精确解析数组元素和Object的字段十分有用 -- 比如解析一个{@code Vector3}的时候就可以指定字段的默认类型为float。
     * 2.辅助方法见：{@link DsonTexts#clsNameTokenOfType(DsonType)}
     */
    public void SetCompClsNameToken(DsonToken dsonToken) {
        GetContext().CompClsNameToken = dsonToken;
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Dispose() {
        base.Dispose();
        if (_scanner != null) {
            _scanner.Dispose();
            _scanner = null;
        }
    }

    /// <summary>
    /// 保存Stack的原始快照
    /// </summary>
    private void InitMarkQueue() {
        if (_pushedTokenQueue.Count <= 0) {
            return;
        }
        foreach (var dsonToken in _pushedTokenQueue) {
            _markedTokenQueue.Add(dsonToken);
        }
    }

    /// <summary>
    /// 通过快照恢复Stack
    /// </summary>
    private void ResetPushedQueue() {
        _pushedTokenQueue.Clear();
        for (var i = _markedTokenQueue.Count - 1; i >= 0; i--) {
            _pushedTokenQueue.Push(_markedTokenQueue[i]);
        }
        _markedTokenQueue.Clear();
    }

    private DsonToken PopToken() {
        if (_pushedTokenQueue.Count == 0) {
            DsonToken dsonToken = _scanner.NextToken();
            if (_marking) {
                _markedTokenQueue.Add(dsonToken);
            }
            return dsonToken;
        }
        else {
            return _pushedTokenQueue.Pop();
        }
    }

    private void PushToken(DsonToken token) {
        if (token == null) throw new ArgumentNullException(nameof(token));
        _pushedTokenQueue.Push(token);
    }

    private void PushNextValue(object nextValue) {
        this._nextValue = nextValue ?? throw new ArgumentNullException(nameof(nextValue));
    }

    private object PopNextValue() {
        object r = this._nextValue;
        this._nextValue = null;
        return r;
    }

    private void PushNextName(string nextName) {
        this._nextName = nextName ?? throw new ArgumentNullException(nameof(nextName));
    }

    private string PopNextName() {
        string r = this._nextName;
        this._nextName = null;
        return r;
    }

    #region state

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        DsonType dsonType = ReadDsonTypeOfToken();
        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VarInt;
        this.currentName = default!;

        onReadDsonType(context, dsonType);
        if (dsonType == DsonType.HEADER) {
            context.HeaderCount++;
        }
        else {
            context.Count++;
        }
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = GetContext();
        checkReadDsonTypeState(context);

        _marking = true;
        InitMarkQueue(); // 保存Stack

        DsonType dsonType = ReadDsonTypeOfToken();
        _nextName = null; // 丢弃临时数据
        _nextValue = null;

        ResetPushedQueue(); // 恢复Stack
        _marking = false;
        return dsonType;
    }

    private DsonType ReadDsonTypeOfToken() {
        // 丢弃旧值
        _nextName = null;
        _nextValue = null;

        Context context = GetContext();
        // 统一处理逗号分隔符，顶层对象之间可不写分隔符
        if (context.Count > 0) {
            DsonToken nextToken = PopToken();
            if (context.contextType != DsonContextType.TOP_LEVEL) {
                VerifyTokenType(context, nextToken, ValueSeparatorTokens);
            }
            if (nextToken.Type != DsonTokenType.COMMA) {
                PushToken(nextToken);
            }
        }

        // object/header 需要先读取 name和冒号，但object可能出现header
        if (context.contextType == DsonContextType.OBJECT || context.contextType == DsonContextType.HEADER) {
            DsonToken nameToken = PopToken();
            switch (nameToken.Type) {
                case DsonTokenType.STRING:
                case DsonTokenType.UNQUOTE_STRING: {
                    PushNextName(nameToken.castAsString());
                    break;
                }
                case DsonTokenType.BEGIN_HEADER: {
                    if (context.contextType == DsonContextType.HEADER) {
                        throw DsonIOException.containsHeaderDirectly(nameToken);
                    }
                    EnsureCountIsZero(context, nameToken);
                    PushNextValue(nameToken);
                    return DsonType.HEADER;
                }
                case DsonTokenType.END_OBJECT: {
                    return DsonType.END_OF_OBJECT;
                }
                default: {
                    throw DsonIOException.invalidTokenType(context.contextType, nameToken,
                        DsonInternals.NewList(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING, DsonTokenType.END_OBJECT));
                }
            }
            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.COLON);
        }

        // 走到这里，表示 top/object/header/array 读值uco
        DsonToken valueToken = PopToken();
        switch (valueToken.Type) {
            case DsonTokenType.INT32: {
                PushNextValue(valueToken.Value);
                return DsonType.INT32;
            }
            case DsonTokenType.INT64: {
                PushNextValue(valueToken.Value);
                return DsonType.INT64;
            }
            case DsonTokenType.FLOAT: {
                PushNextValue(valueToken.Value);
                return DsonType.FLOAT;
            }
            case DsonTokenType.DOUBLE: {
                PushNextValue(valueToken.Value);
                return DsonType.DOUBLE;
            }
            case DsonTokenType.BOOL: {
                PushNextValue(valueToken.Value);
                return DsonType.BOOLEAN;
            }
            case DsonTokenType.STRING: {
                PushNextValue(valueToken.castAsString());
                return DsonType.STRING;
            }
            case DsonTokenType.NULL: {
                PushNextValue(DsonNull.Null);
                return DsonType.NULL;
            }
            case DsonTokenType.UNQUOTE_STRING: return ParseUnquoteStringToken(context, valueToken);
            case DsonTokenType.BEGIN_OBJECT: return ParseBeginObjectToken(context, valueToken);
            case DsonTokenType.BEGIN_ARRAY: return ParseBeginArrayToken(context, valueToken);
            case DsonTokenType.CLASS_NAME: return ParseClassNameToken(context, valueToken);
            case DsonTokenType.BEGIN_HEADER: {
                // object的header已经处理，这里只有topLevel和array可以再出现header
                if (context.contextType.isLikeObject()) {
                    throw DsonIOException.invalidTokenType(context.contextType, valueToken);
                }
                EnsureCountIsZero(context, valueToken);
                PushNextValue(valueToken);
                return DsonType.HEADER;
            }
            case DsonTokenType.END_ARRAY: {
                // endArray 只能在数组上下文出现；Array是在读取下一个值的时候结束；而Object必须在读取下一个name的时候结束
                if (context.contextType == DsonContextType.ARRAY) {
                    return DsonType.END_OF_OBJECT;
                }
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
            case DsonTokenType.EOF: {
                // eof 只能在顶层上下文出现
                if (context.contextType == DsonContextType.TOP_LEVEL) {
                    return DsonType.END_OF_OBJECT;
                }
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
            default: {
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
        }
        ;
    }

    /** 字符串默认解析规则 */
    private DsonType ParseUnquoteStringToken(Context context, DsonToken valueToken) {
        String unquotedString = valueToken.castAsString();
        if (context.contextType != DsonContextType.HEADER && context.CompClsNameToken != null) {
            switch (context.CompClsNameToken.castAsString()) {
                case DsonTexts.LABEL_INT32: {
                    PushNextValue(DsonTexts.parseInt(unquotedString));
                    return DsonType.INT32;
                }
                case DsonTexts.LABEL_INT64: {
                    PushNextValue(DsonTexts.parseLong(unquotedString));
                    return DsonType.INT64;
                }
                case DsonTexts.LABEL_FLOAT: {
                    PushNextValue(DsonTexts.parseFloat(unquotedString));
                    return DsonType.FLOAT;
                }
                case DsonTexts.LABEL_DOUBLE: {
                    PushNextValue(DsonTexts.parseDouble(unquotedString));
                    return DsonType.DOUBLE;
                }
                case DsonTexts.LABEL_BOOL: {
                    PushNextValue(DsonTexts.parseBool(unquotedString));
                    return DsonType.BOOLEAN;
                }
                case DsonTexts.LABEL_STRING: {
                    PushNextValue(unquotedString);
                    return DsonType.STRING;
                }
            }
        }
        // 处理header的特殊属性依赖
        if (context.contextType == DsonContextType.HEADER) {
            switch (_nextName) {
                case DsonHeaderFields.NAMES_CLASS_NAME:
                case DsonHeaderFields.NAMES_COMP_CLASS_NAME:
                case DsonHeaderFields.NAMES_LOCAL_ID: {
                    PushNextValue(unquotedString);
                    return DsonType.STRING;
                }
            }
        }
        // 处理特殊值解析
        bool trueString = "true" == unquotedString;
        if (trueString || "false" == unquotedString) {
            PushNextValue(trueString);
            return DsonType.BOOLEAN;
        }
        if ("null" == (unquotedString)) {
            PushNextValue(DsonNull.Null);
            return DsonType.NULL;
        }
        if (DsonTexts.isParsable(unquotedString)) {
            PushNextValue(DsonTexts.parseDouble(unquotedString));
            return DsonType.DOUBLE;
        }
        PushNextValue(unquotedString);
        return DsonType.STRING;
    }

    /** 处理内置结构体的单值语法糖 */
    private DsonType ParseClassNameToken(Context context, in DsonToken valueToken) {
        // 1.className不能出现在topLevel，topLevel只能出现header结构体 @{}
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonIOException.invalidTokenType(context.contextType, valueToken);
        }
        // 2.object和array的className会在beginObject和beginArray的时候转换为结构体 @{}
        // 因此这里只能出现内置结构体的简写形式
        String clsName = valueToken.castAsString();
        if (DsonTexts.LABEL_REFERENCE == clsName) { // @ref localId
            DsonToken nextToken = PopToken();
            EnsureStringsToken(context, nextToken);
            PushNextValue(new ObjectRef(nextToken.castAsString()));
            return DsonType.REFERENCE;
        }
        if (DsonTexts.LABEL_DATETIME == clsName) { // @dt uuuu-MM-dd'T'HH:mm:ss
            DateTime dateTime = OffsetTimestamp.parseDateTime(ScanStringUtilComma());
            PushNextValue(new OffsetTimestamp(dateTime.ToEpochSeconds()));
            return DsonType.TIMESTAMP;
        }
        throw DsonIOException.invalidTokenType(context.contextType, valueToken);
    }

    /** 处理内置结构体 */
    private DsonType ParseBeginObjectToken(Context context, in DsonToken valueToken) {
        DsonToken? headerToken;
        if (valueToken.lastChar() == '@') { // {@
            headerToken = PopToken();
            EnsureHeadersToken(context, headerToken);
        }
        else if (context.contextType != DsonContextType.HEADER) {
            headerToken = context.CompClsNameToken;
        }
        else {
            headerToken = null;
        }
        if (headerToken == null) {
            PushNextValue(valueToken);
            return DsonType.OBJECT;
        }

        // 内置结构体
        string clsName = headerToken.castAsString();
        switch (clsName) {
            case DsonTexts.LABEL_REFERENCE: {
                PushNextValue(ScanRef(context));
                return DsonType.REFERENCE;
            }
            case DsonTexts.LABEL_DATETIME: {
                PushNextValue(ScanTimestamp(context));
                return DsonType.TIMESTAMP;
            }
            default: {
                EscapeHeaderAndPush(headerToken);
                PushNextValue(valueToken); // push以供context保存
                return DsonType.OBJECT;
            }
        }
    }

    /** 处理内置二元组 */
    private DsonType ParseBeginArrayToken(Context context, in DsonToken valueToken) {
        DsonToken? headerToken;
        if (valueToken.lastChar() == '@') { // [@
            headerToken = PopToken();
            EnsureHeadersToken(context, headerToken);
        }
        else if (context.contextType != DsonContextType.HEADER) {
            headerToken = context.CompClsNameToken;
        }
        else {
            headerToken = null;
        }
        if (headerToken == null) {
            PushNextValue(valueToken);
            return DsonType.ARRAY;
        }
        // 内置元组
        switch (headerToken.castAsString()) {
            case DsonTexts.LABEL_BINARY: {
                Tuple2 tuple2 = ScanTuple2(context);
                byte[] data = Convert.FromHexString(tuple2.Value);
                PushNextValue(new DsonBinary(tuple2.Type, data));
                return DsonType.BINARY;
            }
            case DsonTexts.LABEL_EXTINT32: {
                Tuple2 tuple2 = ScanTuple2(context);
                bool hasValue = !tuple2.IsUnquoteNull();
                int value = hasValue ? DsonTexts.parseInt(tuple2.Value) : 0;
                PushNextValue(new DsonExtInt32(tuple2.Type, value, hasValue));
                return DsonType.EXT_INT32;
            }
            case DsonTexts.LABEL_EXTINT64: {
                Tuple2 tuple2 = ScanTuple2(context);
                bool hasValue = !tuple2.IsUnquoteNull();
                long value = hasValue ? DsonTexts.parseLong(tuple2.Value) : 0;
                PushNextValue(new DsonExtInt64(tuple2.Type, value, hasValue));
                return DsonType.EXT_INT64;
            }
            case DsonTexts.LABEL_EXTDOUBLE: {
                Tuple2 tuple2 = ScanTuple2(context);
                bool hasValue = !tuple2.IsUnquoteNull();
                double value = hasValue ? DsonTexts.parseDouble(tuple2.Value) : 0;
                PushNextValue(new DsonExtDouble(tuple2.Type, value, hasValue));
                return DsonType.EXT_DOUBLE;
            }
            case DsonTexts.LABEL_EXTSTRING: {
                Tuple2 tuple2 = ScanTuple2(context);
                string? value = tuple2.IsUnquoteNull() ? null : tuple2.Value;
                PushNextValue(new DsonExtString(tuple2.Type, value));
                return DsonType.EXT_STRING;
            }
            default: {
                EscapeHeaderAndPush(headerToken);
                PushNextValue(valueToken); // push以供context保存
                return DsonType.ARRAY;
            }
        }
    }

    private void EscapeHeaderAndPush(DsonToken headerToken) {
        // 如果header不是结构体，则封装为结构体，注意...要反序压栈
        if (headerToken.Type == DsonTokenType.BEGIN_HEADER) {
            PushToken(headerToken);
        }
        else {
            PushToken(TokenEndObject);
            PushToken(new DsonToken(DsonTokenType.STRING, headerToken.castAsString(), -1));
            PushToken(TokenColon);
            PushToken(TokenClassname);
            PushToken(TokenBeginHeader);
        }
    }

    #region 内置结构体语法

    private ObjectRef ScanRef(Context context) {
        string? ns = null;
        string? localId = null;
        int type = 0;
        int policy = 0;
        DsonToken keyToken;
        while ((keyToken = PopToken()).Type != DsonTokenType.END_OBJECT) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);

            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.COLON);

            // 根据name校验
            DsonToken valueToken = PopToken();
            switch (keyToken.castAsString()) {
                case ObjectRef.NAMES_NAMESPACE: {
                    EnsureStringsToken(context, valueToken);
                    ns = valueToken.castAsString();
                    break;
                }
                case ObjectRef.NAMES_LOCAL_ID: {
                    EnsureStringsToken(context, valueToken);
                    localId = valueToken.castAsString();
                    break;
                }
                case ObjectRef.NAMES_TYPE: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UNQUOTE_STRING);
                    type = DsonTexts.parseInt(valueToken.castAsString());
                    break;
                }
                case ObjectRef.NAMES_POLICY: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UNQUOTE_STRING);
                    policy = DsonTexts.parseInt(valueToken.castAsString());
                    break;
                }
                default: {
                    throw new DsonIOException("invalid ref fieldName: " + keyToken.castAsString());
                }
            }
            CheckSeparator(context);
        }
        return new ObjectRef(localId, ns, type, policy);
    }

    private OffsetTimestamp ScanTimestamp(Context context) {
        DateOnly date = new DateOnly(1970, 1, 1);
        TimeOnly time = TimeOnly.MinValue;
        int nanos = 0;
        int offset = 0;
        int enables = 0;
        DsonToken keyToken;
        while ((keyToken = PopToken()).Type != DsonTokenType.END_OBJECT) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);

            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.COLON);

            // 根据name校验
            switch (keyToken.castAsString()) {
                case OffsetTimestamp.NAMES_DATE: {
                    String dateString = ScanStringUtilComma();
                    date = OffsetTimestamp.parseDate(dateString);
                    enables |= OffsetTimestamp.MASK_DATE;
                    break;
                }
                case OffsetTimestamp.NAMES_TIME: {
                    String timeString = ScanStringUtilComma();
                    time = OffsetTimestamp.parseTime(timeString);
                    enables |= OffsetTimestamp.MASK_TIME;
                    break;
                }
                case OffsetTimestamp.NAMES_OFFSET: {
                    String offsetString = ScanStringUtilComma();
                    offset = OffsetTimestamp.parseOffset(offsetString);
                    enables |= OffsetTimestamp.MASK_OFFSET;
                    break;
                }
                case OffsetTimestamp.NAMES_NANOS: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    nanos = DsonTexts.parseInt(valueToken.castAsString());
                    enables |= OffsetTimestamp.MASK_NANOS;
                    break;
                }
                case OffsetTimestamp.NAMES_MILLIS: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    nanos = DsonTexts.parseInt(valueToken.castAsString()) * 1000_000;
                    enables |= OffsetTimestamp.MASK_NANOS;
                    break;
                }
                default: {
                    throw new DsonIOException("invalid datetime fieldName: " + keyToken.castAsString());
                }
            }
            CheckSeparator(context);
        }
        long seconds = new DateTime(date.ToDateTime(time).Ticks, DateTimeKind.Utc).ToEpochSeconds();
        return new OffsetTimestamp(seconds, nanos, offset, enables);
    }

    /** 扫描string，直到遇见逗号或结束符 */
    private string ScanStringUtilComma() {
        StringBuilder sb = new StringBuilder(12);
        while (true) {
            DsonToken valueToken = PopToken();
            switch (valueToken.Type) {
                case DsonTokenType.COMMA:
                case DsonTokenType.END_OBJECT:
                case DsonTokenType.END_ARRAY: {
                    PushToken(valueToken);
                    return sb.ToString();
                }
                case DsonTokenType.STRING:
                case DsonTokenType.UNQUOTE_STRING:
                case DsonTokenType.COLON: {
                    sb.Append(valueToken.castAsString());
                    break;
                }
                default: {
                    throw DsonIOException.invalidTokenType(ContextType, valueToken);
                }
            }
        }
    }

    private void CheckSeparator(Context context) {
        // 每读取一个值，判断下分隔符，尾部最多只允许一个逗号 -- 这里在尾部更容易处理
        DsonToken keyToken;
        if ((keyToken = PopToken()).Type == DsonTokenType.COMMA
            && (keyToken = PopToken()).Type == DsonTokenType.COMMA) {
            throw DsonIOException.invalidTokenType(context.contextType, keyToken);
        }
        else {
            PushToken(keyToken);
        }
    }

    #endregion

    #region 内置元组语法

    private Tuple2 ScanTuple2(Context context) {
        // beginArray已读取
        DsonToken nextToken = PopToken();
        VerifyTokenType(context, nextToken, DsonTokenType.UNQUOTE_STRING);
        int type = DsonTexts.parseInt(nextToken.castAsString());

        nextToken = PopToken();
        VerifyTokenType(context, nextToken, DsonTokenType.COMMA);

        nextToken = PopToken();
        EnsureStringsToken(context, nextToken);
        String value = nextToken.castAsString();
        DsonTokenType valueTokenType = nextToken.Type;

        nextToken = PopToken();
        VerifyTokenType(context, nextToken, DsonTokenType.END_ARRAY);
        return new Tuple2(type, value, valueTokenType);
    }

    private struct Tuple2
    {
        internal readonly int Type;
        internal readonly string Value;
        internal readonly DsonTokenType TokenType;

        public Tuple2(int type, string value, DsonTokenType tokenType) {
            this.Type = type;
            this.Value = value;
            this.TokenType = tokenType;
        }

        internal bool IsUnquoteNull() {
            return "null" == Value;
        }
    }

    #endregion

    /** header不可以在中途出现 */
    private static void EnsureCountIsZero(Context context, DsonToken headerToken) {
        if (context.Count > 0) {
            throw DsonIOException.invalidTokenType(context.contextType, headerToken,
                DsonInternals.NewList(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING, DsonTokenType.END_OBJECT));
        }
    }

    private static void EnsureStringsToken(Context context, DsonToken token) {
        if (token.Type != DsonTokenType.STRING && token.Type != DsonTokenType.UNQUOTE_STRING) {
            throw DsonIOException.invalidTokenType(context.contextType, token,
                DsonInternals.NewList(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING));
        }
    }

    private static void EnsureHeadersToken(Context context, DsonToken token) {
        if (token.Type != DsonTokenType.BEGIN_HEADER && token.Type != DsonTokenType.CLASS_NAME) {
            throw DsonIOException.invalidTokenType(context.contextType, token,
                DsonInternals.NewList(DsonTokenType.BEGIN_HEADER, DsonTokenType.CLASS_NAME));
        }
    }

    private static void VerifyTokenType(Context context, DsonToken token, DsonTokenType expected) {
        if (token.Type != expected) {
            throw DsonIOException.invalidTokenType(context.contextType, token, DsonInternals.NewList(expected));
        }
    }

    private static void VerifyTokenType(Context context, DsonToken token, List<DsonTokenType> expected) {
        if (!expected.Contains(token.Type)) {
            throw DsonIOException.invalidTokenType(context.contextType, token, expected);
        }
    }

    protected override void doReadName() {
        currentName = PopNextName();
        // 将header中的特殊属性记录下来
        Context context = GetContext();
        if (context.contextType == DsonContextType.HEADER) {
            if (DsonHeaderFields.NAMES_COMP_CLASS_NAME != currentName) {
                context.CompClsNameToken = new DsonToken(DsonTokenType.CLASS_NAME, _nextValue, -1);
            }
            // else 其它属性
        }
    }

    #endregion

    #region 简单值

    protected override int doReadInt32() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToInt32(NumberFormatInfo.InvariantInfo);
    }

    protected override long doReadInt64() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToInt64(CultureInfo.InvariantCulture);
    }

    protected override float doReadFloat() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToSingle(NumberFormatInfo.InvariantInfo);
    }

    protected override double doReadDouble() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToDouble(NumberFormatInfo.InvariantInfo);
    }

    protected override bool doReadBool() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (bool)value;
    }

    protected override string doReadString() {
        string value = (string)PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return value;
    }

    protected override void doReadNull() {
        PopNextValue();
    }

    protected override DsonBinary doReadBinary() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonBinary)value;
    }

    protected override DsonExtInt32 doReadExtInt32() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtInt32)value;
    }

    protected override DsonExtInt64 doReadExtInt64() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtInt64)value;
    }

    protected override DsonExtDouble doReadExtDouble() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtDouble)value;
    }

    protected override DsonExtString doReadExtString() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtString)value;
    }

    protected override ObjectRef doReadRef() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (ObjectRef)value;
    }

    protected override OffsetTimestamp doReadTimestamp() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (OffsetTimestamp)value;
    }

    #endregion

    #region 容器

    protected override void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        newContext.BeginToken = (DsonToken)(PopNextValue() ?? throw new InvalidOperationException());
        newContext.name = currentName;

        this.recursionDepth++;
        SetContext(newContext);
    }

    protected override void doReadEndContainer() {
        Context context = GetContext();
        // header中的信息是修饰外层对象的
        if (context.contextType == DsonContextType.HEADER) {
            context.Parent.CompClsNameToken = context.CompClsNameToken;
        }

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        SetContext(context.parent);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void doSkipName() {
        // 名字早已读取
        PopNextName();
    }

    protected override void doSkipValue() {
        PopNextValue();
        switch (currentDsonType) {
            case DsonType.HEADER:
            case DsonType.OBJECT:
            case DsonType.ARRAY: {
                SkipStack(1);
                break;
            }
        }
    }

    protected override void doSkipToEndOfObject() {
        DsonToken endToken;
        if (IsAtType) {
            endToken = SkipStack(1);
        }
        else {
            SkipName();
            switch (currentDsonType) {
                case DsonType.HEADER:
                case DsonType.OBJECT:
                case DsonType.ARRAY: { // 嵌套对象
                    endToken = SkipStack(2);
                    break;
                }
                default: {
                    endToken = SkipStack(1);
                    break;
                }
            }
        }
        PushToken(endToken);
    }

    /** @return 触发结束的token */
    private DsonToken SkipStack(int stack) {
        while (stack > 0) {
            DsonToken token = PopToken();
            switch (token.Type) {
                case DsonTokenType.BEGIN_ARRAY:
                case DsonTokenType.BEGIN_OBJECT:
                case DsonTokenType.BEGIN_HEADER: {
                    stack++;
                    break;
                }
                case DsonTokenType.END_ARRAY:
                case DsonTokenType.END_OBJECT: {
                    if (--stack == 0) {
                        return token;
                    }
                    break;
                }
                case DsonTokenType.EOF: {
                    throw DsonIOException.invalidTokenType(ContextType, token);
                }
            }
        }
        throw new InvalidOperationException("Assert Exception");
    }

    protected override T doReadMessage<T>(int binaryType, MessageParser<T> parser) {
        DsonBinary dsonBinary = (DsonBinary)PopNextValue() ?? throw new InvalidOperationException();
        if (dsonBinary.Type != binaryType) {
            throw DsonIOException.unexpectedSubType(binaryType, dsonBinary.Type);
        }
        return parser.ParseFrom(dsonBinary.Data);
    }

    protected override byte[] doReadValueAsBytes() {
        // Text的Reader和Writer实现最好相同，要么都不支持，要么都支持
        throw new DsonIOException("UnsupportedOperation");
    }

    #endregion

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context? context = GetPooledContext();
        if (context != null) {
            SetPooledContext(null);
        }
        else {
            context = new Context();
        }
        context.init(parent, contextType, dsonType);
        return context;
    }

    private void PoolContext(Context context) {
        context.reset();
        SetPooledContext(context);
    }

    protected new class Context : AbstractDsonReader<string>.Context
    {
#nullable disable
        internal DsonToken BeginToken;
#nullable enable

        /** header只可触发一次流程 */
        internal int HeaderCount = 0;
        /** 元素计数，判断冒号 */
        internal int Count;
        /** 数组/Object成员的类型 - token类型可直接复用；header的该属性是用于注释外层对象的 */
        internal DsonToken? CompClsNameToken;

        public Context() {
        }

        public void reset() {
            base.reset();
            BeginToken = null;
            HeaderCount = 0;
            Count = 0;
            CompClsNameToken = null;
        }

        public new Context Parent => (Context)parent;
    }

    #endregion
}