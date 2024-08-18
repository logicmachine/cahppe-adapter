using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(dev.logilabo.cahppe_adapter.editor.PluginDefinition))]

namespace dev.logilabo.cahppe_adapter.editor
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "dev.logilabo.cahppe-adapter";
        public override string DisplayName => "CAHppe Adapter";

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating)
                .BeforePlugin("dev.logilabo.virtuallens2.apply-non-destructive")
                .Run(CAHppeAdapterPass.Instance);
        }
    }
}
