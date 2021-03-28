namespace MultigridProjector.Api
{
    public enum BlockState
    {
        // Block state is still unknown, not determined by the background worker yet
        Unknown = 0,

        // The block is not buildable due to lack of connectivity or colliding objects
        NotBuildable = 1,

        // The block has not built yet and ready to be built (side connections are good and no colliding objects)
        Buildable = 2,

        // The block is being built, but not to the level required by the blueprint (needs more welding)
        BeingBuilt = 4,

        // The block has been built to the level required by the blueprint or more
        FullyBuilt = 8,

        // There is mismatching block in the place of the projected block with a different definition than required by the blueprint
        Mismatch = 128
    }
}