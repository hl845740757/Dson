/**
 * @author wjybxx
 * date - 2023/6/29
 */
module wjybxx.dson.core {
    requires jsr305;

    exports cn.wjybxx.dson;
    exports cn.wjybxx.dson.text;
    exports cn.wjybxx.dson.io;
    exports cn.wjybxx.dson.types;

    opens cn.wjybxx.dson;
    opens cn.wjybxx.dson.text;
    opens cn.wjybxx.dson.io;
    opens cn.wjybxx.dson.types;

    exports cn.wjybxx.dson.internal to wjybxx.dson.codec;
}