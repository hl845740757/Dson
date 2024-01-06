/**
 * 默认全部导出
 *
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.dson.codec {
    requires jsr305;
    requires it.unimi.dsi.fastutil.core;

    requires transitive wjybxx.base;
    requires transitive wjybxx.dson.core;

    exports cn.wjybxx.dson.codec;
    opens cn.wjybxx.dson.codec;
}