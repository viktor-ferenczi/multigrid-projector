using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Definitions;
using System.Linq.Expressions;
using VRage.Game;
using VRage.Network;
using VRage;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.SessionComponents;
using static MultigridProjectorClient.Extra.ConnectSubgrids;

namespace MultigridProjectorClient.Utilities
{
    internal static class Construction
    {
        public static void GrindBlock(MyCubeGrid grid, Vector3I location)
        {
            if (MySession.Static.CreativeToolsEnabled(Sync.MyId) | MySession.Static.CreativeMode)
                grid.RazeBlocks(new List<Vector3I> { location }, MySession.Static.LocalPlayerId, Sync.MyId);

            // Keen is incredibly strict about destroying blocks without creative mode tools.
            // The official servers got nuked too many times so now all damage is calculated on the server.
            // This includes the players grinder and drill tools. The client requests to enable it and the server harms the block they are looking at.
            // The best way is to just ask the player to remove the block themselves, the only alternative being snapping their view to the block and grinding normally.
            else
            {
                MySlimBlock target = grid.GetCubeBlock(location);
                grid.SkinBlocks(target.Min, target.Max, new Vector3(255, 0, 0), MyStringHash.GetOrCompute("Weldless"), false);

                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"Please remove this block: {target?.FatBlock.DisplayNameText ?? target.ToString()}");
            }
        }

        public static void SpawnBlockOnGrid(MyCubeBlockDefinition blockDefinition, MatrixD worldMatrix, MyCubeGrid.MyBlockVisuals visuals)
        {
            MyCubeBuilder cubeBuilder = (MyCubeBuilder)MyAPIGateway.CubeBuilder;

            // Specify the position of the block. This is of the private type BuildData and therefore must be made at runtime.
            object position = Activator.CreateInstance(Reflection.GetType(cubeBuilder, "BuildData"));
            Reflection.SetValue(position, "Position", worldMatrix.Translation);

            if (MySession.Static.ControlledEntity != null)
                Reflection.SetValue(position, "Position", worldMatrix.Translation - MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition());
            else
                Reflection.SetValue(position, "AbsolutePosition", true);

            Reflection.SetValue(position, "Forward", (Vector3)worldMatrix.Forward);
            Reflection.SetValue(position, "Up", (Vector3)worldMatrix.Up);

            // Specify the author of the block. This is of the private type Author and therefore must be made at runtime.
            object authorData = Activator.CreateInstance(
                Reflection.GetType(cubeBuilder, "Author"),
                new object[] {
                    MySession.Static.LocalCharacterEntityId,
                    MySession.Static.LocalPlayerId
                });

            // Create the data to be used in the multiplayer request. This is of the private type GridSpawnRequestData and therefore must be made at runtime.
            object requestData = Activator.CreateInstance(
                Reflection.GetType(cubeBuilder, "GridSpawnRequestData"),
                new object[] {
                    authorData,
                    (DefinitionIdBlit)blockDefinition.Id,
                    position,
                    MySession.Static.CreativeToolsEnabled(Sync.MyId),
                    false,
                    visuals
                });

            // Get the method that is (indirectly) passed into the multiplayer request
            Delegate RequestGridSpawn = Reflection.GetMethod(typeof(MyCubeBuilder), "RequestGridSpawn");

            // Create the lambda that is passed to the multiplayer request and returns the RequestGridSpawn method when called.
            // The game requires that it takes a parameter despite it not being used in any way.
            // As the return type is only known at runtime it must also be compiled it at runtime.
            LambdaExpression lambda = Expression.Lambda(
                Expression.Constant(RequestGridSpawn),
                Expression.Parameter(typeof(IMyEventOwner), "s"));

            Delegate RequestGridSpawnWrapper = lambda.Compile();

            // Get the RaiseStaticEvent function that is used for multiplayer communication.
            // This is a generic method so after finding it we need to create one for the GridSpawnRequestData type
            Delegate RaiseStaticEvent = Reflection.GetGenericMethod(
                typeof(Sandbox.Engine.Multiplayer.MyMultiplayer),
                (m) => m.Name == "RaiseStaticEvent" && m.IsGenericMethodDefinition && m.GetParameters().Length == 4,
                new Type[] { Reflection.GetType(cubeBuilder, "GridSpawnRequestData") });

            // Invoke the method with the warpper function, request data, and default optional parameters
            RaiseStaticEvent.DynamicInvoke(RequestGridSpawnWrapper, requestData, default(EndpointId), null);

            // Call everything subscribed to the OnBlockAdded event
            Delegate eventDelegate = (Delegate)Reflection.GetValue(cubeBuilder, "OnBlockAdded");

            if (eventDelegate == null)
                return;

            foreach (Delegate handler in eventDelegate.GetInvocationList())
                handler.Method.Invoke(handler.Target, new object[] { blockDefinition });
        }

