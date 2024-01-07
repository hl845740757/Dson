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
import java.time.Duration;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class DurationCodec implements DuplexCodec<Duration> {

    @Nonnull
    @Override
    public Class<Duration> getEncoderClass() {
        return Duration.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, Duration instance, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = toTimestamp(instance);
        writer.writeTimestamp(writer.getCurrentName(), timestamp);
    }

    @Override
    public Duration readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return Duration.ofSeconds(timestamp.getSeconds(), timestamp.getNanos());
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Duration instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        OffsetTimestamp timestamp = toTimestamp(instance);
        writer.writeTimestamp(writer.getCurrentName(), timestamp);
    }

    @Override
    public Duration readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return Duration.ofSeconds(timestamp.getSeconds(), timestamp.getNanos());
    }


    private static OffsetTimestamp toTimestamp(Duration instance) {
        return new OffsetTimestamp(instance.getSeconds(), instance.getNano(), 0, OffsetTimestamp.MASK_INSTANT);
    }
}