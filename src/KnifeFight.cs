using CounterStrikeSharp.API.Core;

namespace KnifeFight
{
    public partial class KnifeFight : BasePlugin
    {
        public override string ModuleName => "CS2 KnifeFight";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private bool _isActive = false;

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        public override void Unload(bool hotReload)
        {
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            return HookResult.Continue;
        }
    }
}
