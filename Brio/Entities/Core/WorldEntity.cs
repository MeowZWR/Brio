using Dalamud.Interface;
using System;

namespace Brio.Entities.Core;

public class WorldEntity : Entity
{
    public override string FriendlyName => "世界";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.EarthOceania;
    public override bool IsAttached => true;

    public override EntityFlags Flags => EntityFlags.DisableDraw;

    public WorldEntity(IServiceProvider provider) : base("world", provider)
    {
        OnAttached();
    }

    public override void OnAttached()
    {
        //AddCapability(ActivatorUtilities.CreateInstance<世界Capability>(_serviceProvider, this));
    }
}
