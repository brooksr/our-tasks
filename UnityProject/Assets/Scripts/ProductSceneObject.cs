using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Tags a spawned 3D object with the product it represents so the
    /// room controller can toggle, reposition, and report on it by id.
    /// </summary>
    public class ProductSceneObject : MonoBehaviour
    {
        public string productId;
        public string category;
        public string sceneObjectType;

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
