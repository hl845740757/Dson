package cn.wjybxx.dson.codec.codecs;

import cn.wjybxx.dson.codec.DuplexCodec;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.dson.DsonCodecScanIgnore;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteCodecScanIgnore;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.OffsetTimestamp;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class TimestampCodec implements DuplexCodec<OffsetTimestamp> {

    @Nonnull
    @Override
    public Class<OffsetTimestamp> getEncoderClass() {
        return OffsetTimestamp.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, OffsetTimestamp instance, TypeArgInfo<?> typeArgInfo) {
        writer.writeTimestamp(writer.getCurrentName(), instance);
    }

    @Override
    public OffsetTimestamp readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readTimestamp(reader.getCurrentName());
    }

    @Override
    public void writeObject(DsonObjectWriter writer, OffsetTimestamp instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeTimestamp(writer.getCurrentName(), instance);
    }

    @Override
    public OffsetTimestamp readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readTimestamp(reader.getCurrentName());
    }
}