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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Google.Protobuf;

namespace Wjybxx.Dson.IO;

public class DsonOutputs
{
    public static IDsonOutput NewInstance(byte[] buffer) {
        return new ArrayDsonOutput(buffer, 0, buffer.Length);
    }

    public static IDsonOutput NewInstance(byte[] buffer, int offset, int length) {
        return new ArrayDsonOutput(buffer, offset, length);
    }

    private class ArrayDsonOutput : IDsonOutput
    {
        private readonly byte[] _buffer;
        private readonly int _rawOffset;
        private readonly int _rawLimit;

        private int _bufferPos;
        private int _bufferPosLimit;

        internal ArrayDsonOutput(byte[] buffer, int offset, int length) {
            BinaryUtils.CheckBuffer(buffer, offset, length);
            this._buffer = buffer;
            this._rawOffset = offset;
            this._rawLimit = offset + length;

            this._bufferPos = offset;
            this._bufferPosLimit = offset + length;
        }

        #region check

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CheckNewBufferPos(int newBufferPos) {
            if (newBufferPos < _rawOffset || newBufferPos > _bufferPosLimit) {
                throw new DsonIOException($"BytesLimited, LimitPos: {_bufferPosLimit}," +
                                          $" position: {_bufferPos}," +
                                          $" newPosition: {newBufferPos}");
            }
            return newBufferPos;
        }

        private int SpaceLeft => _bufferPosLimit - _bufferPos;

        #endregion

        public void WriteRawByte(byte value) {
            CheckNewBufferPos(_bufferPos + 1);
            _buffer[_bufferPos++] = value;
        }

        public void WriteInt32(int value) {
            try {
                int newPos = BinaryUtils.WriteInt32(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteUint32(int value) {
            try {
                int newPos = BinaryUtils.WriteUint32(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteSint32(int value) {
            try {
                int newPos = BinaryUtils.WriteSint32(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteFixed32(int value) {
            try {
                int newPos = BinaryUtils.WriteFixed32(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteInt64(long value) {
            try {
                int newPos = BinaryUtils.WriteInt64(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteUint64(long value) {
            try {
                int newPos = BinaryUtils.WriteUint64(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteSint64(long value) {
            try {
                int newPos = BinaryUtils.WriteSint64(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteFixed64(long value) {
            try {
                int newPos = BinaryUtils.WriteFixed64(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteFloat(float value) {
            try {
                int newPos = BinaryUtils.WriteFloat(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteDouble(double value) {
            try {
                int newPos = BinaryUtils.WriteDouble(_buffer, _bufferPos, value);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteBool(bool value) {
            try {
                int newPos = BinaryUtils.WriteUint32(_buffer, _bufferPos, value ? 1 : 0);
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public void WriteString(string value) {
            try {
                // 注意，这里写的编码后的字节长度；而不是字符串长度 -- 提前计算UTF8的长度是很有用的方法
                int byteCount = Encoding.UTF8.GetByteCount(value);
                int newPos = BinaryUtils.WriteUint32(_buffer, _bufferPos, byteCount);
                if (value.Length > 0) {
                    int lengthUtilLimit = _rawLimit - newPos;
                    Span<byte> span = new Span<byte>(_buffer, newPos, lengthUtilLimit);
                    int realByteCount = Encoding.UTF8.GetBytes(value, span);
                    Debug.Assert(byteCount == realByteCount);
                    newPos += byteCount;
                }
                _bufferPos = CheckNewBufferPos(newPos);
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e);
            }
        }

        public void WriteRawBytes(byte[] data, int offset, int length) {
            BinaryUtils.CheckBuffer(data, offset, length);
            CheckNewBufferPos(_bufferPos + length);

            Array.Copy(data, offset, _buffer, _bufferPos, length);
            _bufferPos += length;
        }

        public void WriteMessage(IMessage message) {
            using (CodedOutputStream stream = new CodedOutputStream(new MemoryStream(_buffer, _bufferPos, SpaceLeft))) {
                stream.WriteRawMessage(message);
                stream.Flush();
                _bufferPos += (int)stream.Position;
            }
        }

        public int Position {
            get => _bufferPos - _rawOffset;
            set {
                BinaryUtils.CheckBuffer(_rawLimit - _rawOffset, value);
                _bufferPos = _rawOffset + value;
            }
        }

        public void SetByte(int pos, byte value) {
            BinaryUtils.CheckBuffer(_rawLimit - _rawOffset, pos, 1);
            int bufferPos = _rawOffset + pos;
            _buffer[bufferPos] = value;
        }

        public void SetFixedInt32(int pos, int value) {
            BinaryUtils.CheckBuffer(_rawLimit - _rawOffset, pos, 4);
            int bufferPos = _rawOffset + pos;
            BinaryUtils.SetIntLe(_buffer, bufferPos, value);
        }

        public void Flush() {
        }

        public void Dispose() {
        }
    }
}