using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MyArcade
{
	/// <summary>
	/// Keyboard + mouse implementation of IInputSource: WASD move, mouse aim, hold left click to attack,
	/// always running, Shift aims the pen gun (click shoots), right click dash, E parry, Space then click = special slash, keys 1-3 switch weapon, Esc/P pause, M mute, R restarts (menus: W/S or arrows, Enter/Space or click).
	/// Works with the old and the new Unity input system.
	/// </summary>
	public class PlayerInput : IInputSource
	{
#if ENABLE_INPUT_SYSTEM
		public Vector3 MoveDirection
		{
			get
			{
				Keyboard kb = Keyboard.current;
				if (kb == null) return Vector3.zero;
				float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
				float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
				return new Vector3(x, 0f, z);
			}
		}

		public Vector2 PointerScreenPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
		public bool AttackHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
		public bool AimHeld => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

		public int WeaponSlotPressed
		{
			get
			{
				Keyboard kb = Keyboard.current;
				if (kb == null) return -1;
				if (kb.digit1Key.wasPressedThisFrame) return 0;
				if (kb.digit2Key.wasPressedThisFrame) return 1;
				if (kb.digit3Key.wasPressedThisFrame) return 2;
				return -1;
			}
		}

		public bool RestartPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
		public bool DashPressed => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
		public bool SpecialPressed => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
		public bool ParryPressed => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
		public bool PausePressed => Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame);
		public bool MutePressed => Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;

		public bool StartPressed =>
			(Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) ||
			(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

		public int MenuMove
		{
			get
			{
				Keyboard kb = Keyboard.current;
				if (kb == null) return 0;
				if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) return 1;
				if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) return -1;
				return 0;
			}
		}

		public int MenuAdjust
		{
			get
			{
				Keyboard kb = Keyboard.current;
				if (kb == null) return 0;
				if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) return 1;
				if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) return -1;
				return 0;
			}
		}

		public bool ConfirmPressed => Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
		public bool PointerPressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
		public Vector3 MoveDirection => new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
		public Vector2 PointerScreenPosition => Input.mousePosition;
		public bool AttackHeld => Input.GetMouseButton(0);
		public bool AimHeld => Input.GetKey(KeyCode.LeftShift);

		public int WeaponSlotPressed
		{
			get
			{
				if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
				if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
				if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
				return -1;
			}
		}

		public bool RestartPressed => Input.GetKeyDown(KeyCode.R);
		public bool DashPressed => Input.GetMouseButtonDown(1);
		public bool SpecialPressed => Input.GetKeyDown(KeyCode.Space);
		public bool ParryPressed => Input.GetKeyDown(KeyCode.E);
		public bool PausePressed => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
		public bool MutePressed => Input.GetKeyDown(KeyCode.M);
		public bool StartPressed => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
		public int MenuMove => (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) ? 1 : (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) ? -1 : 0;
		public int MenuAdjust => (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ? 1 : (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ? -1 : 0;
		public bool ConfirmPressed => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
		public bool PointerPressed => Input.GetMouseButtonDown(0);
#endif
	}
}
