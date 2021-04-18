using MultigridProjector.Api;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjector.Logic
{
    public abstract class Connection<T> where T : MyCubeBlock
    {
        // Corresponding preview block
        public readonly T Preview;

        // Built block if any, null if not built
        public T Block;
        public bool HasBuilt => Block != null && !Block.Closed;

        // Block found by the update work, used to follow changes
        public volatile T Found;

        // Requests attaching the counterparty block if exists and at the right position
        public bool RequestAttach;

        protected Connection(T preview)
        {
            Preview = preview;
        }

        public void ClearBuiltBlock()
        {
            Block = null;
            Found = null;
            RequestAttach = false;
        }
    }

    public class BaseConnection: Connection<MyMechanicalConnectionBlockBase>
    {
        public BlockLocation TopLocation;

        public BaseConnection(MyMechanicalConnectionBlockBase previewBlock, BlockLocation topLocation) : base(previewBlock)
        {
            TopLocation = topLocation;
        }

        public bool IsWheel => Preview is MyMotorSuspension;
    }

    public class TopConnection: Connection<MyAttachableTopBlockBase>
    {
        public BlockLocation BaseLocation;

        public TopConnection(MyAttachableTopBlockBase previewBlock, BlockLocation baseLocation) : base(previewBlock)
        {
            BaseLocation = baseLocation;
        }

        public bool IsWheel => Preview is MyWheel;
    }
}