using Asher.Runtime.Core;

namespace Asher.Runtime
{
    public static class RuntimeEntry
    {
        private static RuntimeController? _controller;

        public static void Init(RuntimeContext context)
        {
            if (_controller != null)
                return;

            _controller = new RuntimeController();
            _controller.Init(context);
        }
    }
}
