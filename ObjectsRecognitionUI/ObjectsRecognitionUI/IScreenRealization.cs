using ReactiveUI;

namespace ObjectsRecognitionUI
{
    public class IScreenRealization : ReactiveObject, IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
    }
}
