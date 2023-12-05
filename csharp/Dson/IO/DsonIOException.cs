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

using System.Runtime.Serialization;
using Dson.Text;

namespace Dson.IO;

public class DsonIOException : Exception
{
    public DsonIOException() {
    }

    protected DsonIOException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public DsonIOException(string? message) : base(message) {
    }

    public DsonIOException(string? message, Exception? innerException) : base(message, innerException) {
    }

    public static DsonIOException wrap(Exception e) {
        if (e is DsonIOException exception) {
            return exception;
        }
        return new DsonIOException(null, e);
    }

    // reader/writer
    public static DsonIOException recursionLimitExceeded() {
        return new DsonIOException("Object had too many levels of nesting.");
    }

    public static DsonIOException contextError(DsonContextType expected, DsonContextType contextType) {
        return new DsonIOException($"context error, expected {expected}, but found {contextType}");
    }

    public static DsonIOException contextError(IList<DsonContextType> expected, DsonContextType contextType) {
        return new DsonIOException($"context error, expected {expected}, but found {contextType}");
    }

    public static DsonIOException contextErrorTopLevel() {
        return new DsonIOException("context error, current state is TopLevel");
    }

    public static DsonIOException unexpectedName(int expected, int name) {
        return new DsonIOException($"The number of the field does not match, expected {expected}, but found {name}");
    }

    public static DsonIOException unexpectedName(String expected, String name) {
        return new DsonIOException($"The name of the field does not match, expected {expected}, but found {name}");
    }

    public static DsonIOException dsonTypeMismatch(DsonType expected, DsonType dsonType) {
        return new DsonIOException($"The dsonType does not match, expected {expected}, but found {dsonType}");
    }

    public static DsonIOException invalidDsonType(IList<DsonType> dsonTypeList, DsonType dsonType) {
        return new DsonIOException($"The dson type is invalid in context, context: {dsonTypeList}, dsonType: {dsonType}");
    }

    public static DsonIOException invalidDsonType(DsonContextType contextType, DsonType dsonType) {
        return new DsonIOException($"The dson type is invalid in context, context: {contextType}, dsonType: {dsonType}");
    }

    public static DsonIOException unexpectedSubType(int expected, int subType) {
        return new DsonIOException($"Unexpected subType, expected {expected}, but found {subType}");
    }

    public static DsonIOException invalidState(DsonContextType contextType, IList<DsonReaderState> expected, DsonReaderState state) {
        return new DsonIOException($"invalid state, contextType {contextType}, expected {expected}, but found {state}.");
    }

    public static DsonIOException invalidState(DsonContextType contextType, IList<DsonWriterState> expected, DsonWriterState state) {
        return new DsonIOException($"invalid state, contextType {contextType}, expected {expected}, but found {state}.");
    }

    public static DsonIOException bytesRemain(int bytesUntilLimit) {
        return new DsonIOException("bytes remain " + bytesUntilLimit);
    }

    public static DsonIOException containsHeaderDirectly(DsonToken token) {
        return new DsonIOException($"header contains another header directly, token {token}.");
    }

    public static DsonIOException invalidTokenType(DsonContextType contextType, DsonToken token) {
        return new DsonIOException($"invalid token, contextType {contextType}, token {token}.");
    }

    public static DsonIOException invalidTokenType(DsonContextType contextType, DsonToken token, IList<TokenType> expected) {
        return new DsonIOException($"invalid token, contextType {contextType}, expected {expected}, but found {token}.");
    }

    // endregion
}