        public static void PlacePreviewBlock(Subgrid subgrid, Vector3I blockPosition)
        {
            MyCubeGrid previewGrid = subgrid.PreviewGrid;
            MyCubeGrid builtGrid = subgrid.BuiltGrid;

            MySlimBlock block = previewGrid.GetCubeBlock(blockPosition);
            MyCubeBlock fatBlock = block.FatBlock;

            // Get the relative position to the built grid
            Vector3I previewMin = fatBlock?.Min ?? block.Position;
            Vector3I previewMax = fatBlock?.Max ?? block.Position;

            Vector3I builtMinTemp = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewMin));
            Vector3I builtMaxTemp = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(previewMax));
            Vector3I builtPos = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(block.Position));

            Vector3I builtMin = Vector3I.Min(builtMinTemp, builtMaxTemp);
            Vector3I builtMax = Vector3I.Max(builtMinTemp, builtMaxTemp);

            // Get the relative rotation to the built grid
            subgrid.GetBlockOrientationQuaternion(block, out var previewQuaternion);

            // Define where the block should be placed
            HashSet<MyCubeGrid.MyBlockLocation> blockLocations = new HashSet<MyCubeGrid.MyBlockLocation>()
            {
                new MyCubeGrid.MyBlockLocation(
                    block.BlockDefinition.Id,
                    builtMin, builtMax, builtPos,
                    previewQuaternion,
                    MyEntityIdentifier.AllocateId(),
                    MySession.Static.LocalPlayerId)
            };

            // Place a block with the exact same properties as the one in the projection
            builtGrid.BuildBlocks(
                block.ColorMaskHSV,
                block.SkinSubtypeId,
                blockLocations,
                MySession.Static.LocalCharacterEntityId,
                MySession.Static.LocalPlayerId);
        }

        private static MySlimBlock GetBuiltBlockAtPos(MySlimBlock projectedBlock)
        {
            // ConnectSubgrids has a useful function to get the subgrid (if applicable) of any block
            if (!TryGetSubgrid(projectedBlock, out Subgrid subgrid))
                return null;

            MyCubeGrid builtGrid = subgrid.BuiltGrid;
            MyCubeGrid previewGrid = subgrid.PreviewGrid;

            if (builtGrid == null)
                return null;

            // Get the block on the built grid of the subgrid in the same position as the projectd block
            Vector3I blockPos = builtGrid.WorldToGridInteger(previewGrid.GridIntegerToWorld(projectedBlock.Position));
            MySlimBlock blockAtPos = builtGrid.GetCubeBlock(blockPos);

            return blockAtPos;
        }

        private static MySlimBlock GetPreviewBlockAtPos(MySlimBlock builtBlock)
        {
            // ConnectSubgrids has a useful function to get the subgrid (if applicable) of any block
            if (!TryGetSubgrid(builtBlock, out Subgrid subgrid))
                return null;

            MyCubeGrid builtGrid = subgrid.BuiltGrid;
            MyCubeGrid previewGrid = subgrid.PreviewGrid;

            // Get the block on the built grid of the subgrid in the same position as the projectd block
            Vector3I blockPos = previewGrid.WorldToGridInteger(builtGrid.GridIntegerToWorld(builtBlock.Position));
            MySlimBlock blockAtPos = previewGrid.GetCubeBlock(blockPos);

            return blockAtPos;
        }

        public static MySlimBlock GetBuiltBlock(MySlimBlock projectedBlock)
        {
            MySlimBlock blockAtPos = GetBuiltBlockAtPos(projectedBlock);

            if (VerifyBuiltBlock(projectedBlock, blockAtPos))
                return blockAtPos;

            return null;
        }

        public static MySlimBlock GetPreviewBlock(MySlimBlock builtBlock)
        {
            MySlimBlock blockAtPos = GetPreviewBlockAtPos(builtBlock);

            if (VerifyBuiltBlock(blockAtPos, builtBlock))
                return blockAtPos;

            return null;
        }

        public static bool VerifyBuiltBlock(MySlimBlock projectedBlock, MySlimBlock builtBlock)
        {
            if (builtBlock == null)
                return false;

            MySlimBlock blockAtPos = GetBuiltBlockAtPos(projectedBlock);

            if (blockAtPos == null)
                return false;

            // See if the blocks are the same type
            if (builtBlock.BlockDefinition != blockAtPos.BlockDefinition)
                return false;

            return true;
        }

        public static bool CanPlaceBlock(MyCubeBlockDefinition blockDefinition, MatrixD worldMatrix, bool dynamicMode = true)
        {
            float cubeSize = MyDefinitionManager.Static.GetCubeSize(blockDefinition.CubeSize);
            BoundingBoxD localbox = new BoundingBoxD((Vector3D)(-blockDefinition.Size * cubeSize * 0.5f), (Vector3D)(blockDefinition.Size * cubeSize * 0.5f));
            MyGridPlacementSettings settings = blockDefinition.CubeSize == MyCubeSize.Large ? MyBlockBuilderBase.CubeBuilderDefinition.BuildingSettings.LargeGrid : MyBlockBuilderBase.CubeBuilderDefinition.BuildingSettings.SmallGrid;

            return MyCubeGrid.TestBlockPlacementArea(blockDefinition, new MyBlockOrientation(), worldMatrix, ref settings, localbox, dynamicMode);
        }

        public static bool WeldBlock(MyProjectorBase projector, MySlimBlock cubeBlock, long owner, ref long builtBy)
        {
            // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
            if (!TryGetSubgrid(cubeBlock, out Subgrid subgrid, out MultigridProjection projection))
                return true;

            int gridIndex = subgrid.Index;

            MyCubeBlock previewBlock = cubeBlock.FatBlock;

            // Weld blocks normally if supported
            // This means either the server plugin is present or we are welding the initial grid supported by vanilla servers
            // Blocks with subparts will not weld correctly on vanilla servers without the plugin (the subparts vanish)
            bool hasSubpart = previewBlock is IMyMechanicalConnectionBlock || previewBlock is IMyAttachableTopBlock;

            // The first subgrid with index 0 is the main grid with the projector on it, therefore it is always Supported
            if (Comms.ServerHasPlugin || gridIndex == 0 && (!hasSubpart || !Config.CurrentConfig.ClientWelding))
            {
                if (Comms.ServerHasPlugin)
                    builtBy = gridIndex; // Deliver the subgrid index via the builtBy field, the owner will be used instead in BuildInternal

                return true;
            }
            
            if (!Config.CurrentConfig.ClientWelding)
            {
                return false;
            }

            // Make sure there is enough space to actually place the block
            if (projector.CanBuild(cubeBlock, true) != BuildCheckResult.OK)
                return false;

            // Sanity checks for DLC and if the block can be welded
            ulong steamId = MySession.Static.Players.TryGetSteamId(owner);
            if (!projector.AllowWelding || !MySession.Static.GetComponent<MySessionComponentDLC>().HasDefinitionDLC(cubeBlock.BlockDefinition, steamId))
                return false;

            // Place the block
            PlacePreviewBlock(subgrid, cubeBlock.Position);

            // Register an event to update the block (if applicable)
            if (previewBlock == null)
                return false;

            void OnPreviewPlace(MyCubeBlock builtBlock)
            {
                if (builtBlock is MyTerminalBlock)
                    UpdateBlock.CopyProperties((MyTerminalBlock)previewBlock, (MyTerminalBlock)builtBlock);

                // We need to wait for the basepart to replicate for the block to be fully placed
                if (builtBlock is MyMechanicalConnectionBlockBase builtBase)
                {
                    Events.OnNextAttachedChanged(builtBase, (_) =>
                    {
                        if (Config.CurrentConfig.ConnectSubgrids)
                        {
                            UpdateTopParts((MyMechanicalConnectionBlockBase)previewBlock, builtBase);
                        }
                        else
                        {
                            MyAttachableTopBlockBase previewTop = GetTopPart((MyMechanicalConnectionBlockBase)previewBlock);
                            MyAttachableTopBlockBase builtTop = GetTopPart((MyMechanicalConnectionBlockBase)builtBlock);

                            ConnectionType connection = AnalyzeConnection(
                                (MyMechanicalConnectionBlockBaseDefinition)previewBlock.BlockDefinition,
                                previewTop?.BlockDefinition);

                            if (connection == ConnectionType.Default)
                            {
                                SkinTopParts(previewTop, builtTop);
                            }
                            else
                            {
                                builtTop.CubeGrid.SkinBlocks(builtTop.Min, builtTop.Max, new Vector3(255, 0, 0), MyStringHash.GetOrCompute("Weldless"), false);
                            }
                        }

                    }, (_) => builtBase.TopBlock != null);
                }

                if (builtBlock is MyAttachableTopBlockBase && Config.CurrentConfig.ConnectSubgrids)
                    UpdateBaseParts((MyAttachableTopBlockBase)previewBlock, (MyAttachableTopBlockBase)builtBlock);
            }

            // FIXME: Use previewBlock.IsBuilt
            Events.OnNextFatBlockAdded(subgrid.BuiltGrid, OnPreviewPlace, (builtBlock) => VerifyBuiltBlock(cubeBlock, builtBlock.SlimBlock));

            return false;
        }
    }
}