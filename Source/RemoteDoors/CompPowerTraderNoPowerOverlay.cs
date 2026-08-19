using RimWorld;

namespace RemoteDoors
{
    /// <summary>
    /// A CompPowerTrader that never draws the "needs power" overlay.
    ///
    /// For a remote door, having no power is a normal operating state rather than a fault -
    /// an unpowered remote door is simply a closed door - so the red power icon vanilla puts
    /// on unpowered buildings would flag every deliberately closed door as broken.
    ///
    /// CompPowerTrader.PostDraw only draws those overlays, so skipping the base call
    /// suppresses them without losing other behaviour. Breakdown and other comps draw their
    /// own overlays independently and are unaffected.
    /// </summary>
    public class CompPowerTraderNoPowerOverlay : CompPowerTrader
    {
        public override void PostDraw()
        {
            // Intentionally not calling base.PostDraw().
        }
    }
}
