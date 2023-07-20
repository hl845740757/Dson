# Dson二进制流

Dson提供了两个版本的二进制格式，从整体上看他们是一样的，区别在于**一个使用number映射字段，一个使用string映射字段**。
使用number映射字段可以使编码后的包体更小，编解码性能也更好。

我们以object的编码为例介绍流的构成。

## number映射字段方案

  <pre>
   length  [dsonType + wireType] +  [lnumber  +  idep] + [length] + [subType] + [data] ...
   4Bytes    5 bits     3 bits        n bits  3 bits     4 Bytes     1~5 Byte   0~n Bytes
   数据长度     1 Byte(unit8)             1~n Byte          int32      unit32
  </pre>

### length区域

1. binary/Object/Array/header的length为fixed32编码，以方便扩展。

### 类型区域

1. 字段的类型由 DsonType和 *WireType(数字编码类型)* 构成，共1个字节。
2. WireType分为：VARINT(0)、UINT(1)、SINT(2)、FIXED(3)、BYTE(4)。
    1. VARINT,UINT,SINT,FIXED可参考ProtocolBuffer。
    2. BYTE只写入int32或int64的末尾8位（1个字节），读取时直接返回读取的字节，因此是有符号的。
3. int32和int64数字的编码类型会随着数字序列化，以确保对方正确的解码。
4. **WireType的比特位用于非数字类型时可以表达其它信息** -- 比如标记null字段。

### number区域

1. Dson最初是为序列化而创建的，因此考虑过继承问题，Dson是支持继承的。
2. 字段的fullNumber由两部分构成，localNumber(本地编号)  + idep(继承深度)。
3. idep的取值范围\[0,7]，localNumber不建议超过8192，不可以为负
4. fullNumber为uint32类型。

### 子length区

1. 嵌套对象和顶层对象一样都写入了数据长度。
2. 固定长度的属性类型没有length字段。
3. 数字没有length字段。
4. **string的length是uint32变长编码**，以节省开销 —— 只有最上面提到4种数据的length是fixed32。
5. extInt32、extInt64都是两个简单值的连续写入，subType采用uint32编码，value采用对应wireType的编码。
6. binary的subType使用uint32编码，data的长度为binary的总长度减去subType占用的动态字节数(1~5)。
7. timestamp为 seconds、nanos、offset、enables 4个值的连续写入，编码格式：uint64,uint32,sint32,uint32

### wireType比特位的特殊使用

1. bool使用wireType记录了其值；wireType为1表示true，0表示false
2. Float和Double使用wireType记录了后导全0的字节数
    1. 浮点数的前16位总是写入
    2. 对于Float，前16位包含了7个有效数据位，就涵盖了大量的常用数；WireType的取值范围为\[0, 2]
    3. 对于Double，前16位包含了4个数据位，也包含了少许常用数据；WireType的取值范围为\[0, 6]
    4. 浮点数压缩算法的实际收益不理想，不过聊胜于无。

3. extString用wireType的bits标记了type和value是否存在，只有存在时才会写入。
    1. 001 用于标记type
    2. 010 用于标记value
    3. 编码顺序为 type, value，其中type为uint32编码

4. ref使用wireType标记了namespace、type、policy是否存在，只有存在时才写入；localId总是写入。
    1. 001 用于标记namespace
    2. 010 用于标记type
    3. 100 用于标记policy
    4. 编码顺序为 localId、namespace、type、policy (可选字段后序列化)
    5. 其中 type 和 policy 使用unit32编码

### 其它

1. string采用utf8编码
2. header是object/array的一个匿名属性，*在object中是没有字段id但有类型的值*。

## string映射字段方案

  <p>
  文档型编码格式：
  <pre>
   length  [dsonType + wireType] +  [length + name] +  [length] + [subType] + [data] ...
   4Bytes    5 bits     3 bits          nBytes         4 Bytes    1~5 Byte   0~n Bytes
   数据长度     1 Byte(unit8)            string          int32      unit32
  </pre>

String映射字段其实和number映射字段的差别只是 fullNumber 变为了字符串类型的名字，而name按照普通的String值编码，
即：length采用uint32编码，data采用utf8编码。