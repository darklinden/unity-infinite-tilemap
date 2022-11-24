using UnityEngine;

public class CameraTest : MonoBehaviour
{
    public static CameraTest Instance { get; private set; }
    public FixedJoystick joystick;
    private void Awake()
    {
        Instance = this;
        if (joystick == null)
        {
            joystick = FindObjectOfType<FixedJoystick>();
        }
    }

    protected void Update()
    {
        Vector3 input = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        input.Normalize();

        transform.position += input * 10 * Time.deltaTime;
    }
}