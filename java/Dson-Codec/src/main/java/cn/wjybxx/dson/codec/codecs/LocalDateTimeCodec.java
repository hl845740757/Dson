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
import java.time.LocalDateTime;
import java.time.ZoneOffset;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class LocalDateTimeCodec implements DuplexCodec<LocalDateTime> {

    @Nonnull
    @Override
    public Class<LocalDateTime> getEncoderClass() {
        return LocalDateTime.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, LocalDateTime instance, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = OffsetTimestamp.ofDateTime(instance);
        writer.writeTimestamp(writer.getCurrentName(), timestamp);
    }

    @Override
    public LocalDateTime readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return LocalDateTime.ofEpochSecond(timestamp.getSeconds(), timestamp.getNanos(), ZoneOffset.UTC);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, LocalDateTime instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        OffsetTimestamp timestamp = OffsetTimestamp.ofDateTime(instance);
        writer.writeTimestamp(writer.getCurrentName(), timestamp);
    }

    @Override
    public LocalDateTime readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return LocalDateTime.ofEpochSecond(timestamp.getSeconds(), timestamp.getNanos(), ZoneOffset.UTC);
    }
}