using System;

namespace Jint
{
    public class JintEngineExtended : CJSJintEngine
    {
        public JintEngineExtended(Action<Options> options): base(options) {}
    }
}