/**
 * 默认全部导出
 *
 * @author wjybxx
 * date - 2023/12/24
 */
module wjybxx.dson.codec {
    requires jsr305;
    requires it.unimi.dsi.fastutil.core;

    requires transitive wjybxx.commons.base;
    requires transitive wjybxx.dson.core;

    exports cn.wjybxx.dson.codec;
    exports cn.wjybxx.dson.codec.codecs;
    exports cn.wjybxx.dson.codec.dson;
    exports cn.wjybxx.dson.codec.dsonlite;

    opens cn.wjybxx.dson.codec;
    opens cn.wjybxx.dson.codec.codecs;
    opens cn.wjybxx.dson.codec.dson;
    opens cn.wjybxx.dson.codec.dsonlite;
}