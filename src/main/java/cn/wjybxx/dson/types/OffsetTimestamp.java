package cn.wjybxx.dson.types;

import cn.wjybxx.dson.DsonLites;
import cn.wjybxx.dson.internal.InternalUtils;

import javax.annotation.concurrent.Immutable;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;

/**
 * 有时区偏移的时间戳
 *
 * @author wjybxx
 * date - 2023/6/17
 */
@Immutable
public final class OffsetTimestamp {

    public static final int MASK_DATE = 1;
    public static final int MASK_TIME = 1 << 1;
    public static final int MASK_OFFSET = 1 << 2;
    public static final int MASK_NANOS = 1 << 3;

    public static final int MASK_DATETIME = MASK_DATE | MASK_TIME;
    public static final int MASK_OFFSET_DATETIME = MASK_DATE | MASK_TIME | MASK_OFFSET;

    private final long seconds;
    private final int nanos;
    private final int offset;
    private final int enables;

    public OffsetTimestamp(long seconds) {
        this(seconds, 0, 0, MASK_DATETIME);
    }

    /**
     * @param seconds 纪元时间-秒
     * @param nanos   纪元时间的纳秒部分
     * @param offset  时区偏移-秒
     * @param enables 哪些字段有效
     */
    public OffsetTimestamp(long seconds, int nanos, int offset, int enables) {
        if (InternalUtils.isDisabled(enables, MASK_DATE) && InternalUtils.isDisabled(enables, MASK_TIME)) {
            throw new IllegalArgumentException("date and time are disabled");
        }
        if (offset != 0 && InternalUtils.isDisabled(enables, MASK_OFFSET)) {
            throw new IllegalArgumentException("offset is disabled, but the value is not 0");
        }
        if (nanos != 0 && InternalUtils.isDisabled(enables, MASK_NANOS)) {
            throw new IllegalArgumentException("nanos is disabled, but the value is not 0");
        }
        if (nanos > 999_999_999 || nanos < 0) {
            throw new IllegalArgumentException("nanos > 999999999 or < 0");
        }
        this.seconds = seconds;
        this.nanos = nanos;
        this.offset = offset;
        this.enables = enables;
    }

    public long getSeconds() {
        return seconds;
    }

    public int getNanos() {
        return nanos;
    }

    public int getOffset() {
        return offset;
    }

    public int getEnables() {
        return enables;
    }

    // region

    public boolean hasDate() {
        return InternalUtils.isEnabled(enables, MASK_DATE);
    }

    public boolean hasTime() {
        return InternalUtils.isEnabled(enables, MASK_TIME);
    }

    public boolean hasOffset() {
        return InternalUtils.isEnabled(enables, MASK_OFFSET);
    }

    public boolean hasNanos() {
        return InternalUtils.isEnabled(enables, MASK_NANOS);
    }

    public boolean hasFields(int mask) {
        return InternalUtils.isEnabled(enables, mask);
    }

    public boolean canConvertNanosToMillis() {
        return (nanos % 1000_000) == 0;
    }

    public int getMillisOfNanos() {
        return nanos / 1000_000;
    }

    /** @return 固定格式 yyyy-MM-dd */
    public static String formatDate(long seconds) {
        return LocalDateTime.ofEpochSecond(seconds, 0, ZoneOffset.UTC)
                .toLocalDate()
                .toString();
    }

    /** @return 固定格式 HH:mm:ss */
    public static String formatTime(long seconds) {
        return LocalDateTime.ofEpochSecond(seconds, 1, ZoneOffset.UTC)
                .toLocalTime()
                .toString()
                .substring(0, 8);
    }

    /** @return 固定格式 yyyy-MM-dd'T'HH:mm:ss */
    public static String formatDateTime(long seconds) {
        return formatDate(seconds) + "T" + formatTime(seconds);
    }

    /**
     * Z
     * +HH:mm
     * +HH:mm:ss
     */
    public static String formatOffset(int offset) {
        if (offset == 0) {
            return "Z";
        }
        String pre = offset < 0 ? "-" : "+";
        return pre + LocalTime.ofSecondOfDay(offset).toString();
    }

    /** @param dateString 限定格式 yyyy-MM-dd */
    public static LocalDate parseDate(String dateString) {
//        if (dateString.length() != 10) throw new IllegalArgumentException("invalid dateString " + dateString);
        return LocalDate.parse(dateString, DateTimeFormatter.ISO_DATE);
    }

    /** @param timeString 限定格式 HH:mm:ss */
    public static LocalTime parseTime(String timeString) {
        if (timeString.length() != 8) throw new IllegalArgumentException("invalid timeString " + timeString);
        return LocalTime.parse(timeString, DateTimeFormatter.ISO_TIME);
    }

    /** @param timeString 限定格式 yyyy-MM-dd'T'HH:mm:ss */
    public static LocalDateTime parseDateTime(String timeString) {
        return LocalDateTime.parse(timeString, DateTimeFormatter.ISO_DATE_TIME);
    }

    /**
     * Z
     * +H
     * +HH
     * +HH:mm
     * +HH:mm:ss
     */
    public static int parseOffset(String offsetString) {
        return switch (offsetString.length()) {
            case 1, 2, 3, 6, 9 -> ZoneOffset.of(offsetString).getTotalSeconds();
            default -> throw new IllegalArgumentException("invalid offset string " + offsetString);
        };
    }

    // endregion

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        OffsetTimestamp offsetTimestamp = (OffsetTimestamp) o;

        if (seconds != offsetTimestamp.seconds) return false;
        if (nanos != offsetTimestamp.nanos) return false;
        if (offset != offsetTimestamp.offset) return false;
        return enables == offsetTimestamp.enables;
    }

    @Override
    public int hashCode() {
        int result = (int) (seconds ^ (seconds >>> 32));
        result = 31 * result + nanos;
        result = 31 * result + offset;
        result = 31 * result + enables;
        return result;
    }
    // endregion

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("OffsetTimestamp{");
        if (hasDate()) {
            sb.append("date: '").append(formatDate(seconds));
        }
        if (hasTime()) {
            if (hasDate()) {
                sb.append(", ");
            }
            sb.append("time: '").append(formatTime(seconds));
        }
        if (nanos != 0) {
            sb.append(", ");
            if (canConvertNanosToMillis()) {
                sb.append("millis: ").append(getMillisOfNanos());
            } else {
                sb.append("nanos: ").append(nanos);
            }
        }
        if (hasOffset()) {
            sb.append(", ");
            sb.append("offset: '").append(formatOffset(offset))
                    .append("'");
        }
        return sb.append('}')
                .toString();
    }

    //
    public static final String NAMES_DATE = "date";
    public static final String NAMES_TIME = "time";
    public static final String NAMES_MILLIS = "millis";

    public static final String NAMES_SECONDS = "seconds";
    public static final String NAMES_NANOS = "nanos";
    public static final String NAMES_OFFSET = "offset";
    public static final String NAMES_ENABLES = "enables";

    public static final int NUMBERS_SECONDS = DsonLites.makeFullNumberZeroIdep(0);
    public static final int NUMBERS_NANOS = DsonLites.makeFullNumberZeroIdep(1);
    public static final int NUMBERS_OFFSET = DsonLites.makeFullNumberZeroIdep(2);
    public static final int NUMBERS_ENABLES = DsonLites.makeFullNumberZeroIdep(3);
}