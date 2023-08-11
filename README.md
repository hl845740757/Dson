# Dson(DataScript Object Notation)

Dson是一个有点奇怪的配置文件格式，但这也许会成为流行的配置文件格式。

Dson同时设计了二进制和文本格式，二进制用于网络传输，文本格式用于配置文件；另外，还提供了数字表示字段的高压缩率二进制版本。  
Dson核心包不提供Dson到对象的编解码实现，只包含Dson的二进制流和文本解析实现；Dson支持复杂的数据结构，同时还设计了对象头，因此实现自己的Codec是很容易的。

Dson最初是为序列化而创建的，但我们在这里只讨论文本格式，二进制格式详见
[Dson二进制流](https://github.com/hl845740757/Dson/blob/dev/DsonBinary.md)，[Commons](https://github.com/hl845740757/BigCat)。  
另外，Dson的一些设计可能与你期望的不同，为避免频繁提问，我将一些设计理由进行了整理，见
[Dson设计过程中的一些反思](https://github.com/hl845740757/Dson/blob/dev/DsonIssues.md)。  
另外，我将一些特殊的测试用例或者说模板整理了一下，见
[Dson测试用例](https://github.com/hl845740757/Dson/blob/dev/DsonTestCase.md)。

## Dson的目标

1. 简单
2. 精确
3. 易于编写和阅读
4. 易于维护(修改)
5. 易于解析和输出
6. 易于扩展

PS：Dson的文本格式从设计到最终实现大概2个月时间，尝试了多个版本的语法和行首，现版本我认为还是达到了目标，在缺乏编辑器的情况下，手写和阅读的体验都还不错。

## Dson的特性

1. 支持注释
2. 支持指定值类型
3. 支持行合并，长内容行随意切割
4. 支持纯文本输入，所见即所得，真正摆脱转义字符 - 也支持混合输入模式
5. 引号不是必须的
6. 支持对象头，可自定义对象头内容
7. 支持类型传递，适用数组元素和Object
8. 支持读取Json

## 文本示例

以下代码来自 DsonTextReaderTest2.java

```
   # 输入文本
   - {@{clsName:MyClassInfo, guid :10001, flags: 0}
   -   name : wjybxx,
   -   age: 28,
   -   pos :{@Vector3 x: 0, y: 0, z: 0},
   -   address: [
   -     beijing,
   -     chengdu
   -   ],
   -   intro: @ss
   |   我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达法，
   | 你可以通过github联系到我。
   -   thanks
   ~   , url: @ss https://www.github.com/hl845740757
   ~   , time: {@dt date: 2023-06-17, time: 18:37:00, millis: 100, offset: +08:00}
   - }
    
   # 程序读取后输出
   - {@{clsName: MyClassInfo, guid: 10001, flags: 0}
   -   name: wjybxx,
   -   age: 28,
   -   pos: {@Vector3
   -     x: 0,
   -     y: 0,
   -     z: 0
   -   },
   -   address: [
   -     beijing,
   -     chengdu
   -   ],
   -   intro: @ss   我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达
   | 法，你可以通过github联系到我。
   -   thanks
   ~ , url: "https://www.github.com/hl845740757",
   -   time: {@dt date: 2023-06-17, time: 18:37:00, millis: 100, 
   - offset: +08:00}
   - }
```

## 行首

Dson最特殊的地方在于行的构成，在Dson中，每一行由**行首**和**内容**构成，行首与内容之间通过一个空格分隔。  
行首目前共5类，如下表：

| 标签 |             | 含义                                             | 格式或示例                                                                         |
|----|-------------|------------------------------------------------|-------------------------------------------------------------------------------|
| #  | comment     | 注释行，当前行有效；不支持行尾注释                              | # 这是一行注释                                                                      |
| -  | append line | 普通行，上一行换行符有效                                   | - 这是普通的一行                                                                     |
| \| | append      | 合并行，上一行换行符无效                                   | \| 这里续写上一行                                                                    |
| ~  | end of text | 表示上一个纯文本输入结束                                   |                                                                               |
| ^  | switch mode | 模式切换，当前行有效；<br/>纯文本模式下插入转义行；<br/>双引号模式下插入纯文本行； | - "我想插入一段正则表达式，但不想处理转义 <br/> ^ ^\[\\u4e00-\\u9fa5_a-zA-Z0-9]+$ <br/> \| 接着书写" |

1. 换行处理只在**双引号字符串**和**纯文本模式**下有效，其它输入没有换行逻辑。
2. '^' 行首是append模式，即不会插入换行符。
3. '^' 通常用于双引号模式中输入复杂的字符串，以避免转义。
4. '|' 只有第一个空格是缩进，第二个字符开始是内容。

PS：挑选行首字符也蛮困难的，既要对阅读影响小，还要让人容易接受其语义暗示，还要敲着方便。。。

## 类型

Dson支持的值类型和内置结构体包括：

| 标签  | 类型        | 枚举 | 含义                    | 内置结构体                                                                                | 格式或示例                                                                                                  |
|-----|-----------|----|-----------------------|--------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------|
| i   | int32     | 1  | 32位整型                 |                                                                                      | @i 123 <br> @i  0xFF                                                                                   |
| L   | int64     | 2  | 64位长整型，大写L            |                                                                                      | @L 123 <br> @L 0xFF                                                                                    |
| f   | float     | 3  | 32位浮点数                |                                                                                      | @f 1.0                                                                                                 |
| d   | double    | 4  | 64位浮点数                |                                                                                      | @d 1.5  <br> 1.5                                                                                       |
| b   | bool      | 5  | bool值                 |                                                                                      | @b true <br> true <br/> @b 1                                                                           |
| s   | string    | 6  | 字符串                   |                                                                                      | "10"   <br>  abc                                                                                       |
| N   | null      | 7  | null，大写N              |                                                                                      | @N null <br> null                                                                                      |
| bin | binary    | 8  | 二进制，带类型标签             | {<br> int32 type;<br> byte[] data <br>}                                              | 格式固定二元数组 \[@bin type, data] <br> \[@bin 1, FFFE]                                                       |
| ei  | extInt32  | 9  | 带类型标签的int32           | {<br> int32 type;<br> int32 value <br>}                                              | 格式固定二元数组 \[@ei type, value] <br> \[@ei 1,  10086]                                                      |
| eL  | extInt64  | 10 | 带类型标签的int64           | {<br> int32 type;<br> int64 value <br>}                                              | 格式固定二元数组 \[@eL type, value] <br> \[@eL 1,  10086]                                                      |
| es  | extString | 11 | *<b>带类型标签的string</b>* | {<br> int32 type;<br> string value <br>}                                             | 格式固定二元数组 \[@es type, value] <br> - \[@es 10, <br/>- @ss ^\[\\u4e00-\\u9fa5_a-zA-Z0-9]+$ <br/> ~ ]      |
| ref | reference | 12 | 引用                    | {<br> string namespace;<br> string localId;<br> int32 type; <br> int32 policy; <br>} | 格式为单值 '@ref localId' 格式或 object格式 <br/> @ref abcdefg <br> {@ref ns: wjybxx, localId: abcdefg, type: 0} |
| dt  | datetime  | 13 | 日期时间                  | { <br>  int64 seconds; <br> int32 nanos;<br> int32 offset;<br> int32 enables; <br> } | 无需引号<br/> {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100}                          |
|     | header    | 29 | 对象头                   |                                                                                      | 对象形式： @{clsName: Vector3 } <br/> 简写形式： @Vector3                                                        |
|     | array     | 30 | 数组                    |                                                                                      | \[ 1, 2, 3, 4, 5 ]                                                                                     |
|     | object    | 31 | 对象/结构体                |                                                                                      | { name: wjybxx, age: 28 }                                                                              |

## 特殊标签

特殊标签不是真正的类型，而是对类型的修饰，是语法糖，用以简化书写。

| 标签 | 类型                         | 含义                                                                                            | 格式或示例                                                                                                       |
|----|----------------------------|-----------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|
| ss | string string <br/> (text) | 纯文本 <br> 1. 不会对字符进行转义，所见即所得 <br/>2. 遇见'~'行首时结束 <br/>3. 既可修饰value，**也可修饰key** <br/>4. 相当于特殊的引号 | - {<br> - url:  @ss https://github.com/hl845740757 <br> ~ , reg: @ss ^\[\u4e00-\u9fa5_a-zA-Z0-9]+$ <br> ~ } |

在我自己的笔记中，示例其实比上面两张表复杂，但我实在不想写转义字符和标签，所以示例偷懒了...

## @声明标签

我们可以通过'@label'声明一个元素的类型，一般是修饰下一个元素，元素可以是 key，也可以是value，取决于上下文。  
普通value类型，可在value的前方通过 @ 声明类型；object和array则在{}和[]内声明其类型，且声明时和外层括号之间无空格。

ps: 当@作用于普通值类型和内置简单结构体时，我们称@声明的是其类型；当@作用于object和array时，我们称@声明的是对象的头信息。

---

## 全局规范：

1. Dson是基于行解析的，换行符只支持 \n 和 \r\n，行首的空白将被忽略。
2. Dson的每一行由 行首和内容 构成。
3. \# 表述注释行，# 号仅对当前行有效，不支持行尾注释。
4. 行首与内容 之间至少键入一个空格（且必须是空格），以表达内容的开始。
5. 类型标签的label为字符串类型，支持无引号和双引号模式；**无引号时，与被修饰的元素之间必须通过一个空格或换行分隔**。
6. 顶层对象必须是Object/Array/Header，你不可以直接声明一个基础值类型，顶层对象之间可以不使用逗号分隔。
7. Dson大小写敏感。
8. Json是有效的Dson输入，无行首的Json的每一行都看做append行；如果一个Dson文本也没有行首，也按照全部是append行解析。

ps: 我去除了顶层不能是header的限制，因此可以用顶层的header来表达文件头，用作其它目的都是不好的。

## 各种值的规范

### Number系列

1. 简单数字格式以外的任何格式，都必须声明类型标签（含下划线时也是需要的）
2. NaN、Infinity、-Infinity的支持取决于编程语言 —— 我现在不清楚是否有语言不支持
3. int32和int64支持16进制和2进制，16进制以0x或0X开头，2进制以0b或0B开头；支持负号。
4. 浮点数支持科学计数法。
5. 整数和浮点数都支持下划线分割，不限制分割方式，但建议3个一组。
    1. 首尾不可以是下划线
    2. 不可以连续多个，下划线也不可以和小数点连续。

```
   - {
   - value1: 10001,
   - value2: 1.05,
   - value3: @i 0xFF,
   - value4: @i 0b10010001,
   - value5: @i 100_000_000,
   - value6: @d 1.0E-6,
   - value7: @d Infinity,
   - value8: @d NaN,
   - value9: @i -0xFF,
   - value10: @i -0b10010001,   
   - }
```

### bool值

1. 未声明类型的情况下，只支持 true和false 两个值，且大小写严格；
2. 声明类型的情况下，支持 0和1 输入； 1表示true，0表示false；
3. 声明类型的情况下，拼写错误将引发错误 -- null同理

```
   {value1: true, value2: false, value3: @b 1, value4: @b 0}
   # 拼写错误将引发异常
   {value1: @b ture, value2: @N nul} 
```

### 字符串

1. 可以使用@s或双引号强调值是一个字符串类型 —— 更推荐使用双引号强调。
2. 普通无引号字符串只能由安全字符构成，安全字符较多，我们给出不安全字符集。
3. 无引号字符串遇见不安全字符时将判定为结束。
4. 使用双引号时，\ 是转义字符，解析时将对引号进行转义，因此内容中出现双引号时需要转义
5. 在纯文本中，不需要加引号，\ 不是转义字符，所见即所得，遇见 '~' 行首时表示结束。
6. 在纯文本中，@ss标签未立即换行的情况下，只有第一空格是分隔字符 —— 立即换行和输入一个空格再换行是等同的。
7. 存在一些特殊含义的字符串，用户最好总是使用双引号强调是字符串，否则可能发生兼容性问题。

#### 不安全字符

1. 空白字符
2. dson token字符集：花括号'{}'  、方括号'[]' 、逗号',' 、 冒号':'  、艾特'@' 、反斜杠 '\'
3. 保留字符集：圆括号 '()'
4. 特殊含义字符串："true", "false", "null", "undefine", "NaN", "Infinity", "-Infinity"
5. 简单数字（整数和小数）

#### 转义字符

转义字符与json基本一致，但 '/' 无需转义

| 引号  | 反斜杠 | 退格(BS) | 换页(FF) | 换行(LF) | 回车(CR) | 水平制表(HT) | unicode字符 |
|-----|-----|--------|--------|--------|--------|----------|-----------|
| \\" | \\\ | \b     | \f     | \n     | \r     | \t       | \u        |

#### 转义换行限制

在处理转义字符的过程中，必须保持输入在逻辑上连续。即：

1. 双引号模式下，只可以切换到 '|' 行。
2. 在纯文本单行转义模式下，只可以继续切换到 '^' 行。

### 二进制

1. 配置格式限定二元组 \[type, data]
2. type限定为int32，且禁止负数
3. data部分使用 **16进制** 编码，且不可以为null

### ei、eL、es

1. 配置格式限定二元组 \[type, value]
2. type限定int32，且禁止负数
3. value部分遵循各自的规范
4. es允许value为null

### ref

1. ref支持两种范式 @ref localId 和 {@ref localId: $localId, ns: $ns, type: $type, policy: $policy}
2. @ref localId 简写方式适用大多数情况，结构体用于复杂情况。
3. ns是namespace的缩写，localId和namespace限定字符串，无特殊符号时可省略引号。
4. **Dson默认不解析引用**，只存储为ref结构，会提供根据localId解析的简单方法。
5. 字段拼写错误将引发异常。

```
   {@ref localId: 10001, ns : global, type: 1 }
   {@ref localId: 10001, ns : global, type: 1, policy: 1}
```

PS：对于配置文件，引用的最大作用是复用和减少嵌套。

### dt-日期时间

1. dt只支持标准的结构体样式 {@dt date: yyyy-MM-dd, time: HH:mm:ss, offset: ±HH:mm:ss, millis: $millis, nanos: $nanos}
2. date 部分为 "yyyy-MM-dd" 格式。
3. time 部分为 "HH:mm:ss" 格式，不可省略秒部分。
4. offset 部分为 "Z", "±H", "±HH", "±HH:mm" 或 "±HH:mm:ss" 格式，不可以省略正负号。
5. millis 表示输入毫秒转纳秒。
6. nanos 表示直接输入纳秒，millis 和 nanos通常只应该出现一个。
7. date 和 time 至少输入一个，其它属性都是可选的，拼写错误将引发异常。

```
    {@dt date: 2023-06-17, time: 18:37:00}
    {@dt date: 2023-06-17, time: 18:37:00, offset: +8}
    {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100}
    {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, nanos: 100_000_000}
```

关于内置结构体的说明

1. seconds 是纪元时间的秒时间(datetime)
2. nanos 是时间戳的纳秒部分
3. offset 是时区的秒数
4. enables 记录了用户输入了哪些部分，由掩码构成，后4个比特位有效。
    1. date 的掩码是 0001
    2. time 的掩码是 0010
    3. offset 的掩码是 0100
    4. nanos 的掩码是 1000

### object

1. 键值对之间通过 ',' (英文逗号) 分隔。
2. key和value之间通过 ':' (英文冒号)分隔，冒号两边空格分隔key和value不是必须的，但冒号和value之间建议输入空格。
3. key为字符串类型，适用字符串规范，无特殊字符时可省略双引号，也可以使用 @ss 标签。
4. 声明对象自己的类型时，@和object的 { 之间无空格，即： {@clsName ，否则将被解释为下一个元素的类型信息。
5. 当声明value的类型时，可通过声明header结构体，并指定 'compClsName' 属性实现。
6. 允许末尾出现逗号

### array

1. value之间通过 ',' (英文逗号) 分隔。
2. 声明对象自己的类型时，@与array的 [ 之间无空格，即： [@clsName ，否则将被解释为下一个元素的类型信息。
3. 当声明value的类型时，可通过声明header结构体，并指定 'compClsName' 属性实现。
4. 允许末尾出现逗号

### header

1. header支持两种范式， "@clsName" 和 "@{ }"。
2. "@clsName" 是 @{clsName: $clsName}的语法糖，用于一般情况下简化书写。
3. clsName为字符串类型，适用字符串规范，无特殊字符时可不加引号。
4. header结构体不可以再内嵌header
5. header没有默认结构体，以允许用户自行扩展。

#### Header特殊属性依赖

虽然header没有默认结构体，但我们还是依赖了一些属性，以支持一些基础功能。因此，如果用户扩展header，使用了这三个属性名，请确保类型一致，否则可能引发兼容性问题。

| 属性名         | 类型     | 含义                                          | 备注             |
|-------------|--------|---------------------------------------------|----------------|
| clsName     | string | className的缩写，表达当前对象的类型                      | 可包括内置基础值和内置结构体 | 
| compClsName | string | componentClassName的缩写，表示数组成员或Object的value类型 | 可包括内置基础值和内置结构体 |
| localId     | string | 对象本地id                                      | 用于支持默认的引用解析    |

以下是object声明header的示例

```
    # 不声明header
    { x: 0, y: 0, z: 0 }
    # 只声明ClassName
    {@Vector3 x: 0, y: 0, z: 0 }
    # 声明复杂的header
    {@{clsName: Vector3, localId: 321123} x: 1, y: 1, z: 1} 
```

### 无类型value解析规则

1. 首先去掉value两端的空白字符
2. 如果value首字符为 { ，则按照object解析
3. 如果value首字符为 [ ，则按照array解析
4. 如果value首字符为 "  ，则按照字符串解析
5. 如果value为 true 或 false ，则解析为bool类型
6. 如果value为 null，则解析为null
7. 如果value匹配整数或浮点数(特殊格式不匹配)，则固定解析为double类型。
8. 其它情况下默认解析为String

## 一些好的实践

1. 行首总是写在行首位置，顶级对象之间通过空内容行分离
2. key尽量遵守一般编程语言变量命名规范，仅使用数字字母和下划线，且非数字开头，以避免引号；同时key应当保持简短，避免换行。
3. ClassName尽量不包含特殊字符，使得ClassName总是可以无引号表达。可考虑通过点号'.'或下划线'_'分隔；
4. { 和 [ 与正式内容之间总是键入一个空格
5. header的定义要保持简单和简短，避免增加额外的复杂度。
6. 字符串尽量使用无引号模式 和 纯文本模式输入，手写转义是麻烦易出错的工作。
7. **一旦你想以某种特殊形式输入一个值，都应该显式声明这个值的类型**。
8. 数字避免使用8进制，浮点数慎用16进制

PS：内置结构体的值类型都是确定的，因此可以不声明类型直接使用特殊方式输入。

## 与其他配置文件格式的比较

### JSON和Bson

json存在的问题：

1. 不支持注释。
    1. 不支持注释导致Json不适合做配置模板，虽然部分库支持注释，但并不是所有库都支持。
2. 字符串不支持换行。
    1. 程序生成的json文件经常出现一行上千和上万个字符的情况。
    2. 手写json出现长文本时非常难受。
3. 不支持文本模式，不支持所见即所得，要处理大量的转义字符。
    1. json字符串存在引号的时候，转义的问题更加明显。
4. 总是要加引号，key要加，value要加，手写json的时候有点难受。
5. 值的精确程度不够，json当初是为js脚本语言设计的。

bson存在的问题：

1. bson最大的问题是兼容json，它包含了json的所有问题 —— 我觉得这是bson最大的失误。
2. bson是为mongodb数据库设计的，其数据格式与类型与数据库强相关。
3. bson的类型语法设计得有点复杂，因为它是基于标准的json格式设计出来。

### YAML

我承认，初次看见YAML文件的时候感到很优美，比如unity生成的yaml文件。
但研究yaml的规则之后，我对于YAML的流行表示不解，我觉得yaml的流行是一种灾难。

虽然我讨厌写json，但我认为json设计的其实很优美，它非常简单，输入严格，你很容易学会使用它。  
YAML看上去也很简单是不是？但**YAML的简单只是表面的简单。**

1. 使用空格缩进来表示层级关系，这是YAML最大的优点和缺点。
    1. 使用不可见的空格缩进来表示层级关系，如果YAML是程序生成的，那很beautiful。
    2. 如果YAML是手工书写，并可能长期维护的，这就是灾难 —— 文件越大，嵌套越深，越痛苦。
    3. 个人认为缩进不应该影响正确性，这非常脆弱，多方面的脆弱。
2. YAML换行处理治标不治本
    1. YAML提供了好多种符号来处理文本段换行问题，在我看来就是没有方案，因为各个都不彻底。
3. YAML不易扩展，因为YAML的规则太复杂了。
    1. 写个无bug的Json解析工具可能一天，写个无bug的YAML解析工具呢？

YAML的设计哲学里忽略了一点，**需要手工编写的文件，维护成本才是最重要的！** 美观最多放第二。  
其实json的可读性我觉得已经足够好了，yaml的排版虽然漂亮，但付出的代价太大 —— 我写个文件就是为了排版好看吗？

PS:

1. 我经常怀疑YAML的设计者是不是程序员...
2. 输入并不是越少越好，简化输入不能损害精确性

### TOML

说实话，TOML我接触的很少，看了几个toml文件示例后，我觉得它太像properties文件的优化版了。
TOML的语法非常接近编程语言：

1. 可通过点号 . 来获取属性和赋值；也支持了数据结构内联
2. 字符串段落使用三个引号""" —— 现在编程语言流行这样，Java也支持；但我想彻底摆脱转义。
3. 换行继续输入使用反斜杠+换行 —— 说起来我最开始也想过这个方案，使用shell的经验

不过，我的直觉是TOML并不适合编写复杂的配置文件，更不适合用作程序导出的文件格式，所以我不选择TOML。

PS：

1. 支持数字使用下划线，来自TOML的提醒，我在写代码时经常用到。
2. TOML语法也是比较独特的，Dson也是。
3. 还有一个我比较陌生的配置文件格式 HOCON，但HOCON只适合程序员，因为太像脚本语言。

### Dson呢？

Dson是一个看似复杂，实则简单的语言。  
Dson主要解决的问题有三个：

1. 单行过长问题 - 期望能换行继续输入
2. 纯文本和转义问题 - 转义很影响可读性
3. 序列化的精确性问题 - Dson最初是为序列化设计的

前两个问题，都通过行首得到了很好的解决；行首的设计，是Dson的特色，
行首带来的优势远大于编写时敲两个字符的负面影响，这使得我们可以编写所见即所得的文本，且可以自由拆分行内容。

序列化的精确性问题则通过对象头来解决；对象头是额外的数据区，不会污染正常的数据区；
我们通过在对象头中存储对象的类型信息，可以实现序列化的精确性，也可以实现一些其它的扩展功能。

## 多语言

作者本人是一个游戏开发者，熟悉JAVA、C#、LUA，后期会提供C#的Dson实现；其它语言可能只能依靠喜欢上Dson的小伙伴们提供。

1. [Java库使用指南](https://github.com/hl845740757/Dson/blob/dev/Dson-Java.md)。
   ```
      # java maven坐标
      <groupId>cn.wjybxx.dson</groupId>
      <artifactId>dson-core</artifactId>
      <version>1.0.2</version>
   ```  
2. C#

## Dson编辑器（缺失）

行首虽然很好地解决了内容拆分的问题，但对新手使用者有一定的干扰，如果有编辑器（和插件）支持的话，行首带来的影响就可以消除。
不过，作者我不是很擅长做这个，暂时无法提供。

## 特别鸣谢

这里感谢MongoDB的Bson团队，Bson源代码对于我实现Dson起到了很大的帮助作用。