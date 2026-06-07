using UnityEngine;
using UnityEngine.InputSystem;

public class TouchInputManager : MonoBehaviour
{
    private const int MaxRaycastHits = 32;

    [SerializeField] private InputActionReference press;
    [SerializeField] private InputActionReference touch;
    [SerializeField] private float rayDistance = 700f;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool debugHits;

    private readonly RaycastHit[] raycastHits = new RaycastHit[MaxRaycastHits];
    private InputAction m_pressAction;
    private InputAction m_touchAction;
    private Vector2 m_touchPos;

    private void Awake()
    {
        CacheActions();
    }

    private void Update()
    {
        if (m_pressAction != null && m_pressAction.WasPressedThisFrame())
            Touch();
    }

    private void OnEnable()
    {
        CacheActions();
        m_pressAction?.Enable();
        m_touchAction?.Enable();
    }

    private void OnDisable()
    {
        m_pressAction?.Disable();
        m_touchAction?.Disable();
    }

    private void Touch()
    {
        if (cam == null || m_touchAction == null)
            return;

        m_touchPos = m_touchAction.ReadValue<Vector2>();

        Ray ray = cam.ScreenPointToRay(m_touchPos);
        if (debugHits)
            Debug.DrawRay(ray.origin, ray.direction * 200f, Color.magenta, 1f);

        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, rayDistance, hitMask, QueryTriggerInteraction.Ignore);
        if (hitCount == 0)
        {
            if (debugHits)
                Debug.Log("Touch hit nothing.");
            return;
        }
        if (debugHits && hitCount == raycastHits.Length)
            Debug.Log("Touch ray hit buffer is full. Consider narrowing the hit mask if touches feel inconsistent.");

        SortHitsByDistance(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.collider == null)
                continue;

            ITouchable touchable = hit.collider.GetComponentInParent<Matchable>();
            if (touchable == null || touchable.IsTouched)
                continue;

            if (touchable.OnTouched())
                return;
        }

        if (debugHits)
            Debug.Log($"Touch hit {hitCount} collider(s), but no untapped matchable accepted it.");
    }

    private void CacheActions()
    {
        m_touchAction = touch != null ? touch.action : null;
        m_pressAction = press != null ? press.action : null;
    }

    private void SortHitsByDistance(int hitCount)
    {
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit current = raycastHits[i];
            int j = i - 1;

            while (j >= 0 && raycastHits[j].distance > current.distance)
            {
                raycastHits[j + 1] = raycastHits[j];
                j--;
            }

            raycastHits[j + 1] = current;
        }
    }
}
