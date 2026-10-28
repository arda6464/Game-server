using DietPhysics;
using LiteEntitySystem;

public struct PlayerInputCommand
{
    public byte Keys;
    public byte AimByte;
}

[Flags]
public enum InputKeys : byte
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    Fire = 16,
    Reload = 32,
    Interact = 64,
}

public class PlayerInputController : HumanControllerLogic<PlayerInputCommand, BasePlayerEntity>
{
    public PlayerInputController(EntityParams entityParams)
        : base(entityParams) { }

    public void SetInput(byte keys, byte aimByte)
    {
        ref var input = ref ModifyPendingInput();
        input.Keys = keys;
        input.AimByte = aimByte;
    }

    protected override void BeforeControlledUpdate()
    {
        if (ControlledEntity == null)
            return;

        var input = CurrentInput;
        ControlledEntity.PendingFire = (input.Keys & (byte)InputKeys.Fire) != 0;
        ControlledEntity.PendingInteract = (input.Keys & (byte)InputKeys.Interact) != 0;
        ControlledEntity.PendingAim = input.AimByte * (360f / 256f);

        Vec3 moveDir = Vec3.zero;
        if ((input.Keys & (byte)InputKeys.Up) != 0)
            moveDir.z -= 1f;
        if ((input.Keys & (byte)InputKeys.Down) != 0)
            moveDir.z += 1f;
        if ((input.Keys & (byte)InputKeys.Left) != 0)
            moveDir.x -= 1f;
        if ((input.Keys & (byte)InputKeys.Right) != 0)
            moveDir.x += 1f;

        if (moveDir.sqrMagnitude > 0.001f)
            ControlledEntity.PendingMovement = moveDir.normalized;
        else if (input.Keys != 0 || ControlledEntity.PendingMovement.sqrMagnitude == 0)
            ControlledEntity.PendingMovement = Vec3.zero;

        base.BeforeControlledUpdate();
    }
}
