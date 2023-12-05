/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dson;

import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;

/**
 * 1.读取数组内普通成员时，name传null，读取嵌套对象时使用无name参数的方法
 * 2.如果先调用了{@link #readName()}，name传null或之前读取的值
 *
 * @author wjybxx
 * date - 2023/4/20
 */
@SuppressWarnings("unused")
public interface DsonReader extends AutoCloseable {

    @Override
    void close();

    /** 当前是否处于应该读取type状态 */
    boolean isAtType();

    /**
     * 读取下一个值的类型
     * 如果到达对象末尾，则返回{@link DsonType#END_OF_OBJECT}
     * <p>
     * 循环的基本写法：
     * <pre>{@code
     *  DsonType dsonType;
     *  while((dsonType = readDsonType()) != DsonType.END_OF_OBJECT) {
     *      readName();
     *      readValue();
     *  }
     * }</pre>
     */
    DsonType readDsonType();

    /**
     * 查看下一个值的类型
     * 1.该方法对于解码很有帮助，最常见的作用是判断是否写入了header
     * 2.不论是否支持mark和reset，定义该方法都是必要的，以允许实现类以最小的代价实现
     */
    DsonType peekDsonType();

    /** 当前是否处于应该读取name状态 */
    boolean isAtName();

    /** 读取下一个值的name */
    String readName();

    /**
     * 读取下一个值的name
     * 如果下一个name不等于期望的值，则抛出异常
     */
    void readName(String name);

    /** 当前是否处于应该读取value状态 */
    boolean isAtValue();

    /**
     * 获取当前的数据类型
     * 1.该值在调用任意的读方法后将变化
     * 2.如果尚未执行过{@link #readDsonType()}则抛出异常
     */
    @Nonnull
    DsonType getCurrentDsonType();

    /**
     * 获取当前的字段名字
     * 1.该值在调用任意的读方法后将变化
     * 2.只有在读取值状态下才可访问
     *
     * @return 当前字段的name
     */
    String getCurrentName();

    /**
     * 获取当前上下文的类型
     */
    DsonContextType getContextType();

    // region 简单值

    int readInt32(String name);

    long readInt64(String name);

    float readFloat(String name);

    double readDouble(String name);

    boolean readBoolean(String name);

    String readString(String name);

    void readNull(String name);

    DsonBinary readBinary(String name);

    DsonExtInt32 readExtInt32(String name);

    DsonExtInt64 readExtInt64(String name);

    DsonExtDouble readExtDouble(String name);

    DsonExtString readExtString(String name);

    ObjectRef readRef(String name);

    OffsetTimestamp readTimestamp(String name);

    // endregion

    // region 容器

    void readStartArray();

    void readEndArray();

    void readStartObject();

    void readEndObject();

    /** 开始读取对象头，对象头属于对象的匿名属性 */
    void readStartHeader();

    void readEndHeader();

    /**
     * 回退到等待开始状态
     * 1.该方法只回退状态，不回退输入
     * 2.只有在等待读取下一个值的类型时才可以执行，即等待{@link #readDsonType()}时才可以执行
     * 3.通常用于在读取header之后回退，然后让业务对象的codec去解码
     */
    void backToWaitStart();

    default void readStartArray(String name) {
        readName(name);
        readStartArray();
    }

    default void readStartObject(String name) {
        readName(name);
        readStartObject();
    }

    // endregion

    // region 特殊支持

    /**
     * 如果当前是数组上下文，则不产生影响；
     * 如果当前是Object上下文，且处于读取Name状态则跳过name，否则抛出状态异常
     */
    void skipName();

    /**
     * 如果当前不处于读值状态则抛出状态异常
     */
    void skipValue();

    /**
     * 跳过当前容器对象(Array、Object、Header)的剩余内容
     * 调用该方法后，{@link #getCurrentDsonType()}将返回{@link DsonType#END_OF_OBJECT}
     * 也就是说，调用该方法后应立即调用 readEnd 相关方法
     */
    void skipToEndOfObject();

    /**
     * {@link DsonType#INT32}
     * {@link DsonType#INT64}
     * {@link DsonType#FLOAT}
     * {@link DsonType#DOUBLE}
     */
    Number readNumber(String name);

    /**
     * 读取一个protobuf消息
     * 只有当前数据是Binary的时候才合法
     *
     * @param binaryType 期望的二进制子类型
     */
    <T> T readMessage(String name, int binaryType, @Nonnull Parser<T> parser);

    /**
     * 将value的值读取为字节数组
     * 1.支持类型：String、Binary、Array、Object、Header；
     * 2.返回的bytes中去除了value的length信息；
     * 3.只在二进制流下生效。
     * <p>
     * 该方法主要用于避免中间编解码过程，eg：
     * <pre>
     * A端：             B端            C端
     * object->bytes  bytes->bytes  bytes->object
     * </pre>
     */
    byte[] readValueAsBytes(String name);

    /**
     * 附近一个数据到当前上下文
     *
     * @return 旧值
     */
    Object attach(Object userData);

    Object attachment();

    DsonReaderGuide whatShouldIDo();

}