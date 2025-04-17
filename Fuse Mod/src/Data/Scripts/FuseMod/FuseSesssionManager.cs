using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace FuseMod
{

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class FuseSessionManager : MySessionComponentBase
    {
        private List<FuseGridManager> gridManagers;
        private bool initialized = false;

        public override void UpdateAfterSimulation()
        {

            if (!initialized)
            {
                Init();
                initialized = true;
            }

            //perform cleanup before we perform any updates
            gridManagers.RemoveAll(gm =>
            {
                var isClosed = gm.IsGridClosed();
                if (isClosed)
                {
                    Logger.DebugToChat($"[Session] Removing GridManager for closed grid: {gm.GetGridName()}");
                    gm.Dispose();
                }
                return isClosed;
            });



            foreach (var manager in gridManagers)
            {
                manager.Update();
            }
        }

        private void Init()
        {

            Logger.DebugToChat("Initialising Fuse Session Manager");

            gridManagers = new List<FuseGridManager>();

            var allGrids = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allGrids, e => e is IMyCubeGrid);

            foreach (IMyCubeGrid grid in allGrids)
            {
                gridManagers.Add(new FuseGridManager(grid));
            }

        }

        public override void LoadData()
        {
            Init();
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
        }


        private void OnEntityAdded(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid != null)
            {
                Logger.DebugToChat($"[Session] New grid added: {grid.CustomName}");
                gridManagers.Add(new FuseGridManager(grid));
            }
        }
    }

}