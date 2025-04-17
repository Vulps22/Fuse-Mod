using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum; // required for MyTransparentGeometry/MySimpleObjectDraw to be able to set blend type.

namespace FuseMod
{
    // This object is always present, from the world load to world unload.
    // NOTE: all clients and server run mod scripts, keep that in mind.
    // NOTE: this and gamelogic comp's update methods run on the main game thread, don't do too much in a tick or you'll lower sim speed.
    // NOTE: also mind allocations, avoid realtime allocations, re-use collections/ref-objects (except value types like structs, integers, etc).
    //
    // The MyUpdateOrder arg determines what update overrides are actually called.
    // Remove any method that you don't need, none of them are required, they're only there to show what you can use.
    // Also remove all comments you've read to avoid the overload of comments that is this file.
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class Session : MySessionComponentBase
    {
        public static Session Instance; // the only way to access session comp from other classes and the only accepted static field.

        public override void LoadData()
        {
            // amogst the earliest execution points, but not everything is available at this point.

            // These can be used anywhere, not just in this method/class:
            // MyAPIGateway. - main entry point for the API
            // MyDefinitionManager.Static. - reading/editing definitions
            // MyGamePruningStructure. - fast way of finding entities in an area
            // MyTransparentGeometry. and MySimpleObjectDraw. - to draw sprites (from TransparentMaterials.sbc) in world (they usually live a single tick)
            // MyVisualScriptLogicProvider. - mainly designed for VST but has its uses, use as a last resort.
            // System.Diagnostics.Stopwatch - for measuring code execution time.
            // ...and many more things, ask in #programming-modding in keen's discord for what you want to do to be pointed at the available things to use.

            Logger.Init();
            Logger.Debug("================= Initialising Fuse Mod ===================");

            Instance = this;
        }

        public override void BeforeStart()
        {
            //Logger.Debug("================= BeforeStart Fuse Mod ===================");
            // executed before the world starts updating

            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyFunctionalBlock>(out controls);
            if (controls.Exists(c => c.Id == "FuseMod_TripFuse"))
                return; // Already exists, don't add again

            var toggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyTerminalBlock>("FuseMod_TripFuse");

            toggle.Title = MyStringId.GetOrCompute("Breaker");
            toggle.Tooltip = MyStringId.GetOrCompute("Manually trip or reset the fuse");
            toggle.OnText = MyStringId.GetOrCompute("SAFE");
            toggle.OffText = MyStringId.GetOrCompute("TRIPPED");
            toggle.SupportsMultipleBlocks = false;

            toggle.Visible = (b) => b?.BlockDefinition.SubtypeName == "FuseBlock";


            toggle.Getter = (b) => !b.GameLogic.GetAs<FuseLogic>()?.IsTripped() ?? true;
            toggle.Setter = (b, v) => b.GameLogic.GetAs<FuseLogic>()?.setTripped(!v);



            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(toggle);

            foreach (var control in controls)
            {
                if (control.Id == "OnOff")
                {
                    control.Enabled = (b) => false;  // Grays it out
                    control.Visible = (b) => false;  // Hides it entirely (optional)
                    Logger.DebugToChat("[FuseLogic] Disabled default On/Off control.");
                    break;
                }
            }
        }

        protected override void UnloadData()
        {
            Logger.Debug("================= UnloadData Fuse Mod ===================");

            // executed when world is exited to unregister events and stuff

            Instance = null; // important for avoiding this object to remain allocated in memory

            Logger.Terminate();
        }

        public override void HandleInput()
        {
            //Logger.Debug("================= HandleInput Fuse Mod ===================");
            // gets called 60 times a second before all other update methods, regardless of framerate, game pause or MyUpdateOrder.
        }

        public override void UpdateBeforeSimulation()
        {
            //Logger.Debug("================= UpdateBeforeSim Fuse Mod ===================");
            // executed every tick, 60 times a second, before physics simulation and only if game is not paused.
        }

        public override void Simulate()
        {
           // Logger.Debug("================= Sim Fuse Mod ===================");
            // executed every tick, 60 times a second, during physics simulation and only if game is not paused.
            // NOTE in this example this won't actually be called because of the lack of MyUpdateOrder.Simulation argument in MySessionComponentDescriptor
        }

        public override void UpdateAfterSimulation()
        {
            //Logger.Debug("================= UpdateAfterSum Fuse Mod ===================");
            // executed every tick, 60 times a second, after physics simulation and only if game is not paused.

            try // example try-catch for catching errors and notifying player, use only for non-critical code!
            {
                // ...
            }
            catch (Exception e) // NOTE: never use try-catch for code flow or to ignore errors! catching has a noticeable performance impact.
            {
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
            }
        }

        public override void Draw()
        {
            // gets called 60 times a second after all other update methods, regardless of framerate, game pause or MyUpdateOrder.
            // NOTE: this is the only place where the camera matrix (MyAPIGateway.Session.Camera.WorldMatrix) is accurate, everywhere else it's 1 frame behind.
        }

        public override void SaveData()
        {
            Logger.Debug("================= Save Fuse Mod ===================");
            // executed AFTER world was saved
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            // executed during world save, most likely before entities.

            return base.GetObjectBuilder(); // leave as-is.
        }

        public override void UpdatingStopped()
        {
            // executed when game is paused
        }
    }
}