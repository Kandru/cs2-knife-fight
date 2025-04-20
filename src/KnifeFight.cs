using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using PanoramaVoteManagerAPI;

namespace KnifeFight
{
    public partial class KnifeFight : BasePlugin
    {
        public override string ModuleName => "CS2 KnifeFight";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private static PluginCapability<IPanoramaVoteManagerAPI> VoteAPI { get; } = new("panoramavotemanager:api");
        private IPanoramaVoteManagerAPI? _voteManager;
        private bool _isActive = false;

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _voteManager = VoteAPI.Get();
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
