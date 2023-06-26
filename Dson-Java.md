# Dson Java库指南

引入maven依赖：

```
   <dependency>
     <groupId>cn.wjybxx.dson</groupId>
     <artifactId>dson-core</artifactId>
     <version>0.99.beta</version>
   </dependency>
```

## Dsons和DsonLites工具类

在Dson库中提供了Dsons和DsonLites两个工具类，提供了读写Dson的快捷API。  
注意：fromDson默认只读取第一个对象。

```
    DsonObject<String> dsonObject = Dsons.fromDson(dsonString).asObject();
    String dsonString = Dsons.toDson(value, ObjectStyle.INDENT);
    System.out.println(dsonString)
```

## 解析引用(DsonRepository)

Dsons和DsonLites中的方法默认不解析引用，库提供了简单解析引用的工具类*DsonRepository*。

方式1：fromDson的时候解析引用。

```
    DsonRepository repository = DsonRepository.fromDson(dsonString, true);
```

方式2：需要的时候解析引用。该方式支持手动构建repository。

```
    DsonRepository repository = DsonRepository.fromDson(dsonString);
    repository.resolveReference();
```


