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
import cn.wjybxx.dson.types.ObjectRef;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonLiteCodecScanIgnore
@DsonCodecScanIgnore
public class ObjectRefCodec implements DuplexCodec<ObjectRef> {

    @Nonnull
    @Override
    public Class<ObjectRef> getEncoderClass() {
        return ObjectRef.class;
    }

    @Override
    public void writeObject(DsonLiteObjectWriter writer, ObjectRef instance, TypeArgInfo<?> typeArgInfo) {
        writer.writeRef(writer.getCurrentName(), instance);
    }

    @Override
    public ObjectRef readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readRef(reader.getCurrentName());
    }

    @Override
    public void writeObject(DsonObjectWriter writer, ObjectRef instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeRef(writer.getCurrentName(), instance);
    }

    @Override
    public ObjectRef readObject(DsonObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return reader.readRef(reader.getCurrentName());
    }
}