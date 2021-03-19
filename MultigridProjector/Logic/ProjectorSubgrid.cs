namespace MultigridProjector.Logic
{
    public class ProjectorSubgrid: Subgrid
    {
        public ProjectorSubgrid(MultigridProjection projection) : base(projection, 0)
        {
        }

        public override bool IsConnectedSomewhere => true;
    }
}