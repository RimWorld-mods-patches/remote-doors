using RimWorld;
using Verse;

namespace RemoteDoors
{
    /// <summary>
    /// An IED trap that is made safe for as long as it has power, and armed when power is cut.
    ///
    /// Building_Trap.Tick hands each pawn that newly enters the cell to CheckSpring, which rolls
    /// SpringChance and only then calls Spring. Returning zero from SpringChance while powered
    /// therefore skips the whole step-on path, including vanilla's "friendly trap sprung" letter.
    ///
    /// Only the step-on trigger is gated. CompExplosive still starts its wick from damage -
    /// bullets, arrows, or hit points dropping low - exactly as on a vanilla IED, powered or not.
    ///
    /// A pawn already standing on the trap when power drops does not set it off; like vanilla,
    /// the trigger fires on entering the cell, not on standing in it.
    /// </summary>
    public class Building_RemoteIED : Building_TrapExplosive
    {
        private CompPowerTrader power;

        private bool Powered => power != null && power.PowerOn;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            power = this.TryGetComp<CompPowerTrader>();
            if (power == null)
            {
                Log.Error($"[RemoteDoors] {def.defName} uses Building_RemoteIED but has no CompPowerTrader, so it can never be made safe.");
            }
        }

        protected override float SpringChance(Pawn p)
        {
            return Powered ? 0f : base.SpringChance(p);
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            if (power == null)
            {
                return baseString;
            }

            string status = Powered
                ? "RemoteDoors_IEDSafePowered".Translate()
                : "RemoteDoors_IEDArmed".Translate();

            return baseString.NullOrEmpty() ? status : baseString + "\n" + status;
        }
    }
}
