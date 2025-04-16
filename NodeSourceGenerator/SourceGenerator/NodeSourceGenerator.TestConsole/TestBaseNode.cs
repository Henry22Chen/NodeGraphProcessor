namespace GraphProcessor;

public class TestBaseNode : BaseNode
{
    [Input]
    public int baseInput1;

    [Input]
    public int baseInput2;
    
    [Output]
    public double baseOutput;
}

[PartialNode]
public partial class TestDerivedNode : TestBaseNode
{
    [Input]
    public float floatInput1;

    [Input]
    public short floatInput2;
    
    [Output]
    public string stringOutput;
}