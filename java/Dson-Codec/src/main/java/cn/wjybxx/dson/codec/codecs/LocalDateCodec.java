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
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneOffset;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class LocalDateCodec implements DuplexCodec<LocalDate> {

    @Nonnull
    @Override
    public Class<LocalDate> getEncoderClass() {
        return LocalDate.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, LocalDate instance, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = toTimestamp(instance);
        writer.writeTimestamp(writer.getCurrentName(), timestamp);
    }

    @Override
    public LocalDate readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return ofTimestamp(timestamp);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, LocalDate instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        OffsetTimestamp timestamp = toTimestamp(instance);
        writer.writeTimestamp(writer.getCurrentName(), timestamp);
    }

    @Override
    public LocalDate readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return ofTimestamp(timestamp);
    }

    private static LocalDate ofTimestamp(OffsetTimestamp timestamp) {
        LocalDateTime localDateTime = LocalDateTime.ofEpochSecond(timestamp.getSeconds(), 0, ZoneOffset.UTC);
        return localDateTime.toLocalDate();
    }

    private static OffsetTimestamp toTimestamp(LocalDate localDate) {
        long epochSecond = localDate.toEpochSecond(LocalTime.MIN, ZoneOffset.UTC);
        return new OffsetTimestamp(epochSecond, 0, 0, OffsetTimestamp.MASK_DATE);
    }
}