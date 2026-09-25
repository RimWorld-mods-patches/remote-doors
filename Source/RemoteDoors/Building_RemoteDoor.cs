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
    /// door that colonists open by hand, including vanilla's hold-open toggle.
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

            // A loaded door keeps the wasPowered it was saved with. Resampling it here would lose
            // a power drop that happened between the last refresh and the save, so the door
            // would coast shut later than it does in the game that saved it - a desync in
            // Multiplayer, where joiners load the host's save while the host keeps running.
            if (!respawningAfterLoad)
            {
                wasPowered = Powered;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Saves from before this field was scribed load it as false, the unpowered default.
            // A door loaded while powered just renews its hold at the next refresh as usual.
            Scribe_Values.Look(ref wasPowered, "wasPowered", defaultValue: false);
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
                // While there is power the switch is authoritative. Vanilla's hold-open latch
                // is a veto on closing that outranks everything else, so a leftover one would
                // silently keep the door open the next time power is cut - clearing it here
                // means hold-open only ever applies to an unpowered door.
                holdOpenInt = false;

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
