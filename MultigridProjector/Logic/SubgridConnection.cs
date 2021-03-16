using MultigridProjector.Api;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Logic
{
    public abstract class Connection<T> where T : MyCubeBlock
    {
        // Corresponding preview block
        public T Preview;

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
        public bool RequestHead;
        public bool RequestAttach;
        public bool Connected => HasBuilt && Block.TopBlock != null && !Block.TopBlock.Closed;

        public BaseConnection(MyMechanicalConnectionBlockBase previewBlock, BlockLocation topLocation) : base(previewBlock)
        {
            TopLocation = topLocation;
        }

        public override void ClearBuiltBlock()
        {
            base.ClearBuiltBlock();

            RequestHead = false;
            RequestAttach = false;
        }
    }

    public class TopConnection: Connection<MyAttachableTopBlockBase>
    {
        public BlockLocation BaseLocation;
        public bool Connected => HasBuilt && Block.Stator != null && !Block.Stator.Closed;

        public TopConnection(MyAttachableTopBlockBase previewBlock, BlockLocation baseLocation) : base(previewBlock)
        {
            BaseLocation = baseLocation;
        }
    }
}