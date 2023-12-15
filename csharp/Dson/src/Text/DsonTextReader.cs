#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text;

[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public class DsonTextReader : AbstractDsonReader<string>
{
    private static readonly List<DsonTokenType> ValueSeparatorTokens =
        DsonInternals.NewList(DsonTokenType.Comma, DsonTokenType.EndObject, DsonTokenType.EndArray);
    private static readonly List<DsonTokenType> HeaderTokens =
        DsonInternals.NewList(DsonTokenType.BeginHeader, DsonTokenType.ClassName);

    private static readonly DsonToken TokenBeginHeader = new DsonToken(DsonTokenType.BeginHeader, "@{", -1);
    private static readonly DsonToken TokenClassname = new DsonToken(DsonTokenType.UnquoteString, DsonHeaders.NamesClassName, -1);
    private static readonly DsonToken TokenColon = new DsonToken(DsonTokenType.Colon, ":", -1);
    private static readonly DsonToken TokenEndObject = new DsonToken(DsonTokenType.EndObject, "}", -1);

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

    public DsonTextReader(DsonTextReaderSettings settings, string dson, DsonMode dsonMode = DsonMode.Standard)
        : this(settings, new DsonScanner(dson, dsonMode)) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, IDsonCharStream charStream)
        : this(settings, new DsonScanner(charStream)) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, DsonScanner scanner)
        : base(settings) {
        this._scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        SetContext(new Context().Init(null, DsonContextType.TopLevel, DsonTypes.Invalid));
    }

    /**
     * 用于动态指定成员数据类型
     * 1.这对于精确解析数组元素和Object的字段十分有用 -- 比如解析一个{@code Vector3}的时候就可以指定字段的默认类型为float。
     * 2.辅助方法见：{@link DsonTexts#clsNameTokenOfType(DsonType)}
     */
    public void SetCompClsNameToken(DsonToken dsonToken) {
        GetContext()._compClsNameToken = dsonToken;
    }

    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context?)base.GetPooledContext();
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
        CheckReadDsonTypeState(context);

        DsonType dsonType = ReadDsonTypeOfToken();
        this._currentDsonType = dsonType;
        this._currentWireType = WireType.VarInt;
        this._currentName = default!;

        OnReadDsonType(context, dsonType);
        if (dsonType == DsonType.Header) {
            context._headerCount++;
        }
        else {
            context._count++;
        }
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

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
        if (context._count > 0) {
            DsonToken nextToken = PopToken();
            if (context._contextType != DsonContextType.TopLevel) {
                VerifyTokenType(context, nextToken, ValueSeparatorTokens);
            }
            if (nextToken.Type != DsonTokenType.Comma) {
                PushToken(nextToken);
            }
        }

        // object/header 需要先读取 name和冒号，但object可能出现header
        if (context._contextType == DsonContextType.Object || context._contextType == DsonContextType.Header) {
            DsonToken nameToken = PopToken();
            switch (nameToken.Type) {
                case DsonTokenType.String:
                case DsonTokenType.UnquoteString: {
                    PushNextName(nameToken.CastAsString());
                    break;
                }
                case DsonTokenType.BeginHeader: {
                    if (context._contextType == DsonContextType.Header) {
                        throw DsonIOException.ContainsHeaderDirectly(nameToken);
                    }
                    EnsureCountIsZero(context, nameToken);
                    PushNextValue(nameToken);
                    return DsonType.Header;
                }
                case DsonTokenType.EndObject: {
                    return DsonType.EndOfObject;
                }
                default: {
                    throw DsonIOException.InvalidTokenType(context._contextType, nameToken,
                        DsonInternals.NewList(DsonTokenType.String, DsonTokenType.UnquoteString, DsonTokenType.EndObject));
                }
            }
            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);
        }

        // 走到这里，表示 top/object/header/array 读值uco
        DsonToken valueToken = PopToken();
        switch (valueToken.Type) {
            case DsonTokenType.Int32: {
                PushNextValue(valueToken.Value);
                return DsonType.Int32;
            }
            case DsonTokenType.Int64: {
                PushNextValue(valueToken.Value);
                return DsonType.Int64;
            }
            case DsonTokenType.Float: {
                PushNextValue(valueToken.Value);
                return DsonType.Float;
            }
            case DsonTokenType.Double: {
                PushNextValue(valueToken.Value);
                return DsonType.Double;
            }
            case DsonTokenType.Bool: {
                PushNextValue(valueToken.Value);
                return DsonType.Boolean;
            }
            case DsonTokenType.String: {
                PushNextValue(valueToken.CastAsString());
                return DsonType.String;
            }
            case DsonTokenType.Null: {
                PushNextValue(DsonNull.Null);
                return DsonType.Null;
            }
            case DsonTokenType.UnquoteString: return ParseUnquoteStringToken(context, valueToken);
            case DsonTokenType.BeginObject: return ParseBeginObjectToken(context, valueToken);
            case DsonTokenType.BeginArray: return ParseBeginArrayToken(context, valueToken);
            case DsonTokenType.ClassName: return ParseClassNameToken(context, valueToken);
            case DsonTokenType.BeginHeader: {
                // object的header已经处理，这里只有topLevel和array可以再出现header
                if (context._contextType.IsLikeObject()) {
                    throw DsonIOException.InvalidTokenType(context._contextType, valueToken);
                }
                EnsureCountIsZero(context, valueToken);
                PushNextValue(valueToken);
                return DsonType.Header;
            }
            case DsonTokenType.EndArray: {
                // endArray 只能在数组上下文出现；Array是在读取下一个值的时候结束；而Object必须在读取下一个name的时候结束
                if (context._contextType == DsonContextType.Array) {
                    return DsonType.EndOfObject;
                }
                throw DsonIOException.InvalidTokenType(context._contextType, valueToken);
            }
            case DsonTokenType.Eof: {
                // eof 只能在顶层上下文出现
                if (context._contextType == DsonContextType.TopLevel) {
                    return DsonType.EndOfObject;
                }
                throw DsonIOException.InvalidTokenType(context._contextType, valueToken);
            }
            default: {
                throw DsonIOException.InvalidTokenType(context._contextType, valueToken);
            }
        }
    }

    /** 字符串默认解析规则 */
    private DsonType ParseUnquoteStringToken(Context context, DsonToken valueToken) {
        string unquotedString = valueToken.CastAsString();
        if (context._contextType != DsonContextType.Header && context._compClsNameToken != null) {
            switch (context._compClsNameToken.CastAsString()) {
                case DsonTexts.LabelInt32: {
                    PushNextValue(DsonTexts.ParseInt(unquotedString));
                    return DsonType.Int32;
                }
                case DsonTexts.LabelInt64: {
                    PushNextValue(DsonTexts.ParseLong(unquotedString));
                    return DsonType.Int64;
                }
                case DsonTexts.LabelFloat: {
                    PushNextValue(DsonTexts.ParseFloat(unquotedString));
                    return DsonType.Float;
                }
                case DsonTexts.LabelDouble: {
                    PushNextValue(DsonTexts.ParseDouble(unquotedString));
                    return DsonType.Double;
                }
                case DsonTexts.LabelBool: {
                    PushNextValue(DsonTexts.ParseBool(unquotedString));
                    return DsonType.Boolean;
                }
                case DsonTexts.LabelString: {
                    PushNextValue(unquotedString);
                    return DsonType.String;
                }
            }
        }
        // 处理header的特殊属性依赖
        if (context._contextType == DsonContextType.Header) {
            switch (_nextName) {
                case DsonHeaders.NamesClassName:
                case DsonHeaders.NamesCompClassName:
                case DsonHeaders.NamesLocalId: {
                    PushNextValue(unquotedString);
                    return DsonType.String;
                }
            }
        }
        // 处理特殊值解析
        bool trueString = "true" == unquotedString;
        if (trueString || "false" == unquotedString) {
            PushNextValue(trueString);
            return DsonType.Boolean;
        }
        if ("null" == (unquotedString)) {
            PushNextValue(DsonNull.Null);
            return DsonType.Null;
        }
        if (DsonTexts.IsParsable(unquotedString)) {
            PushNextValue(DsonTexts.ParseDouble(unquotedString));
            return DsonType.Double;
        }
        PushNextValue(unquotedString);
        return DsonType.String;
    }

    /** 处理内置结构体的单值语法糖 */
    private DsonType ParseClassNameToken(Context context, in DsonToken valueToken) {
        // 1.className不能出现在topLevel，topLevel只能出现header结构体 @{}
        if (context._contextType == DsonContextType.TopLevel) {
            throw DsonIOException.InvalidTokenType(context._contextType, valueToken);
        }
        // 2.object和array的className会在beginObject和beginArray的时候转换为结构体 @{}
        // 因此这里只能出现内置结构体的简写形式
        string clsName = valueToken.CastAsString();
        if (DsonTexts.LabelReference == clsName) { // @ref localId
            DsonToken nextToken = PopToken();
            EnsureStringsToken(context, nextToken);
            PushNextValue(new ObjectRef(nextToken.CastAsString()));
            return DsonType.Reference;
        }
        if (DsonTexts.LabelDatetime == clsName) { // @dt uuuu-MM-dd'T'HH:mm:ss
            DateTime dateTime = OffsetTimestamp.ParseDateTime(ScanStringUtilComma());
            PushNextValue(new OffsetTimestamp(dateTime.ToEpochSeconds()));
            return DsonType.Timestamp;
        }
        throw DsonIOException.InvalidTokenType(context._contextType, valueToken);
    }

    /** 处理内置结构体 */
    private DsonType ParseBeginObjectToken(Context context, in DsonToken valueToken) {
        DsonToken? headerToken;
        if (valueToken.LastChar() == '@') { // {@
            headerToken = PopToken();
            EnsureHeadersToken(context, headerToken);
        }
        else if (context._contextType != DsonContextType.Header) {
            headerToken = context._compClsNameToken;
        }
        else {
            headerToken = null;
        }
        if (headerToken == null) {
            PushNextValue(valueToken);
            return DsonType.Object;
        }

        // 内置结构体
        string clsName = headerToken.CastAsString();
        switch (clsName) {
            case DsonTexts.LabelReference: {
                PushNextValue(ScanRef(context));
                return DsonType.Reference;
            }
            case DsonTexts.LabelDatetime: {
                PushNextValue(ScanTimestamp(context));
                return DsonType.Timestamp;
            }
            default: {
                EscapeHeaderAndPush(headerToken);
                PushNextValue(valueToken); // push以供context保存
                return DsonType.Object;
            }
        }
    }

    /** 处理内置二元组 */
    private DsonType ParseBeginArrayToken(Context context, in DsonToken valueToken) {
        DsonToken? headerToken;
        if (valueToken.LastChar() == '@') { // [@
            headerToken = PopToken();
            EnsureHeadersToken(context, headerToken);
        }
        else if (context._contextType != DsonContextType.Header) {
            headerToken = context._compClsNameToken;
        }
        else {
            headerToken = null;
        }
        if (headerToken == null) {
            PushNextValue(valueToken);
            return DsonType.Array;
        }
        // 内置元组
        switch (headerToken.CastAsString()) {
            case DsonTexts.LabelBinary: {
                Tuple2 tuple2 = ScanTuple2(context);
                byte[] data = Convert.FromHexString(tuple2.Value);
                PushNextValue(new DsonBinary(tuple2.Type, data));
                return DsonType.Binary;
            }
            case DsonTexts.LabelExtInt32: {
                Tuple2 tuple2 = ScanTuple2(context);
                bool hasValue = !tuple2.IsUnquoteNull();
                int value = hasValue ? DsonTexts.ParseInt(tuple2.Value) : 0;
                PushNextValue(new DsonExtInt32(tuple2.Type, value, hasValue));
                return DsonType.ExtInt32;
            }
            case DsonTexts.LabelExtInt64: {
                Tuple2 tuple2 = ScanTuple2(context);
                bool hasValue = !tuple2.IsUnquoteNull();
                long value = hasValue ? DsonTexts.ParseLong(tuple2.Value) : 0;
                PushNextValue(new DsonExtInt64(tuple2.Type, value, hasValue));
                return DsonType.ExtInt64;
            }
            case DsonTexts.LabelExtDouble: {
                Tuple2 tuple2 = ScanTuple2(context);
                bool hasValue = !tuple2.IsUnquoteNull();
                double value = hasValue ? DsonTexts.ParseDouble(tuple2.Value) : 0;
                PushNextValue(new DsonExtDouble(tuple2.Type, value, hasValue));
                return DsonType.ExtDouble;
            }
            case DsonTexts.LabelExtString: {
                Tuple2 tuple2 = ScanTuple2(context);
                string? value = tuple2.IsUnquoteNull() ? null : tuple2.Value;
                PushNextValue(new DsonExtString(tuple2.Type, value));
                return DsonType.ExtString;
            }
            default: {
                EscapeHeaderAndPush(headerToken);
                PushNextValue(valueToken); // push以供context保存
                return DsonType.Array;
            }
        }
    }

    private void EscapeHeaderAndPush(DsonToken headerToken) {
        // 如果header不是结构体，则封装为结构体，注意...要反序压栈
        if (headerToken.Type == DsonTokenType.BeginHeader) {
            PushToken(headerToken);
        }
        else {
            PushToken(TokenEndObject);
            PushToken(new DsonToken(DsonTokenType.String, headerToken.CastAsString(), -1));
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
        while ((keyToken = PopToken()).Type != DsonTokenType.EndObject) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);

            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);

            // 根据name校验
            DsonToken valueToken = PopToken();
            switch (keyToken.CastAsString()) {
                case ObjectRef.NamesNamespace: {
                    EnsureStringsToken(context, valueToken);
                    ns = valueToken.CastAsString();
                    break;
                }
                case ObjectRef.NamesLocalId: {
                    EnsureStringsToken(context, valueToken);
                    localId = valueToken.CastAsString();
                    break;
                }
                case ObjectRef.NamesType: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UnquoteString);
                    type = DsonTexts.ParseInt(valueToken.CastAsString());
                    break;
                }
                case ObjectRef.NamesPolicy: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UnquoteString);
                    policy = DsonTexts.ParseInt(valueToken.CastAsString());
                    break;
                }
                default: {
                    throw new DsonIOException("invalid ref fieldName: " + keyToken.CastAsString());
                }
            }
            CheckSeparator(context);
        }
        return new ObjectRef(localId, ns, type, policy);
    }

    private OffsetTimestamp ScanTimestamp(Context context) {
        DateOnly date = DsonInternals.UtcEpochDate;
        TimeOnly time = TimeOnly.MinValue;
        int nanos = 0;
        int offset = 0;
        int enables = 0;
        DsonToken keyToken;
        while ((keyToken = PopToken()).Type != DsonTokenType.EndObject) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);

            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);

            // 根据name校验
            switch (keyToken.CastAsString()) {
                case OffsetTimestamp.NamesDate: {
                    string dateString = ScanStringUtilComma();
                    date = OffsetTimestamp.ParseDate(dateString);
                    enables |= OffsetTimestamp.MaskDate;
                    break;
                }
                case OffsetTimestamp.NamesTime: {
                    string timeString = ScanStringUtilComma();
                    time = OffsetTimestamp.ParseTime(timeString);
                    enables |= OffsetTimestamp.MaskTime;
                    break;
                }
                case OffsetTimestamp.NamesOffset: {
                    string offsetString = ScanStringUtilComma();
                    offset = OffsetTimestamp.ParseOffset(offsetString);
                    enables |= OffsetTimestamp.MaskOffset;
                    break;
                }
                case OffsetTimestamp.NamesNanos: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    nanos = DsonTexts.ParseInt(valueToken.CastAsString());
                    enables |= OffsetTimestamp.MaskNanos;
                    break;
                }
                case OffsetTimestamp.NamesMillis: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    nanos = DsonTexts.ParseInt(valueToken.CastAsString()) * 1000_000;
                    enables |= OffsetTimestamp.MaskNanos;
                    break;
                }
                default: {
                    throw new DsonIOException("invalid datetime fieldName: " + keyToken.CastAsString());
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
                case DsonTokenType.Comma:
                case DsonTokenType.EndObject:
                case DsonTokenType.EndArray: {
                    PushToken(valueToken);
                    return sb.ToString();
                }
                case DsonTokenType.String:
                case DsonTokenType.UnquoteString:
                case DsonTokenType.Colon: {
                    sb.Append(valueToken.CastAsString());
                    break;
                }
                default: {
                    throw DsonIOException.InvalidTokenType(ContextType, valueToken);
                }
            }
        }
    }

    private void CheckSeparator(Context context) {
        // 每读取一个值，判断下分隔符，尾部最多只允许一个逗号 -- 这里在尾部更容易处理
        DsonToken keyToken;
        if ((keyToken = PopToken()).Type == DsonTokenType.Comma
            && (keyToken = PopToken()).Type == DsonTokenType.Comma) {
            throw DsonIOException.InvalidTokenType(context._contextType, keyToken);
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
        VerifyTokenType(context, nextToken, DsonTokenType.UnquoteString);
        int type = DsonTexts.ParseInt(nextToken.CastAsString());

        nextToken = PopToken();
        VerifyTokenType(context, nextToken, DsonTokenType.Comma);

        nextToken = PopToken();
        EnsureStringsToken(context, nextToken);
        string value = nextToken.CastAsString();

        nextToken = PopToken();
        VerifyTokenType(context, nextToken, DsonTokenType.EndArray);
        return new Tuple2(type, value);
    }

    private readonly struct Tuple2
    {
        internal readonly int Type;
        internal readonly string Value;

        public Tuple2(int type, string value) {
            this.Type = type;
            this.Value = value;
        }

        internal bool IsUnquoteNull() {
            return "null" == Value;
        }
    }

    #endregion

    /** header不可以在中途出现 */
    private static void EnsureCountIsZero(Context context, DsonToken headerToken) {
        if (context._count > 0) {
            throw DsonIOException.InvalidTokenType(context._contextType, headerToken,
                DsonInternals.NewList(DsonTokenType.String, DsonTokenType.UnquoteString, DsonTokenType.EndObject));
        }
    }

    private static void EnsureStringsToken(Context context, DsonToken token) {
        if (token.Type != DsonTokenType.String && token.Type != DsonTokenType.UnquoteString) {
            throw DsonIOException.InvalidTokenType(context._contextType, token,
                DsonInternals.NewList(DsonTokenType.String, DsonTokenType.UnquoteString));
        }
    }

    private static void EnsureHeadersToken(Context context, DsonToken token) {
        if (token.Type != DsonTokenType.BeginHeader && token.Type != DsonTokenType.ClassName) {
            throw DsonIOException.InvalidTokenType(context._contextType, token,
                DsonInternals.NewList(DsonTokenType.BeginHeader, DsonTokenType.ClassName));
        }
    }

    private static void VerifyTokenType(Context context, DsonToken token, DsonTokenType expected) {
        if (token.Type != expected) {
            throw DsonIOException.InvalidTokenType(context._contextType, token, DsonInternals.NewList(expected));
        }
    }

    private static void VerifyTokenType(Context context, DsonToken token, List<DsonTokenType> expected) {
        if (!expected.Contains(token.Type)) {
            throw DsonIOException.InvalidTokenType(context._contextType, token, expected);
        }
    }

    protected override void DoReadName() {
        _currentName = PopNextName();
        // 将header中的特殊属性记录下来
        Context context = GetContext();
        if (context._contextType == DsonContextType.Header) {
            if (DsonHeaders.NamesCompClassName == _currentName) {
                context._compClsNameToken = new DsonToken(DsonTokenType.ClassName, _nextValue, -1);
            }
            // else 其它属性
        }
    }

    #endregion

    #region 简单值

    protected override int DoReadInt32() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToInt32(NumberFormatInfo.InvariantInfo);
    }

    protected override long DoReadInt64() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToInt64(CultureInfo.InvariantCulture);
    }

    protected override float DoReadFloat() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToSingle(NumberFormatInfo.InvariantInfo);
    }

    protected override double DoReadDouble() {
        IConvertible number = (IConvertible)PopNextValue();
        return number.ToDouble(NumberFormatInfo.InvariantInfo);
    }

    protected override bool DoReadBool() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (bool)value;
    }

    protected override string DoReadString() {
        string value = (string)PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return value;
    }

    protected override void DoReadNull() {
        PopNextValue();
    }

    protected override DsonBinary DoReadBinary() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonBinary)value;
    }

    protected override DsonExtInt32 DoReadExtInt32() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtInt32)value;
    }

    protected override DsonExtInt64 DoReadExtInt64() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtInt64)value;
    }

    protected override DsonExtDouble DoReadExtDouble() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtDouble)value;
    }

    protected override DsonExtString DoReadExtString() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (DsonExtString)value;
    }

    protected override ObjectRef DoReadRef() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (ObjectRef)value;
    }

    protected override OffsetTimestamp DoReadTimestamp() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (OffsetTimestamp)value;
    }

    #endregion

    #region 容器

    protected override void DoReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        newContext._beginToken = (DsonToken)(PopNextValue() ?? throw new InvalidOperationException());
        newContext._name = _currentName;

        this._recursionDepth++;
        SetContext(newContext);
    }

    protected override void DoReadEndContainer() {
        Context context = GetContext();
        // header中的信息是修饰外层对象的
        if (context._contextType == DsonContextType.Header) {
            context.Parent._compClsNameToken = context._compClsNameToken;
        }

        // 恢复上下文
        RecoverDsonType(context);
        this._recursionDepth--;
        SetContext(context._parent);
        PoolContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoSkipName() {
        // 名字早已读取
        PopNextName();
    }

    protected override void DoSkipValue() {
        PopNextValue();
        switch (_currentDsonType) {
            case DsonType.Header:
            case DsonType.Object:
            case DsonType.Array: {
                SkipStack(1);
                break;
            }
        }
    }

    protected override void DoSkipToEndOfObject() {
        DsonToken endToken;
        if (IsAtType) {
            endToken = SkipStack(1);
        }
        else {
            SkipName();
            switch (_currentDsonType) {
                case DsonType.Header:
                case DsonType.Object:
                case DsonType.Array: { // 嵌套对象
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
                case DsonTokenType.BeginArray:
                case DsonTokenType.BeginObject:
                case DsonTokenType.BeginHeader: {
                    stack++;
                    break;
                }
                case DsonTokenType.EndArray:
                case DsonTokenType.EndObject: {
                    if (--stack == 0) {
                        return token;
                    }
                    break;
                }
                case DsonTokenType.Eof: {
                    throw DsonIOException.InvalidTokenType(ContextType, token);
                }
            }
        }
        throw new InvalidOperationException("Assert Exception");
    }

    protected override byte[] DoReadValueAsBytes() {
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
        context.Init(parent, contextType, dsonType);
        return context;
    }

    private void PoolContext(Context context) {
        context.Reset();
        SetPooledContext(context);
    }

    protected new class Context : AbstractDsonReader<string>.Context
    {
#nullable disable
        internal DsonToken _beginToken;
#nullable enable

        /** header只可触发一次流程 */
        internal int _headerCount = 0;
        /** 元素计数，判断冒号 */
        internal int _count;
        /** 数组/Object成员的类型 - token类型可直接复用；header的该属性是用于注释外层对象的 */
        internal DsonToken? _compClsNameToken;

        public Context() {
        }

        public override void Reset() {
            base.Reset();
            _beginToken = null;
            _headerCount = 0;
            _count = 0;
            _compClsNameToken = null;
        }

        public new Context Parent => (Context)_parent;
    }

    #endregion
}