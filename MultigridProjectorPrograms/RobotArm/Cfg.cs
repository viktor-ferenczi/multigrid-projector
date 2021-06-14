namespace MultigridProjectorPrograms.RobotArm
{
    public static class Cfg
    {
        // Name of the projector to receive the projection information from via MGP's PB API (required)
        public const string ProjectorName = "Shipyard Projector";

        // Name of the rotor rotating the projector (optional),
        // the program makes sure to reverse this rotor if it becomes stuck due to an arm in the way
        public const string ProjectorRotorName = "Shipyard Projector Rotor";

        // Name of the block group containing the first mechanical bases of each arm (required)
        public const string WelderArmsGroupName = "Welder Arms";

        // Name of the block group containing LCD panels to show completion statistics and debug information (optional)
        // Names should contains: Timer, Details, Status, Log
        public const string TextPanelsGroupName = "Shipyard Text Panels";

        // Weight of the direction component of the optimized effector pose in the cost, higher value prefers more precise effector direction
        public const double DirectionCostWeight = 1.0; // Turn the welder arm towards the preview grid's center

        // Weight of the roll component of the optimized effector pose, higher value prefers more precise roll control
        public const double RollCostWeight = 0.0; // Welders don't care about roll, therefore no need to optimize for that

        // L2 regularization of mechanical base activations, higher value prefers simpler arm poses closer to the initial activations
        public const double ActivationRegularization = 2.0;

        // Maximum distance from the effector's tip to weld blocks,
        // it applies to block intersection, not to the distance of their center
        public const double MaxWeldingDistanceLargeWelder = 2.26; // [m]
        public const double MaxWeldingDistanceSmallWelder = 1.3; // [m]

        // Maximum number of full forward-backward optimization passes along the arm segments each tick
        public const int OptimizationPasses = 1;

        // Maximum time to retract the arm after a collision on moving the arm to the target block or during welding
        public const int MaxRetractionTimeAfterCollision = 3; // [Ticks] (1/6 seconds, due to Update10)

        // Maximum time to retract the arm after a block proved to be unreachable after the arm tried to reach it
        public const int MaxRetractionTimeAfterUnreachable = 6; // [Ticks] (1/6 seconds, due to Update10)

        // If the arm moves the wrong direction then consider the target as unreachable
        public const double MovingCostIncreaseLimit = 50.0;

        // Timeout moving the arm near the target block, counted until welding range
        public const int MovingTimeout = 20; // [Ticks] (1/6 seconds, due to Update10)

        // Timeout for welding a block
        public const int WeldingTimeout = 6; // [Ticks] (1/6 seconds, due to Update10)

        // Resets the arm after this many subsequent failed welding attempts
        public const int ResetArmAfterFailedWeldingAttempts = 5;

        // Minimum meaningful activation steps during optimization
        public const double MinActivationStepPiston = 0.001; // [m]
        public const double MinActivationStepRotor = 0.001; // [rad]
        public const double MinActivationStepHinge = 0.001; // [rad]

        // Maximum number of blocks to weld at the same time
        public const int MaxLargeBlocksToWeld = 1;
        public const int MaxSmallBlocksToWeld = 125;
    }
}