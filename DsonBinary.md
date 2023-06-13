# Dson二进制流

Dson提供了两个版本的二进制格式，从整体上看他们是一样的，区别在于**一个使用number映射字段，一个使用string映射字段**。
使用number映射字段可以使编码后的包体更小，编解码性能也更好。

我们以object的编码为例介绍流的构成。

## number映射字段方案

  <pre>
   length  [dsonType + wireType] +  [lnumber  +  idep] + [length] + [subType] + [data] ...
   4Bytes    5 bits     3 bits       1~13 bits  3 bits   4 Bytes     1~5 Byte     0~n Bytes
   数据长度     1 Byte(unit8)           1 ~ 3 Byte          int32     unit32/Byte
  </pre>

### length区域

1. binary/Object/Array/header的length为fixed32编码，以方便扩展 - binary的length包含subType。

### 类型区域

1. 字段的类型由 DsonType和 *WireType(数字编码类型)* 构成，共1个字节。
2. WireType分为：VarInt、UINT、SINT、FIXED -- 可参考ProtocolBuffer。
3. int32和int64数字的编码类型会随着数字序列化，以确保对方正确的解码

### number区域

1. Dson最初是为序列化而创建的，因此考虑过继承问题，Dson是支持继承的。
2. 字段的fullNumber由两部分构成，localNumber(本地编号)  + idep(继承深度)。
3. fullNumber为uint32类型，1~3个字节

### 子length区

1. 嵌套对象和顶层对象一样都写入了数据长度。
2. 固定长度的属性类型没有length字段
3. 数字没有length字段
4. **string的length是uint32变长编码**，以节省开销 —— 只有最上面提到4种数据的length是fixed32。
5. extInt32、extInt64和extString都是两个简单值的连续写入，subType采用uint32编码，value采用对应值类型的编码。

### 其它

1. binary的subType现固定1个Byte，以减小复杂度和节省开销。
2. string采用utf8编码
3. header是object/array的一个匿名属性，在object中是没有字段id但有类型的值。

## string映射字段

  <p>
  文档型编码格式：
  <pre>
   length  [dsonType + wireType] +  [length + name] +  [length] + [subType] + [data] ...
   4Bytes    5 bits     3 bits           nBytes         4 Bytes    1~5 Byte   0~n Bytes
   数据长度     1 Byte(unit8)             string          int32    unit32/Byte
  </pre>

String映射字段其实和number映射字段的差别只是 fullNumber 变为了字符串类型的名字，而name按照普通的Strig值编码，
即：length采用uint32编码，data采用utf8编码。