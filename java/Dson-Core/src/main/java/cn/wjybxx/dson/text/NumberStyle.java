package cn.wjybxx.dson.text;

import java.math.BigDecimal;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/19
 */
public enum NumberStyle implements INumberStyle {

    /**
     * 简单格式，能省略类型标签的情况下就省略标签
     */
    SIMPLE(1) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            styleOut.setValue(Integer.toString(value));
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            styleOut.setValue(Long.toString(value));
            // 数字的默认解析类型是double，如果值超过了double的表示范围，将无法正确解析
            if (Math.abs(value) >= DOUBLE_MAX_LONG) {
                styleOut.setTyped(true);
            }
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            if (Float.isNaN(value) || Float.isInfinite(value)) {
                String string = Float.toString(value);
                styleOut.setValue(string)
                        .setTyped(true);
            } else {
                int iv = (int) value;
                if (iv == value) {
                    styleOut.setValue(Integer.toString(iv));
                } else {
                    String string = Float.toString(value);
                    styleOut.setValue(string)
                            .setTyped(string.lastIndexOf('E') >= 0);
                }
            }
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            if (Double.isNaN(value) || Double.isInfinite(value)) {
                styleOut.setValue(Double.toString(value))
                        .setTyped(true);
            } else {
                long lv = (long) value;
                if (lv == value) {
                    styleOut.setValue(Long.toString(lv));
                } else {
                    String string = Double.toString(value);
                    styleOut.setValue(string)
                            .setTyped(string.lastIndexOf('E') >= 0);
                }
            }
        }
    },

    /** 简单模式，但禁用科学计数法 */
    SIMPLE_NO_SCI(2) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            styleOut.setValue(Integer.toString(value));
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            styleOut.setValue(Long.toString(value));
            // 数字的默认解析类型是double，如果值超过了double的表示范围，将无法正确解析
            if (Math.abs(value) >= DOUBLE_MAX_LONG) {
                styleOut.setTyped(true);
            }
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            if (Float.isNaN(value) || Float.isInfinite(value)) {
                String string = Float.toString(value);
                styleOut.setValue(string)
                        .setTyped(true);
            } else {
                int iv = (int) value;
                if (iv == value) {
                    styleOut.setValue(Integer.toString(iv));
                } else {
                    styleOut.setValue(NumberStyle.toStringNoSci(value));
                }
            }
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            if (Double.isNaN(value) || Double.isInfinite(value)) {
                styleOut.setValue(Double.toString(value))
                        .setTyped(true);
            } else {
                long lv = (long) value;
                if (lv == value) {
                    styleOut.setValue(Long.toString(lv));
                } else {
                    styleOut.setValue(NumberStyle.toStringNoSci(value));
                }
            }
        }
    },

    /** 有类型标签的 */
    TYPED(3) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            SIMPLE.toString(value, styleOut);
            styleOut.setTyped(true);
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            SIMPLE.toString(value, styleOut);
            styleOut.setTyped(true);
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            SIMPLE.toString(value, styleOut);
            styleOut.setTyped(true);
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            SIMPLE.toString(value, styleOut);
            styleOut.setTyped(true);
        }
    },

    /** 有类型标签，且禁用科学计数法 */
    TYPED_NO_SCI(4) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            SIMPLE_NO_SCI.toString(value, styleOut);
            styleOut.setTyped(true);
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            SIMPLE_NO_SCI.toString(value, styleOut);
            styleOut.setTyped(true);
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            SIMPLE_NO_SCI.toString(value, styleOut);
            styleOut.setTyped(true);
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            SIMPLE_NO_SCI.toString(value, styleOut);
            styleOut.setTyped(true);
        }
    },

    /** 16进制，打印正负号，只打印绝对值部分 -- 一定有标签，对于浮点数要小心使用 */
    SIGNED_HEX(5) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            styleOut.setTyped(true);
            if (value < 0 && value != Integer.MIN_VALUE) {
                styleOut.setValue("-0x" + Integer.toHexString(-value));
            } else {
                styleOut.setValue("0x" + Integer.toHexString(value));
            }
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            styleOut.setTyped(true);
            if (value < 0 && value != Long.MIN_VALUE) {
                styleOut.setValue("-0x" + Long.toHexString(-value));
            } else {
                styleOut.setValue("0x" + Long.toHexString(value));
            }
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue(Float.toHexString(value));
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue(Double.toHexString(value));
        }
    },

    /** 无符号16进制，按有效位打印 -- 对于整数来说，无符号的16进制输出不带负号 */
    UNSIGNED_HEX(6) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue("0x" + Integer.toHexString(value));
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue("0x" + Long.toHexString(value));
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue(Float.toHexString(value));
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue(Double.toHexString(value));
        }
    },

    /** 二进制，打印正负号 -- 一定有标签，不支持浮点数 */
    SIGNED_BINARY(7) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            styleOut.setTyped(true);
            if (value < 0 && value != Integer.MIN_VALUE) {
                styleOut.setValue("-0b" + Integer.toBinaryString(-value));
            } else {
                styleOut.setValue("0b" + Integer.toBinaryString(value));
            }
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            styleOut.setTyped(true);
            if (value < 0 && value != Long.MIN_VALUE) {
                styleOut.setValue("-0b" + Long.toBinaryString(-value));
            } else {
                styleOut.setValue("0b" + Long.toBinaryString(value));
            }
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            throw new UnsupportedOperationException();
        }
    },

    /** 无符号2进制，按有效位打印;对于整数来说，无符号的2进制输出不带负号 */
    UNSIGNED_BINARY(8) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue("0b" + Integer.toBinaryString(value));
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            styleOut.setTyped(true)
                    .setValue("0b" + Long.toBinaryString(value));
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            throw new UnsupportedOperationException();
        }
    },

    /** 对于整数来说，固定输出为32位或64位 */
    FIXED_BINARY(9) {
        @Override
        public void toString(int value, StyleOut styleOut) {
            String binaryString = Integer.toBinaryString(value);
            StringBuilder sb = new StringBuilder(34);
            sb.append("0b");
            if (binaryString.length() < 32) {
                pending(sb, 32 - binaryString.length());
            }
            sb.append(binaryString);

            styleOut.setTyped(true);
            styleOut.setValue(sb.toString());
        }

        @Override
        public void toString(long value, StyleOut styleOut) {
            String binaryString = Long.toBinaryString(value);
            StringBuilder sb = new StringBuilder(66);
            sb.append("0b");
            if (binaryString.length() < 64) {
                pending(sb, 64 - binaryString.length());
            }
            sb.append(binaryString);

            styleOut.setTyped(true);
            styleOut.setValue(sb.toString());
        }

        private void pending(StringBuilder sb, int count) {
            if (count <= 0) {
                return;
            }
            sb.append("0".repeat(count));
        }

        @Override
        public void toString(float value, StyleOut styleOut) {
            throw new UnsupportedOperationException();
        }

        @Override
        public void toString(double value, StyleOut styleOut) {
            throw new UnsupportedOperationException();
        }
    },
    ;

    public final int number;

    NumberStyle(int number) {
        this.number = number;
    }

    public int getNumber() {
        return number;
    }

    /** 是否支持浮点数 */
    public boolean supportFloat() {
        return switch (this) {
            case SIGNED_BINARY, UNSIGNED_BINARY, FIXED_BINARY -> false;
            default -> true;
        };
    }

    public static final List<NumberStyle> LOOK_TABLE = List.of(values());

    public static NumberStyle forNumber(int number) {
        return switch (number) {
            case 1 -> SIMPLE;
            case 2 -> SIMPLE_NO_SCI;
            case 3 -> TYPED;
            case 4 -> TYPED_NO_SCI;
            case 5 -> SIGNED_HEX;
            case 6 -> UNSIGNED_HEX;
            case 7 -> SIGNED_BINARY;
            case 8 -> UNSIGNED_BINARY;
            case 9 -> FIXED_BINARY;
            default -> throw new IllegalArgumentException("invalid number " + number);
        };
    }

    /** double能精确表示的最大整数 */
    private static final long DOUBLE_MAX_LONG = (1L << 53) - 1;

    private static String toStringNoSci(float value) {
        return new BigDecimal(Float.toString(value)) // 需先转换为String
                .stripTrailingZeros()
                .toPlainString();
    }

    private static String toStringNoSci(double value) {
        return new BigDecimal(Double.toString(value)) // 需先转换为String
                .stripTrailingZeros()
                .toPlainString();
    }

}