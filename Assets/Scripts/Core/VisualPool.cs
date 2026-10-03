using UnityEngine;

namespace OrbitGuard
{
    // Fixed capacity: exhaustion skips cosmetic effects/shots instead of allocating mid-frame.
    public sealed class VisualPool
    {
        public sealed class Item
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public bool active;
            public Vector2 velocity;
            public float lifetime;
            public int team;
        }
        public readonly Item[] items;
        int cursor;
        public VisualPool(string name, int capacity, Sprite sprite, Transform root, int order)
        {
            items = new Item[capacity];
            var parent = new GameObject(name).transform;
            parent.SetParent(root);
            for (int i = 0; i < capacity; i++)
            {
                var obj = new GameObject(name + " " + i);
                obj.transform.SetParent(parent);
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = order;
                obj.SetActive(false);
                items[i] = new Item { transform = obj.transform, renderer = renderer };
            }
        }
        public Item Rent(Vector2 position, Vector2 scale, Color color, float life, Vector2 velocity, int team = 0)
        {
            for (int n = 0; n < items.Length; n++)
            {
                int i = cursor++ % items.Length;
                var item = items[i];
                if (item.active) continue;
                item.active = true;
                item.velocity = velocity;
                item.lifetime = life;
                item.team = team;
                item.transform.position = position;
                item.transform.localScale = scale;
                item.transform.rotation = Quaternion.identity;
                item.renderer.color = color;
                item.transform.gameObject.SetActive(true);
                return item;
            }
            return null;
        }
        public void Return(Item item)
        {
            item.active = false;
            item.transform.gameObject.SetActive(false);
        }
        public void Clear() { foreach (var item in items) if (item.active) Return(item); }
    }
}
