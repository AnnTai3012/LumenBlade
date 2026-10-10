using UnityEngine;

public struct PlayerInputFrame
{
    public float Horizontal;
    public bool InteractPressed;
    public bool PickupPressed;
    public bool JumpHeld;
    public bool JumpPressed;
    public bool JumpReleased;
    public bool DashPressed;
    public bool AttackPressed;
    public bool Skill1Pressed;
    public bool Skill2Pressed;
    public bool Skill3Pressed;
    public bool HealItemPressed;
    public bool ShieldItemPressed;
}

[DisallowMultipleComponent]
public sealed class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private KeyCode skill1Key = KeyCode.K;
    [SerializeField] private KeyCode skill2Key = KeyCode.I;
    [SerializeField] private KeyCode skill3Key = KeyCode.U;
    [SerializeField] private KeyCode healItemKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode shieldItemKey = KeyCode.Alpha2;

    public PlayerInputFrame ReadFrame()
    {
        if (Time.timeScale <= 0f || GameplayPanelController.BlocksGameplayInput)
            return default;

        return new PlayerInputFrame
        {
            Horizontal = Input.GetAxisRaw("Horizontal"),
            InteractPressed = Input.GetKeyDown(interactKey),
            PickupPressed = Input.GetKeyDown(pickupKey),
            JumpHeld = Input.GetButton("Jump"),
            JumpPressed = Input.GetButtonDown("Jump"),
            JumpReleased = Input.GetButtonUp("Jump"),
            DashPressed = Input.GetKeyDown(dashKey),
            AttackPressed = Input.GetKeyDown(attackKey),
            Skill1Pressed = Input.GetKeyDown(skill1Key),
            Skill2Pressed = Input.GetKeyDown(skill2Key),
            Skill3Pressed = Input.GetKeyDown(skill3Key),
            HealItemPressed = Input.GetKeyDown(healItemKey) || Input.GetKeyDown(KeyCode.Keypad1),
            ShieldItemPressed = Input.GetKeyDown(shieldItemKey) || Input.GetKeyDown(KeyCode.Keypad2)
        };
    }

    public KeyCode GetSkillKey(PlayerSkill skill) => skill == PlayerSkill.Skill1 ? skill1Key :
        skill == PlayerSkill.Skill2 ? skill2Key : skill3Key;
}
