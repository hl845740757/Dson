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

package cn.wjybxx.dson.apt;

import cn.wjybxx.apt.AptUtils;

import javax.annotation.Nonnull;
import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.AnnotationValue;
import javax.lang.model.element.Element;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Types;
import java.util.List;
import java.util.Objects;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/13
 */
class AptClassImpl {

    public boolean isAnnotationPresent = false;
    public boolean isSingleton = false;

    public String singleton;
    public Set<String> skipFields = Set.of();

    public AptClassImpl() {
    }

    @Nonnull
    public static AptClassImpl parse(Types typeUtils, Element element, TypeMirror implMirror) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, element, implMirror)
                .orElse(null);
        if (annotationMirror == null) {
            return new AptClassImpl();
        }
        return parse(annotationMirror);
    }

    public static AptClassImpl parse(AnnotationMirror annotationMirror) {
        Objects.requireNonNull(annotationMirror, "annotationMirror");
        final AptClassImpl properties = new AptClassImpl();
        properties.isAnnotationPresent = true;
        properties.singleton = AptUtils.getAnnotationValueValue(annotationMirror, "singleton", "").trim();
        properties.isSingleton = !AptUtils.isBlank(properties.singleton);

        // 字符串数组属性返回值为List - 数组都不是直接值...
        List<AnnotationValue> skipFields = AptUtils.getAnnotationValueValue(annotationMirror, "skipFields", List.of());
        if (skipFields.size() > 0) {
            properties.skipFields = skipFields.stream()
                    .map(e -> (String) e.getValue())
                    .collect(Collectors.toSet());
        }
        return properties;
    }
}