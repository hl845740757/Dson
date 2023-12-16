package cn.wjybxx.dson.pb;

import cn.wjybxx.dson.io.DsonOutput;
import com.google.protobuf.MessageLite;

/**
 * @author wjybxx
 * date - 2023/12/16
 */
public interface DsonProtobufOutput extends DsonOutput {

    /** 向输出流中写入一个消息，不包含长度 */
    void writeMessage(MessageLite message);

}