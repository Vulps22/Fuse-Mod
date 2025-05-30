using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


namespace FuseMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ControlPanel), false, "FuseBlock")]
    public class FuseLogic : MyGameLogicComponent
    {
        private IMyTerminalBlock block;
        private TrackedBlock attachedBlock;
        private bool tripped = true;
        public bool TrippedByDamage = false;
        private bool leverPosition = false; //true == down == tripped, false == up == safe
        private bool initialized = false;
        private Matrix baseMatrix;
        private float currentAngle = 0f;
        private float angleDirection = 1f;
        private const float maxAngle = 185f; // degrees
        private const float minAngle = 125f; // degrees
        private const float rotationSpeed = 1f; // degrees per tick
        private float lastDamage = 0f; // last damage value to compare against

        public static IMyTerminalControlOnOffSwitch toggle;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Logger.DebugToChat("Initialising Fuse Logic Component");
            base.Init(objectBuilder);

            block = Entity as IMyTerminalBlock;
            if (block == null) return;

            block.AppendingCustomInfo += AppendInfo;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

           // FindAttachedBlock();

            Logger.DebugToChat("Fuse Logic Component initialised");
        }

        public void DoOnce()
        {
            var controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyFunctionalBlock>(out controls);

            var fuseToggle = controls.Find(c => c.Id == "FuseMod_TripFuse") as IMyTerminalControlOnOffSwitch;

            if (fuseToggle != null)
            {
                
                toggle.UpdateVisual();
            }


            
            InitAnimation();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if(!initialized)
            {
                DoOnce();
                initialized = true;
            }

            if(!tripped && ShouldTrip())
            {
                setTripped(true);
            }

            if (leverPosition != tripped)
            {
                Animate();
            }

            if (attachedBlock == null) return;

            var sink = attachedBlock.Block.Components.Get<MyResourceSinkComponent>();
            // Additional power logic can be implemented here.
        }

        public void Animate()
        {
            var lever = Entity.GetSubpart("Lever");
            if (lever == null)
            {
                Logger.DebugToChat("Lever subpart not found.");
                return;
            }

            currentAngle += angleDirection * rotationSpeed;

            if (currentAngle > maxAngle || currentAngle < minAngle)
            {
                leverPosition = !leverPosition;
                angleDirection *= -1f;
                currentAngle = MathHelper.Clamp(currentAngle, minAngle, maxAngle);
            }

            var rotation = Matrix.CreateFromAxisAngle(Vector3.Left, MathHelper.ToRadians(-currentAngle));
            var newMatrix = rotation * baseMatrix;

            lever.PositionComp.SetLocalMatrix(ref newMatrix);
        }

        private void InitAnimation()
        {
            var lever = Entity.GetSubpart("Lever");
            baseMatrix = lever.PositionComp.LocalMatrixRef;
            if (leverPosition == tripped) 
            { 
            //just to catch edge cases where the block may already be tripped... to stop the block animating twice
            Animate();
            }
        }

        public override void Close()
        {
            if (block != null)
            {
                block.AppendingCustomInfo -= AppendInfo;
                attachedBlock.RecheckFuse();
            }
        }

        private void AppendInfo(IMyTerminalBlock b, System.Text.StringBuilder info)
        {
            info.AppendLine("Fuse: " + (tripped ? "BLOWN" : "OK"));
        }

        public bool IsTripped()
        {
            return tripped;
        }

        public void setTripped(bool value)
        {
            if(tripped == value) return; // No change
            tripped = value;
        }


        public bool ShouldTrip()
        {
            if (attachedBlock == null)
                return true;
            var targetPos = attachedBlock.Block.Position;

            var grid = block.CubeGrid;
            var slim = grid.GetCubeBlock(targetPos);

            // No block behind -> TRIP immediately
            if (slim == null)
            {
                Logger.DebugToChat("[FuseLogic] No block behind, tripping fuse.");
                return true;
            }

            float integrity = slim.Integrity; // current health
            float maxIntegrity = slim.MaxIntegrity; // max health

            float damage = maxIntegrity - integrity;

            if (damage <= 0)
            {
                TrippedByDamage = false; // Reset tripped by damage flag
                lastDamage = 0; // Reset last damage
                // Block is undamaged
                return false;
            }

            // If the block has not sustained more damage, respect the status quo
            if (damage <= lastDamage) return TrippedByDamage;
            lastDamage = damage;
            // % chance to trip based on damage
            float damageRatio = damage / maxIntegrity; // 0 to 1
            float chanceToTrip = damageRatio * 0.5f;    // up to 50% max

            double roll = MyUtils.GetRandomDouble(0.0, 1.0);
            bool trip = roll < chanceToTrip;

            if (trip)
            {
                TrippedByDamage = true;
            }

            Logger.DebugToChat($"[FuseLogic] Damage: {damage:F2}/{maxIntegrity:F2}, TripChance: {chanceToTrip:P0}, Roll: {roll:F2} -> {(trip ? "TRIP" : "PASS")}");

            return trip;
        }

        public void SetAttachedBlock(TrackedBlock block)
        {
            attachedBlock = block;
            Logger.DebugToChat($"[Fuse Logic] Attached block set: {attachedBlock.Block.CustomName}");
        }
    }
}
