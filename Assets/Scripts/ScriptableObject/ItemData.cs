using UnityEngine;

namespace ThreeDUnity.Items
{
    /// <summary>
    /// Definición de un ítem de tienda: prefab en mundo y precios de compra/venta.
    /// Crea assets con <c>Assets → Create → ThreeD Unity → Item</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "ThreeD Unity/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Prefab")]
        [SerializeField] private GameObject prefab;

        [Header("Precios")]
        [SerializeField, Min(0f)] private float buyPrice;
        [SerializeField, Min(0f)] private float sellPrice;

        public GameObject Prefab => prefab;
        public float BuyPrice => buyPrice;
        public float SellPrice => sellPrice;
    }
}
