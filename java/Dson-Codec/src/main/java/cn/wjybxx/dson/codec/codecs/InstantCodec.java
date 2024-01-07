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
import java.time.Instant;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class InstantCodec implements DuplexCodec<Instant> {

    @Nonnull
    @Override
    public Class<Instant> getEncoderClass() {
        return Instant.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, Instant instance, TypeArgInfo<?> typeArgInfo) {
        writer.writeTimestamp(writer.getCurrentName(), OffsetTimestamp.ofInstant(instance));
    }

    @Override
    public Instant readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return Instant.ofEpochSecond(timestamp.getSeconds(), timestamp.getNanos());
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Instant instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeTimestamp(writer.getCurrentName(), OffsetTimestamp.ofInstant(instance));
    }

    @Override
    public Instant readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return Instant.ofEpochSecond(timestamp.getSeconds(), timestamp.getNanos());
    }
}