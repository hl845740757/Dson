package cn.wjybxx.dson.codec.codecs;

import cn.wjybxx.base.time.TimeUtils;
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
import java.time.LocalTime;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class LocalTimeCodec implements DuplexCodec<LocalTime> {

    @Nonnull
    @Override
    public Class<LocalTime> getEncoderClass() {
        return LocalTime.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, LocalTime instance, TypeArgInfo<?> typeArgInfo) {
        writer.writeTimestamp(writer.getCurrentName(), toTimestamp(instance));
    }

    @Override
    public LocalTime readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return ofTimestamp(timestamp);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, LocalTime instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeTimestamp(writer.getCurrentName(), toTimestamp(instance));
    }

    @Override
    public LocalTime readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        OffsetTimestamp timestamp = reader.readTimestamp(reader.getCurrentName());
        return ofTimestamp(timestamp);
    }

    private static LocalTime ofTimestamp(OffsetTimestamp timestamp) {
        long nanoOfDay = timestamp.getSeconds() * TimeUtils.NANOS_PER_SECOND + timestamp.getNanos();
        return LocalTime.ofNanoOfDay(nanoOfDay);
    }

    private static OffsetTimestamp toTimestamp(LocalTime localTime) {
        int mask = localTime.getNano() == 0
                ? OffsetTimestamp.MASK_TIME
                : (OffsetTimestamp.MASK_TIME | OffsetTimestamp.MASK_NANOS);
        return new OffsetTimestamp(localTime.toSecondOfDay(), localTime.getNano(), 0, mask);
    }
}