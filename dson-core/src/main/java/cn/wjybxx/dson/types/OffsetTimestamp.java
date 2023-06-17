package cn.wjybxx.dson.types;

import java.time.*;
import java.time.format.DateTimeFormatter;

/**
 * 有时区偏移的时间戳
 *
 * @author wjybxx
 * date - 2023/6/17
 */
public class OffsetTimestamp {

    public static final int MASK_DATE = 1 << 3;
    public static final int MASK_TIME = 1 << 2;
    public static final int MASK_OFFSET = 1 << 1;

    public static final int MASK_DATETIME = MASK_DATE | MASK_TIME;
    public static final int MASK_OFFSET_DATETIME = MASK_DATE | MASK_TIME | MASK_OFFSET;

    public static final String FIELDS_DATE = "date";
    public static final String FIELDS_TIME = "time";
    public static final String FIELDS_NANOS = "nanos";
    public static final String FIELDS_OFFSET = "offset";
    public static final String FIELDS_MILLIS = "millis";

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
        if (!isEnabled(MASK_DATE, enables) && !isEnabled(MASK_TIME, enables)) {
            throw new IllegalArgumentException("date and time are disabled");
        }
        if (nanos < 0) throw new IllegalArgumentException("nanos cant be negative");
        if (offset != 0 && !isEnabled(MASK_OFFSET, enables)) {
            throw new IllegalArgumentException("offset is disable, but the value is not 0");
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
        return isEnabled(MASK_DATE, enables);
    }

    public boolean hasTime() {
        return isEnabled(MASK_TIME, enables);
    }

    public boolean hasOffset() {
        return isEnabled(MASK_OFFSET, enables);
    }

    public boolean hasFields(int mask) {
        return (mask & enables) == mask;
    }

    private static boolean isEnabled(int mask, int value) {
        return (mask & value) == mask;
    }

    public boolean isSimpleDateTime() {
        return enables == MASK_DATETIME;
    }

    public boolean canConvertNanosToMillis() {
        return (nanos % 1000000) == 0;
    }

    public int getMillisOfNanos() {
        return nanos / 1000000;
    }

    public OffsetDateTime toOffsetDateTime() {
        if (!hasDate() || !hasTime()) {
            throw new IllegalStateException("this timestamp is not a datetime");
        }
        return OffsetDateTime.ofInstant(Instant.ofEpochSecond(seconds, nanos),
                ZoneOffset.ofTotalSeconds(offset));
    }

    public static String formatDate(long seconds) {
        return LocalDateTime.ofEpochSecond(seconds, 0, ZoneOffset.UTC)
                .toLocalDate()
                .toString();
    }

    public static String formatTime(long seconds) {
        return LocalDateTime.ofEpochSecond(seconds, 1, ZoneOffset.UTC)
                .toLocalTime()
                .toString()
                .substring(0, 8);
    }

    public static String formatOffset(int offset) {
        String pre = offset < 0 ? "-" : "+";
        return pre + LocalTime.ofSecondOfDay(offset).toString();
    }

    public static LocalDate parseDate(String dateString) {
        if (dateString.length() != 10) throw new IllegalArgumentException("invalid dateString " + dateString);
        return LocalDate.parse(dateString, DateTimeFormatter.ISO_DATE);
    }

    public static LocalTime parseTime(String timeString) {
        if (timeString.length() != 8) throw new IllegalArgumentException("invalid timeString " + timeString);
        return LocalTime.parse(timeString, DateTimeFormatter.ISO_TIME);
    }

    public static int parseOffset(String offsetString) {
        if (offsetString.charAt(0) == '-') {
            return -1 * LocalTime.parse(offsetString.substring(1)).toSecondOfDay();
        }
        if (offsetString.charAt(0) == '+') {
            return LocalTime.parse(offsetString.substring(1)).toSecondOfDay();
        }
        throw new IllegalArgumentException("invalid offset string " + offsetString);
    }

    // endregion

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

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("OffsetTimestamp{");
        if (hasDate()) {
            sb.append("date: '").append(formatDate(seconds))
                    .append("', ");
        }
        if (hasTime()) {
            sb.append("time: '").append(formatTime(seconds))
                    .append("', ");
        }
        if (nanos != 0) {
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
}