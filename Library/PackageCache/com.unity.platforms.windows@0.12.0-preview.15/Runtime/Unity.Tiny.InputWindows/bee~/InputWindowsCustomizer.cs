using JetBrains.Annotations;

[UsedImplicitly]
class CustomizerForInputWindows : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.InputWindows";

    public override string[] ImplementationFor => new [] { "Unity.Tiny.Input" };
}
