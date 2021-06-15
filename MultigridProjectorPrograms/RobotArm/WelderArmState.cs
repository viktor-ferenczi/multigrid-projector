namespace MultigridProjectorPrograms.RobotArm
{
    public enum WelderArmState
    {
        // Arm is stopped
        Stopped,

        // The arm is retracting to its initial position
        Retracting,

        // Moving towards the projected block
        Moving,

        // Building the projected block or welding up the built block
        Welding,

        // Finished welding the block
        Finished,

        // The arm detected a collision on moving towards the projected block's position
        Collided,

        // Failed to build the target block
        Failed,

        // The arm fails to reach the target block
        Unreachable,
    }
}