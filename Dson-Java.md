# Dson Java库指南

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

## Java库特性

解析规则不分语言，因此reader的实现应该保持一致，也就不存在特别的特性。
但书写格式各个库的实现可能并不相同，这里谈一谈我为Java Dson库的设计的一些特性 —— 未来c#会具备相同的特性。

### 全局设置

1. 支持行长度限制，自动换行
2. 支持关闭纯文本模式
3. 支持ASCII不可见字符转unicode字符输出
4. 支持无行首打印，即打印为类json模式

一般不建议开启unicode字符输出，我设计它的目的仅仅是考虑到可能有跨语言移植的需求。
支持关闭纯文本模式，是为了与ASCII不可见字符打印为unicode字符兼容。

### NumberStyle

Number支持4种格式输出：

1. SIMPLE 简单模式 —— 普通整数和小数格式，科学计数法
2. TYPED 简单类型模式 —— 在简单模式的基础上打印类型
3. HEX 16进制 —— 一定打印类型
4. BINARY 2进制 —— 一定打印类型
5. FIXED_BINARY 固定长度二进制 —— 一定打印类型

注意：

1. 浮点数不支持二进制格式
2. 浮点数是NaN、Infinite或科学计数法格式时，简单模式下也会打印类型
3. 浮点数的16进制需要小心使用，需先了解规范
4. 因为数字的默认解析类型是double，因此int64的值大于double的精确区间时，将自动添加类型
5. 允许用户添加自己的Style

### StringStyle

字符串支持4种格式：

1. AUTO 自动判别
    1. 当内容较短且无特殊字符，且不是特殊值时不加引号
    2. 当内容长度中等时，打印为双引号字符串
    3. 当内容较长时，打印为文本模式
2. AUTO_QUOTE —— 与AUTO模式相似，但不启用文本模式
3. QUOTE 双引号模式
4. UNQUOTE 无引号模式 —— 常用于输出一些简单格式字符串
5. TEXT 文本模式 —— 常用于输出长字符串

### ObjectStyle

对象(object/array/header)支持两种格式：

1. INDENT 缩进模式(换行)
2. FLOW 流模式

属性较少的对象适合使用Flow模式简化书写和减少行数，同时数据量较大的Array也很适合Flow模式，
可以很好的减少行数。

PS：其实Writer的目标就是尽可能和我们的书写格式一致。