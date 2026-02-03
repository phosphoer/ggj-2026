using UnityEngine;

public class ParentConstraint : MonoBehaviour
{

    public bool DefaultEnabled = true;

    public Transform Target;

    public bool Constrain_Position = true;
    public bool Constrain_Rotation = true;
    public bool MaintainOffset = true;
    public bool UpdateOffsetOnEnable = false;

    bool constrain = true;
    Vector3 position_offset = Vector3.zero;
    Vector3 euler_offset = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetOffset();
        SetConstraint(DefaultEnabled);
    }

    // Update is called once per frame
    void Update()
    {
        if (constrain && Target != null)
        {
            if (Constrain_Position) transform.position = Target.position + position_offset;
            if (Constrain_Rotation) transform.eulerAngles = Target.eulerAngles + euler_offset;
        }
    }

    public void SetConstraint(bool constraint_enabled)
    {
        if (UpdateOffsetOnEnable) SetOffset();

        constrain = constraint_enabled;
    }

    public void SetOffset()
    {
        if (MaintainOffset)
        {
            position_offset = transform.position - Target.position;
            euler_offset = transform.eulerAngles - Target.eulerAngles;
        }
    }

}
