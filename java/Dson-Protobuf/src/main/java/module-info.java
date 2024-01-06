/**
 * @author wjybxx
 * date - 2023/12/16
 */
module Dson.ProtoBuf {
    requires wjybxx.dson.core;
    requires wjybxx.base;
    requires protobuf.java;

    exports cn.wjybxx.dson.pb;
    opens cn.wjybxx.dson.pb;
}