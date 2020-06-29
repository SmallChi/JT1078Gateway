using JT1078.Gateway.Abstractions;

namespace JT1078.Gateway.Impl
{
    public class JT1078NormalGatewayBuilderDefault : IJT1078NormalGatewayBuilder
    {
        public IJT1078Builder JT1078Builder { get; }

        public JT1078NormalGatewayBuilderDefault(IJT1078Builder builder)
        {
            JT1078Builder = builder;
        }

        public IJT1078Builder Builder()
        {
            return JT1078Builder;
        }
    }
}