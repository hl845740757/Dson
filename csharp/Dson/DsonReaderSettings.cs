namespace Dson;

public class DsonReaderSettings
{
    public readonly int recursionLimit;
    public readonly bool autoClose;
    public readonly bool enableFieldIntern;

    public DsonReaderSettings(Builder builder) {
        recursionLimit = builder.RecursionLimit;
        autoClose = builder.AutoClose;
        enableFieldIntern = builder.EnableFieldIntern;
    }

    public class Builder
    {
        /// <summary>
        /// 递归深度限制
        /// </summary>
        public int RecursionLimit { get; set; } = 32;

        /// <summary>
        /// 是否自动关闭底层的输入输出流
        /// </summary>
        public bool AutoClose { get; set; } = true;

        /// <summary>
        /// 是否池化字段名
        /// 字段名几乎都是常量，因此命中率几乎百分之百。
        /// 池化字段名可以降低字符串内存占用，有一定的查找开销。
        /// </summary>
        public bool EnableFieldIntern { get; set; } = true;
    }
}