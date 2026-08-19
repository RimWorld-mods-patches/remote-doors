using RimWorld;
using Verse;

namespace RemoteDoors
{
    /// <summary>
    /// A door that is held open for as long as it has power, and closes itself when power is cut.
    ///
    /// Rather than driving the door directly, this rides Building_Door's own close timer:
    /// DoorOpen(ticksToClose) writes ticksUntilClose, which Building_Door.Tick counts down
    /// before calling DoorTryClose. Renewing that budget faster than it drains keeps the door
    /// open; simply not renewing it lets vanilla close the door under vanilla's rules,
    /// including waiting for a pawn to clear the doorway first.
    ///
    /// With no power nothing here runs, so an unpowered remote door behaves as an ordinary
    /// door that colonists open by hand.
    /// </summary>
    public class Building_RemoteDoor : Building_Door
    {
        /// <summary>Ticks between renewals of the hold while powered.</summary>
        private const int RefreshInterval = 15;

        private CompPowerTrader power;
        private bool wasPowered;

        private bool Powered => power != null && power.PowerOn;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            power = this.TryGetComp<CompPowerTrader>();
            if (power == null)
            {
                Log.Error($"[RemoteDoors] {def.defName} uses Building_RemoteDoor but has no CompPowerTrader, so it can never be commanded open.");
            }

            wasPowered = Powered;
        }

        protected override void Tick()
        {
            base.Tick();

            if (power == null || !this.IsHashIntervalTick(RefreshInterval))
            {
                return;
            }

            bool powered = Powered;

            if (powered)
            {
                // Renew the close budget with more time than elapses before the next renewal,
                // so the countdown never reaches zero and FreePassage stays true.
                DoorOpen(RefreshInterval * 3);
            }
            else if (wasPowered)
            {
                // Power just dropped. Collapse whatever budget is left so the door shuts on the
                // next tick instead of coasting for up to RefreshInterval * 3.
                DoorOpen(1);
            }

            wasPowered = powered;
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            if (power == null)
            {
                return baseString;
            }

            string status = Powered
                ? "RemoteDoors_HeldOpenPowered".Translate()
                : "RemoteDoors_UnpoweredManual".Translate();

            return baseString.NullOrEmpty() ? status : baseString + "\n" + status;
        }
    }
}
