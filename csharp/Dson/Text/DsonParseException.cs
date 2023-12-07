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
using Dson.IO;

namespace Dson.Text;

public class DsonParseException : DsonIOException
{
    public DsonParseException() {
    }

    protected DsonParseException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public DsonParseException(string? message) : base(message) {
    }

    public DsonParseException(string? message, Exception? innerException) : base(message, innerException) {
    }

    public new static DsonParseException wrap(Exception e) {
        if (e is DsonParseException dsonParseException) {
            return dsonParseException;
        }
        return new DsonParseException(null, e);
    }
}