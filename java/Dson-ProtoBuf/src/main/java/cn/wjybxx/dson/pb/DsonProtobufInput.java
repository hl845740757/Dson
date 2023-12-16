package cn.wjybxx.dson.pb;

import cn.wjybxx.dson.io.DsonInput;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/16
 */
public interface DsonProtobufInput extends DsonInput {

    /** 从输入流中读取一个消息 */
    <T> T readMessage(@Nonnull Parser<T> parser);

}