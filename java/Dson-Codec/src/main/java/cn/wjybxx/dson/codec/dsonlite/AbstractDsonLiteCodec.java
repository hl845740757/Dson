/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.dson.codec.dsonlite;

import cn.wjybxx.dson.codec.TypeArgInfo;

import java.util.function.Supplier;

/**
 * 生成的代码会继承该类
 *
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public abstract class AbstractDsonLiteCodec<T> implements DsonLiteCodec<T> {

    @Override
    public final T readObject(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        @SuppressWarnings("unchecked") Supplier<? extends T> factory = (Supplier<? extends T>) typeArgInfo.factory;
        final T instance;
        if (factory != null) {
            instance = factory.get();
        } else {
            instance = newInstance(reader, typeArgInfo);
        }
        readFields(reader, instance, typeArgInfo);
        afterDecode(reader, instance, typeArgInfo);
        return instance;
    }

    /**
     * 创建一个对象，如果是一个抽象类，应该抛出异常
     */
    protected abstract T newInstance(DsonLiteObjectReader reader, TypeArgInfo<?> typeArgInfo);

    /**
     * 从输入流中读取所有序列化的字段到指定实例上。
     *
     * @param instance 可以是子类实例
     */
    public abstract void readFields(DsonLiteObjectReader reader, T instance, TypeArgInfo<?> typeArgInfo);

    /**
     * 用于执行用户序列化完成的钩子方法
     */
    protected void afterDecode(DsonLiteObjectReader reader, T instance, TypeArgInfo<?> typeArgInfo) {

    }

}