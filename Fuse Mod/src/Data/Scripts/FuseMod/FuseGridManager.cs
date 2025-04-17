using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace FuseMod
{
    public class FuseGridManager
    {
        private List<TrackedBlock> trackedBlocks = new List<TrackedBlock>();
        private IMyCubeGrid grid;

        public FuseGridManager(IMyCubeGrid grid)
        {
            this.grid = grid;

            Logger.DebugToChat($"[GridManager] Created for grid: {grid.CustomName}");

            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks, slim =>
            {
                CreateTrackedBlock(slim);
                return false;
            });

            grid.OnBlockAdded += OnBlockAdded;
            grid.OnBlockRemoved += OnBlockRemoved;
        }

        public void Update()
        {
            for (int i = trackedBlocks.Count - 1; i >= 0; i--)
            {
                var tracked = trackedBlocks[i];
                if (tracked.Block.Closed || tracked.Block.MarkedForClose)
                {
                    Logger.DebugToChat($"[GridManager] Removing closed block: {tracked.Block.CustomName}");
                    trackedBlocks.RemoveAt(i);
                    continue;
                }

                tracked.Update();
            }
        }

        private void OnBlockAdded(IMySlimBlock slim)
        {
            Logger.DebugToChat($"[GridManager] Block added: {slim?.FatBlock?.DisplayName}");
            CreateTrackedBlock(slim);
            NotifyNeighbors(slim.Position);
        }

        private void OnBlockRemoved(IMySlimBlock slim)
        {
            var fat = slim?.FatBlock as IMyFunctionalBlock;
            if (fat != null)
            {
                Logger.DebugToChat($"[GridManager] Block removed: {fat.DisplayName}");
                trackedBlocks.RemoveAll(t => t.Block == fat);
            }

            NotifyNeighbors(slim.Position);
        }

        private void NotifyNeighbors(Vector3I pos)
        {
            foreach (var dir in Base6Directions.EnumDirections)
            {
                var offset = Base6Directions.GetIntVector(dir);
                var neighborBlock = grid.GetCubeBlock(pos + offset)?.FatBlock as IMyFunctionalBlock;

                if (neighborBlock == null)
                    continue;

                var tracked = trackedBlocks.FirstOrDefault(t => t.Block == neighborBlock);
                tracked?.RecheckFuse(); // You’d add this method to TrackedBlock
            }
        }


        private void CreateTrackedBlock(IMySlimBlock slim)
        {
            var fat = slim?.FatBlock as IMyFunctionalBlock;
            if (fat == null)
            {
                Logger.DebugToChat($"[GridManager] Block is not a functional blocks");
                return;
            }

            var sink = fat.Components?.Get<MyResourceSinkComponent>();
            if (sink == null)
            {
                Logger.DebugToChat($"[GridManager] Block has no ResourceSinkComponent: {fat.DisplayName}");
                return;
            }
            
            Logger.DebugToChat($"[GridManager] Tracking new block: {fat.CustomName}");
            trackedBlocks.Add(new TrackedBlock(fat));
        }

        private bool HasFuseAdjacent(IMyTerminalBlock block)
        {
            var grid = block.CubeGrid;
            var pos = block.Position;

            foreach (Base6Directions.Direction dir in Base6Directions.EnumDirections)
            {
                var offset = Base6Directions.GetIntVector(dir);
                var neighbor = grid.GetCubeBlock(pos + offset)?.FatBlock as IMyTerminalBlock;
                if (neighbor != null && neighbor.BlockDefinition.SubtypeName == "FuseBlock")
                    return true;
            }

            return false;
        }

        public bool HandlesGrid(IMyCubeGrid testGrid)
        {
            return testGrid == grid;
        }

        public bool IsGridClosed()
        {
            return grid == null || grid.Closed || grid.MarkedForClose;
        }

        public string GetGridName()
        {
            return grid?.CustomName ?? "Unknown Grid";
        }

        public void Dispose()
        {
            if (grid != null)
            {
                grid.OnBlockAdded -= OnBlockAdded;
                grid.OnBlockRemoved -= OnBlockRemoved;
            }
        }


    }
}
