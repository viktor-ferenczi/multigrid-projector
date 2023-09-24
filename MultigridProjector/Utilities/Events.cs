using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.Entity;

namespace MultigridProjector.Utilities
{
    internal class Events
    {
        public static void InvokeOnGameThread(Action task, int frames = -1)
        {
            int targetFrame;

            if (frames > 0)
                targetFrame = MySession.Static.GameplayFrameCounter + frames;
            else
                targetFrame = -1;

            // This is called by the game itself and will not be caught by any of the plugin's try/catch blocks
            // We need to wrap our Action in a try/catch so should it error it does not take out the entire game
            void safeTask()
            {
                try
                {
                    task();
                }
                catch (Exception e)
                {
                    PluginLog.Error(e);
                }
            }

            MyAPIGateway.Utilities.InvokeOnGameThread(safeTask, "MultigridProjector", targetFrame);
        }

        public static void OnNextFatBlockAdded(
            MyCubeGrid grid,
            Action<MyCubeBlock> action,
            Func<MyCubeBlock, bool> predicate = null)
        {
            void verifyReplicated(MyCubeBlock loadedBlock)
            {
                if (loadedBlock is IMyTerminalBlock terminalBlock)
                {
                    var properties = new List<ITerminalProperty>();
                    terminalBlock.GetProperties(properties);

                    if (properties.Count == 0)
                    {
                        // Not fully replicated yet, come back later
                        InvokeOnGameThread(() => verifyReplicated(loadedBlock), frames: 2);
                        return;
                    }
                }

                action(loadedBlock);
            }

            InvokeOnEvent(
                grid,
                (gridInstance, handler) => gridInstance.OnFatBlockAdded += handler,
                (gridInstance, handler) => gridInstance.OnFatBlockAdded -= handler,
                (loadedBlock) => verifyReplicated(loadedBlock),
                predicate);
        }

        public static void OnNextAttachedChanged(
            MyMechanicalConnectionBlockBase baseBlock,
            Action<IMyMechanicalConnectionBlock> action,
            Func<IMyMechanicalConnectionBlock, bool> predicate = null)
        {
            InvokeOnEvent(
                baseBlock,
                (baseBlockInstance, handler) => baseBlockInstance.OnAttachedChanged += handler,
                (baseBlockInstance, handler) => baseBlockInstance.OnAttachedChanged -= handler,
                action,
                predicate);
        }

        public static void OnBlockSpawned(
            Action<MySlimBlock> action,
            Func<MySlimBlock, bool> predicate = null)
        {
            InvokeOnEvent<object, MyEntity>(
                null,
                (_, handler) => MyEntities.OnEntityAdd += handler,
                (_, handler) => MyEntities.OnEntityAdd -= handler,
                entity =>
                {
                    MySlimBlock block = ((MyCubeGrid)entity).GetBlocks().First();
                    action(block);
                },
                entity =>
                {
                    if (entity is MyCubeGrid grid && grid.BlocksCount == 1)
                        return predicate(grid.GetBlocks().First());

                    return false;
                });
        }

        public static void InvokeOnEvent<TObject, TEvent1>(
            TObject eventOwner,
            Action<TObject, Action<TEvent1>> attachHandler,
            Action<TObject, Action<TEvent1>> detachHandler,
            Action<TEvent1> actionToExecute,
            Func<TEvent1, bool> predicate = null,
            int delay = 1)
        {
            void onEvent(TEvent1 arg1)
            {
                // Do not invoke if the predicate fails
                if (predicate != null && !predicate(arg1))
                    return;

                // Detach the event handler
                detachHandler(eventOwner, onEvent);

                // Call the desired action in one frame (events are called before the rest of the game can catch up)
                InvokeOnGameThread(() => actionToExecute(arg1), delay);
            }

            // Attach the event handler
            attachHandler(eventOwner, onEvent);
        }
    }
}