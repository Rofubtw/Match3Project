using UnityEngine;

namespace Match3
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Candy : MonoBehaviour
    {
        public CandyType type;

        public void SetType(CandyType type)
        {
            this.type = type;
            GetComponent<SpriteRenderer>().sprite = type.sprite;
        }

        public CandyType GetType() => type;

        public void DestroyCandy() => Destroy(gameObject);
    }
}