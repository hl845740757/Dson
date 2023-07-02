package cn.wjybxx.dson.text;

/**
 * @author wjybxx
 * date - 2023/6/19
 */
public enum NumberStyle {

    /**
     * 简单的 - 无类型标签
     * 对于long类型的值要小心使用该模式，因为数字的默认解析类型是double，如果值超过了double的表示范围，将无法正确解析。
     */
    SIMPLE,

    /** 有类型标签的 */
    TYPED,

    /** 16进制 -- 一定有标签 */
    HEX,

    /** 二进制 -- 一定有标签 */
    BINARY,
}