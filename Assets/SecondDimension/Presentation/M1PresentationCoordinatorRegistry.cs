using System;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Composition hook owned by Presentation. The domain adapter registers a factory;
    /// tests may initialize M1FlowPresenter directly with a fake coordinator.
    /// </summary>
    public static class M1PresentationCoordinatorRegistry
    {
        private static Func<IM1PresentationCoordinator> _factory;

        public static void Register(Func<IM1PresentationCoordinator> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public static bool TryCreate(out IM1PresentationCoordinator coordinator)
        {
            coordinator = _factory?.Invoke();
            return coordinator != null;
        }
    }
}
