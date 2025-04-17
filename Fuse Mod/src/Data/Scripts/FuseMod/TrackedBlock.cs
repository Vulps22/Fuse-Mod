using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.Game.Entity;
using System;
using Sandbox.Game.EntityComponents;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace FuseMod
{
    public class TrackedBlock
    {
        public IMyFunctionalBlock Block { get; private set; }
        private IMyTerminalBlock fuseBlock;
        private FuseLogic fuseLogic;
        private bool isFuseBlock = false;
        private MyResourceSinkComponent sink;
        private int lastUpdateTick = 0;
        private const int updateInterval = 60; // Update every second
        private List<Vector3I> adjacentCells = new List<Vector3I>();
        public TrackedBlock(IMyFunctionalBlock block)
        {
            Block = block;
            Logger.DebugToChat($"[Init] Creating tracked block: {Block.CustomName}");

            sink = Block.Components.Get<MyResourceSinkComponent>();
            if (sink == null)
            {
                Logger.DebugToChat($"[Init] Block '{Block.CustomName}' has no ResourceSinkComponent.");
            }

            adjacentCells = ComputeAdjacentCells(Block);

            FindNearbyFuse();
        }

        public void Update()
        {
            var debug = false;
            int currentTick = MyAPIGateway.Session.GameplayFrameCounter;
            if (Block.CustomName == "vulps")
            {
                debug = true;
            }
            else {
                debug = false;
            }
            if (isFuseBlock)
            {
                Logger.DebugToChat($"[Update] '{Block.CustomName}' is a fuse block, skipping power control.", debug);
                return;
            }

            if ((currentTick - lastUpdateTick) < updateInterval)
            {
                //Logger.DebugToChat($"[Update] '{Block.CustomName}' skipped (not due for update yet).");
                return;
            }

            lastUpdateTick = currentTick;

            if (Block.Closed || Block.MarkedForClose)
            {
                Logger.DebugToChat($"[Update] '{Block.CustomName}' is closed or marked for close.", debug);
                return;
            }

            Logger.DebugToChat($"[Update] Evaluating power state for: '{Block.CustomName}'", debug);

            if (Block.BlockDefinition.SubtypeName == "FuseBlock")
            {
                Logger.DebugToChat($"[Update] '{Block.CustomName}' is a fuse block. Should not be tracked here.", debug);
                return;
            }

            if (fuseBlock == null)
            {
                Logger.DebugToChat($"[Update] '{Block.CustomName}' has no adjacent fuse.", debug);
            }
            else
            {
                Logger.DebugToChat($"[Update] '{Block.CustomName}' has fuse '{fuseBlock.CustomName}'.", debug);
            }

            bool shouldHavePower = ShouldReceivePower();
            Logger.DebugToChat($"[Update] ShouldReceivePower() for '{Block.CustomName}' = {shouldHavePower}", debug);

            Block.Enabled = shouldHavePower;

            // Build the status string
            var sb = new StringBuilder();
            string fuseStatus = "Fuse: ";

            if (fuseBlock == null)
            {
                fuseStatus += "Not Protected";
            }
            else if (fuseLogic.IsTripped())
            {
                fuseStatus += "Tripped";
            }
            else
            {
                fuseStatus += "OK";
            }

            sb.AppendLine(fuseStatus);
        }


        private void FindNearbyFuse()
        {
            var grid = Block.CubeGrid;
            foreach (var pos in adjacentCells)
            {
                var slim = grid.GetCubeBlock(pos);
                var terminal = slim?.FatBlock as IMyTerminalBlock;

                var logic = terminal?.GameLogic?.GetAs<FuseLogic>();
                if (logic != null)
                {
                    fuseBlock = terminal;
                    fuseLogic = logic;
                    fuseLogic.SetAttachedBlock(Block);
                    Logger.DebugToChat($"[Fuse] Connected to {fuseBlock.CustomName} at {pos}");
                    return;
                }
            }


            Logger.DebugToChat($"[FindFuse] Block '{Block.CustomName}' is NOT protected by a fuse.");
        }

        private bool ShouldReceivePower()
        {
            if (sink == null)
            {
                return false;
            }

            if (fuseLogic == null)
            {
                return false;
            }

            bool isTripped = fuseLogic.IsTripped();

            return !isTripped;
        }

        public void RecheckFuse()
        {
            FindNearbyFuse();
        }

        private List<Vector3I> ComputeAdjacentCells(IMyCubeBlock block)
        {
            var min = block.Min;
            var max = block.Max;

            var result = new List<Vector3I>();

            for (int x = min.X - 1; x <= max.X + 1; x++)
            {
                for (int y = min.Y - 1; y <= max.Y + 1; y++)
                {
                    for (int z = min.Z - 1; z <= max.Z + 1; z++)
                    {
                        bool isInsideX = (x >= min.X && x <= max.X);
                        bool isInsideY = (y >= min.Y && y <= max.Y);
                        bool isInsideZ = (z >= min.Z && z <= max.Z);

                        // skip if inside the bounding box on all axes
                        if (isInsideX && isInsideY && isInsideZ)
                            continue;

                        // must be touching the surface
                        if (isInsideX || isInsideY || isInsideZ)
                        {
                            result.Add(new Vector3I(x, y, z));
                        }
                    }
                }
            }

            return result;
        }


    }
}
