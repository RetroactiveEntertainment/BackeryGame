using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TouchInputManager : MonoBehaviour
{
    [SerializeField] private InputActionReference press;
    [SerializeField] private InputActionReference touch;
    [SerializeField] private float rayDistance = 700f;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask hitMask = ~0;
    private InputAction m_pressAction;
    private InputAction m_touchAction;
    private Vector2 m_touchPos;

    private void Start()
    {
        m_touchAction = touch.action;
        m_pressAction = press.action;
    }

    private void Update()
    {
        if (m_pressAction.WasPressedThisFrame())
            Touch();
    }

    private void OnEnable()
    {
        press.action.Enable();
        touch.action.Enable();
    }

    private void OnDisable()
    {
        press.action.Disable();
        touch.action.Disable();
    }

    private void Touch()
    {
        m_touchPos = m_touchAction.ReadValue<Vector2>();

        Ray ray = cam.ScreenPointToRay(m_touchPos);
        Debug.DrawRay(ray.origin, ray.direction * 200f, Color.magenta, 1f);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning($"Hit nothing!");
            return;
        }

        GameObject hitObject = hit.collider.gameObject;
        Debug.Log(hitObject.name);
        hitObject.GetComponent<ITouchable>().OnTouched();
    }
}