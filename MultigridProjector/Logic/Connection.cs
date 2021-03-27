using MultigridProjector.Api;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;

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

        protected Connection(T preview)
        {
            Preview = preview;
        }

        public virtual void ClearBuiltBlock()
        {
            Block = null;
            Found = null;
        }
    }

    public class BaseConnection: Connection<MyMechanicalConnectionBlockBase>
    {
        public BlockLocation TopLocation;
        public bool RequestAttach;

        public BaseConnection(MyMechanicalConnectionBlockBase previewBlock, BlockLocation topLocation) : base(previewBlock)
        {
            TopLocation = topLocation;
        }

        public override void ClearBuiltBlock()
        {
            base.ClearBuiltBlock();

            RequestAttach = false;
        }
    }

    public class TopConnection: Connection<MyAttachableTopBlockBase>
    {
        public BlockLocation BaseLocation;

        public TopConnection(MyAttachableTopBlockBase previewBlock, BlockLocation baseLocation) : base(previewBlock)
        {
            BaseLocation = baseLocation;
        }
    }
}