namespace Dson.Text;

public class DsonTextReader : AbstractDsonReader<string>
{
#nullable disable
    private DsonScanner scanner;
    private readonly Stack<DsonToken> pushedTokenQueue = new(6);
    private string nextName;
    /** 未声明为DsonValue，避免再拆装箱 */
    private object nextValue;

    private bool marking;
    private readonly Queue<DsonToken> markedTokenQueue = new(6);

#nullable enable

    public DsonTextReader(DsonTextReaderSettings settings, string dson, DsonMode dsonMode = DsonMode.STANDARD)
        : this(settings, new DsonScanner(dson, dsonMode)) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, DsonCharStream charStream)
        : this(settings, new DsonScanner(charStream)) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, DsonScanner scanner)
        : base(settings) {
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    
    protected override Context GetContext() {
        return (Context)_context;
    }

    protected override Context? GetPooledContext() {
        return (Context)base.GetPooledContext();
    }

    public override void Dispose() {
        base.Dispose();
    }

    #region context

    private Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = GetPooledContext();
        if (context != null) {
            setPooledContext(null);
        }
        else {
            context = new Context();
        }
        context.init(parent, contextType, dsonType);
        return context;
    }

    private void PoolContext(Context context) {
        context.reset();
        setPooledContext(context);
    }

    protected new class Context : AbstractDsonReader<string>.Context
    {
#nullable disable
        DsonToken beginToken;
#nullable enable

        /** header只可触发一次流程 */
        int headerCount = 0;
        /** 元素计数，判断冒号 */
        int count;
        /** 数组/Object成员的类型 - token类型可直接复用；header的该属性是用于注释外层对象的 */
        DsonToken? compClsNameToken;

        public Context() {
        }

        public void reset() {
            base.reset();
            beginToken = null;
            headerCount = 0;
            count = 0;
            compClsNameToken = null;
        }

        public Context getParent() {
            return (Context)parent;
        }
    }

    #endregion
}