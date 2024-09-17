namespace MRK {
    public class DisableAtRuntime : BaseBehaviour {
        void Awake() {
            gameObject.SetActive(false);
        }
    }
}
