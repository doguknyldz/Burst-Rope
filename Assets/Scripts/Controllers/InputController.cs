using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BurstRope
{
    public class InputController : MonoBehaviour
    {
        public float DragSpeed = 12;
        public LayerMask RopeLayer;
        RopeTarget draggingTarget;
        Vector3 offset;
        Plane rayPlane;

        private void Start()
        {
            rayPlane = new Plane(Vector3.up, new Vector3(0, 0, 0));
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100, RopeLayer))
                    if (hit.transform.TryGetComponent(out RopeTarget rope))
                    {
                        draggingTarget = rope;
                        if (rayPlane.Raycast(ray, out float enter))
                            offset = hit.transform.position - ray.GetPoint(enter);
                        offset.y = 0;
                        draggingTarget.StartDragging();
                    }
            }
            if (Input.GetMouseButton(0) && draggingTarget != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (rayPlane.Raycast(ray, out float enter))
                    draggingTarget.transform.position = Vector3.MoveTowards(draggingTarget.transform.position, ray.GetPoint(enter) + offset, DragSpeed * Time.deltaTime);
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (draggingTarget != null)
                    draggingTarget.StopDragging();
                draggingTarget = null;
            }
        }
    }
}
