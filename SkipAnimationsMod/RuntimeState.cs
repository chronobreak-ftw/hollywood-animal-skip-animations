#if DEBUG
using GameEvents;
using Managers;
using Model;

namespace SkipAnimationsMod
{
    internal static class RuntimeState
    {
        public static PoliceRaidManager PoliceRaidManager;
        public static AssociationManager AssociationManager;
        public static MoviesManager MoviesManager;
        public static ViewController ViewController;
        public static GameEventManager GameEventManager;
        public static GameStateManager GameStateManager;
        public static TimeManager TimeManager;
    }
}
#endif
